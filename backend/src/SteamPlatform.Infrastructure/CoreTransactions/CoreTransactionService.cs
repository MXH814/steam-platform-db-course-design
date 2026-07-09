using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using SteamPlatform.Application.Auth;
using SteamPlatform.Application.Common;
using SteamPlatform.Application.CoreTransactions;
using SteamPlatform.Infrastructure.Data;
using SteamPlatform.Shared;

namespace SteamPlatform.Infrastructure.CoreTransactions;

public sealed class CoreTransactionService(IDbConnectionFactory connectionFactory) : ICoreTransactionService
{
    private const string FreeClaimGameId = "GAME_CS2";
    private const decimal RechargeMinAmount = 0.01m;
    private const decimal RechargeMaxAmount = 99999.99m;
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    public async Task<WalletSummary> GetWalletAsync(AuthClaims claims, CancellationToken cancellationToken)
    {
        var userId = NormalizePrincipal(claims);

        await using var connection = _connectionFactory.CreateConnection();
        var row = await connection.QueryFirstOrDefaultAsync<WalletRow>(new CommandDefinition(
            """
            select wallet_id, user_id, available_balance, frozen_balance, version
              from wallet_account
             where user_id = :UserId
            """,
            new { UserId = userId },
            cancellationToken: cancellationToken));

        return row?.ToSummary() ?? throw new ResourceNotFoundException("Wallet does not exist.");
    }

    public async Task<RechargeWalletResult> RechargeWalletAsync(AuthClaims claims, RechargeWalletRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = NormalizePrincipal(claims);
        var amount = NormalizeRechargeAmount(request.Amount);
        var idempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            var wallet = await LoadWalletForUpdateAsync(connection, transaction, userId, cancellationToken);
            var existing = await FindWalletTransactionByIdempotencyAsync(connection, transaction, idempotencyKey, cancellationToken);
            if (existing is not null)
            {
                if (!IsSameRecharge(existing, wallet.WalletId) || existing.Amount != amount)
                {
                    throw new BusinessRuleException("IDEMPOTENCY_CONFLICT", "IdempotencyKey is already used by another wallet transaction.");
                }

                await transaction.CommitAsync(cancellationToken);
                return new RechargeWalletResult(
                    wallet.WalletId,
                    existing.TxnId,
                    existing.AvailBalAfter,
                    wallet.FrozenBalance,
                    existing.AvailBalAfter + wallet.FrozenBalance);
            }

            var transactionId = IdGenerator.NewId("WT");
            var balanceBefore = wallet.AvailableBalance;
            var balanceAfter = balanceBefore + amount;

            await connection.ExecuteAsync(new CommandDefinition(
                """
                update wallet_account
                   set available_balance = :BalanceAfter,
                       version = version + 1
                 where wallet_id = :WalletId
                """,
                new { wallet.WalletId, BalanceAfter = balanceAfter },
                transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                """
                insert into wallet_transaction
                  (txn_id, wallet_id, biz_type, biz_ref_id, funds_direction, amount, avail_bal_before, avail_bal_after, idempotency_key, create_time)
                values
                  (:TxnId, :WalletId, 'RECHARGE', :BizRefId, 'CREDIT', :Amount, :Before, :After, :IdempotencyKey, SYSTIMESTAMP)
                """,
                new
                {
                    TxnId = transactionId,
                    wallet.WalletId,
                    BizRefId = transactionId,
                    Amount = amount,
                    Before = balanceBefore,
                    After = balanceAfter,
                    IdempotencyKey = idempotencyKey
                },
                transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
            return new RechargeWalletResult(
                wallet.WalletId,
                transactionId,
                balanceAfter,
                wallet.FrozenBalance,
                balanceAfter + wallet.FrozenBalance);
        }
        catch (Exception exception) when (IsOracleUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new BusinessRuleException("IDEMPOTENCY_CONFLICT", "IdempotencyKey is already used by another wallet transaction.");
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<PagedResponse<WalletTransactionEntry>> ListWalletTransactionsAsync(AuthClaims claims, int page, int pageSize, CancellationToken cancellationToken)
    {
        var userId = NormalizePrincipal(claims);
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var offset = (normalizedPage - 1) * normalizedPageSize;

        await using var connection = _connectionFactory.CreateConnection();
        var walletId = await connection.QueryFirstOrDefaultAsync<string>(new CommandDefinition(
            """
            select wallet_id
              from wallet_account
             where user_id = :UserId
            """,
            new { UserId = userId },
            cancellationToken: cancellationToken));

        if (walletId is null)
        {
            throw new ResourceNotFoundException("Wallet does not exist.");
        }

        var total = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            select count(*)
              from wallet_transaction
             where wallet_id = :WalletId
            """,
            new { WalletId = walletId },
            cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<WalletTransactionRow>(new CommandDefinition(
            """
            select wt.txn_id,
                   wt.biz_type,
                   wt.biz_ref_id,
                   wt.funds_direction,
                   wt.amount,
                   wt.avail_bal_before,
                   wt.avail_bal_after,
                   wt.idempotency_key,
                   wt.create_time
              from wallet_transaction wt
             where wt.wallet_id = :WalletId
             order by wt.create_time desc, wt.txn_id desc
             offset :Offset rows fetch next :PageSize rows only
            """,
            new { WalletId = walletId, Offset = offset, PageSize = normalizedPageSize },
            cancellationToken: cancellationToken));

        return new PagedResponse<WalletTransactionEntry>(
            rows.Select(row => row.ToEntry()).ToArray(),
            normalizedPage,
            normalizedPageSize,
            total);
    }

    public async Task<OrderSummary> BuyGameAsync(AuthClaims claims, CreateOrderRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = NormalizePrincipal(claims);
        var gameId = NormalizeRequired(request.GameId, nameof(request.GameId));
        var idempotencyKey = NormalizeRequired(request.IdempotencyKey, nameof(request.IdempotencyKey));

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var existing = await FindOrderByIdempotencyAsync(connection, userId, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            var game = await LoadOnlineGameAsync(connection, transaction, gameId, cancellationToken);
            var payable = CalculatePayable(game);
            if (payable <= 0)
            {
                throw new BusinessRuleException("GAME_NOT_PAID", "Free games must use the free-claim endpoint.");
            }

            await EnsureGameNotOwnedAsync(connection, transaction, userId, gameId, cancellationToken);
            var wallet = await LoadWalletForUpdateAsync(connection, transaction, userId, cancellationToken);
            if (wallet.AvailableBalance < payable)
            {
                throw new BusinessRuleException("INSUFFICIENT_BALANCE", "Wallet balance is not enough for this purchase.");
            }

            var orderId = IdGenerator.NewId("O");
            var detailId = IdGenerator.NewId("OD");
            var paymentId = IdGenerator.NewId("PAY");
            var libraryId = IdGenerator.NewId("LIB");
            var createdLogId = IdGenerator.NewId("OSL");
            var completedLogId = IdGenerator.NewId("OSL");
            var walletTxnId = IdGenerator.NewId("WT");
            var discountAmount = game.BasePrice - payable;
            var balanceBefore = wallet.AvailableBalance;
            var balanceAfter = balanceBefore - payable;

            await connection.ExecuteAsync(new CommandDefinition(
                """
                update wallet_account
                   set available_balance = :BalanceAfter,
                       version = version + 1
                 where wallet_id = :WalletId
                """,
                new { wallet.WalletId, BalanceAfter = balanceAfter },
                transaction,
                cancellationToken: cancellationToken));

            await InsertCompletedOrderAsync(
                connection,
                transaction,
                orderId,
                userId,
                payable,
                idempotencyKey,
                detailId,
                game,
                discountAmount,
                paymentId,
                createdLogId,
                completedLogId,
                cancellationToken);

            await connection.ExecuteAsync(new CommandDefinition(
                """
                insert into player_library
                  (lib_id, user_id, game_id, acquire_way, status, play_minutes, last_play_time)
                values
                  (:LibraryId, :UserId, :GameId, 'BUY', 'NORMAL', 0, null)
                """,
                new { LibraryId = libraryId, UserId = userId, GameId = game.GameId },
                transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                """
                insert into wallet_transaction
                  (txn_id, wallet_id, biz_type, biz_ref_id, funds_direction, amount, avail_bal_before, avail_bal_after, idempotency_key, create_time)
                values
                  (:TxnId, :WalletId, 'BUY_GAME', :OrderId, 'DEBIT', :Amount, :Before, :After, :IdempotencyKey, SYSTIMESTAMP)
                """,
                new
                {
                    TxnId = walletTxnId,
                    wallet.WalletId,
                    OrderId = orderId,
                    Amount = payable,
                    Before = balanceBefore,
                    After = balanceAfter,
                    IdempotencyKey = $"wallet-{idempotencyKey}"
                },
                transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
            return await GetOrderAsync(claims, orderId, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<OrderSummary> ClaimFreeGameAsync(AuthClaims claims, string gameId, CancellationToken cancellationToken)
    {
        var userId = NormalizePrincipal(claims);
        var normalizedGameId = NormalizeRequired(gameId, nameof(gameId));
        if (!string.Equals(normalizedGameId, FreeClaimGameId, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("GAME_NOT_FREE", "Only Counter-Strike 2 uses the free-claim flow.");
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            var game = await LoadOnlineGameAsync(connection, transaction, FreeClaimGameId, cancellationToken);
            if (game.BasePrice != 0)
            {
                throw new BusinessRuleException("GAME_NOT_FREE", "The requested game is not free.");
            }

            await EnsureGameNotOwnedAsync(connection, transaction, userId, game.GameId, cancellationToken);

            var orderId = IdGenerator.NewId("O");
            var detailId = IdGenerator.NewId("OD");
            var paymentId = IdGenerator.NewId("PAY");
            var libraryId = IdGenerator.NewId("LIB");
            var createdLogId = IdGenerator.NewId("OSL");
            var completedLogId = IdGenerator.NewId("OSL");
            var idempotencyKey = $"free-{userId}-{game.GameId}";

            await InsertCompletedOrderAsync(
                connection,
                transaction,
                orderId,
                userId,
                0,
                idempotencyKey,
                detailId,
                game,
                0,
                paymentId,
                createdLogId,
                completedLogId,
                cancellationToken);

            await connection.ExecuteAsync(new CommandDefinition(
                """
                insert into player_library
                  (lib_id, user_id, game_id, acquire_way, status, play_minutes, last_play_time)
                values
                  (:LibraryId, :UserId, :GameId, 'FREE', 'NORMAL', 0, null)
                """,
                new { LibraryId = libraryId, UserId = userId, GameId = game.GameId },
                transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
            return await GetOrderAsync(claims, orderId, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<IReadOnlyList<OrderSummary>> ListOrdersAsync(AuthClaims claims, CancellationToken cancellationToken)
    {
        var userId = NormalizePrincipal(claims);

        await using var connection = _connectionFactory.CreateConnection();
        var orderIds = await connection.QueryAsync<string>(new CommandDefinition(
            """
            select order_id
              from game_order
             where user_id = :UserId
             order by create_time desc, order_id desc
            """,
            new { UserId = userId },
            cancellationToken: cancellationToken));

        var orders = new List<OrderSummary>();
        foreach (var orderId in orderIds)
        {
            orders.Add(await QueryOrderAsync(connection, userId, orderId, null, cancellationToken)
                ?? throw new ResourceNotFoundException("Order does not exist."));
        }

        return orders;
    }

    public async Task<OrderSummary> GetOrderAsync(AuthClaims claims, string orderId, CancellationToken cancellationToken)
    {
        var userId = NormalizePrincipal(claims);
        var normalizedOrderId = NormalizeRequired(orderId, nameof(orderId));

        await using var connection = _connectionFactory.CreateConnection();
        return await QueryOrderAsync(connection, userId, normalizedOrderId, null, cancellationToken)
            ?? throw new ResourceNotFoundException("Order does not exist.");
    }

    public async Task<IReadOnlyList<LibraryEntry>> ListLibraryAsync(AuthClaims claims, CancellationToken cancellationToken)
    {
        var userId = NormalizePrincipal(claims);

        await using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<LibraryRow>(new CommandDefinition(
            """
            select pl.lib_id,
                   pl.game_id,
                   g.game_name,
                   pl.acquire_way,
                   pl.status,
                   pl.play_minutes,
                   pl.last_play_time
              from player_library pl
              join game g on g.game_id = pl.game_id
             where pl.user_id = :UserId
             order by pl.last_play_time desc nulls last, g.game_name
            """,
            new { UserId = userId },
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToEntry()).ToArray();
    }

    public async Task<LibraryEntry> AddPlaytimeAsync(AuthClaims claims, string gameId, UpdatePlaytimeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = NormalizePrincipal(claims);
        var normalizedGameId = NormalizeRequired(gameId, nameof(gameId));
        if (request.MinutesToAdd <= 0)
        {
            throw new ArgumentException("MinutesToAdd must be greater than 0.");
        }

        await using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            """
            update player_library
               set play_minutes = play_minutes + :MinutesToAdd,
                   last_play_time = SYSTIMESTAMP
             where user_id = :UserId
               and game_id = :GameId
               and status = 'NORMAL'
            """,
            new { UserId = userId, GameId = normalizedGameId, request.MinutesToAdd },
            cancellationToken: cancellationToken));

        if (affected == 0)
        {
            throw new ResourceNotFoundException("Library asset does not exist.");
        }

        var row = await connection.QueryFirstOrDefaultAsync<LibraryRow>(new CommandDefinition(
            """
            select pl.lib_id,
                   pl.game_id,
                   g.game_name,
                   pl.acquire_way,
                   pl.status,
                   pl.play_minutes,
                   pl.last_play_time
              from player_library pl
              join game g on g.game_id = pl.game_id
             where pl.user_id = :UserId
               and pl.game_id = :GameId
            """,
            new { UserId = userId, GameId = normalizedGameId },
            cancellationToken: cancellationToken));

        return row?.ToEntry() ?? throw new ResourceNotFoundException("Library asset does not exist.");
    }

    public async Task<RefundSummary> CreateRefundAsync(AuthClaims claims, CreateRefundRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = NormalizePrincipal(claims);
        var orderId = NormalizeRequired(request.OrderId, nameof(request.OrderId));
        var reason = NormalizeRequired(request.Reason, nameof(request.Reason));

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            var order = await connection.QueryFirstOrDefaultAsync<OrderRefundRow>(new CommandDefinition(
                """
                select order_id, user_id, order_status, payment_status
                  from game_order
                 where order_id = :OrderId
                   and user_id = :UserId
                 for update
                """,
                new { OrderId = orderId, UserId = userId },
                transaction,
                cancellationToken: cancellationToken));

            if (order is null)
            {
                throw new ResourceNotFoundException("Order does not exist.");
            }

            if (order is not { OrderStatus: "COMPLETED", PaymentStatus: "PAID" or "PARTIAL_REFUNDED" })
            {
                throw new BusinessRuleException("ORDER_NOT_REFUNDABLE", "The order is not refundable.");
            }

            var pendingCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                """
                select count(*)
                  from refund_ticket
                 where order_id = :OrderId
                   and status = 'PENDING'
                """,
                new { OrderId = orderId },
                transaction,
                cancellationToken: cancellationToken));

            if (pendingCount > 0)
            {
                throw new BusinessRuleException("REFUND_STATUS_INVALID", "The order already has a pending refund.");
            }

            var details = (await connection.QueryAsync<RefundableDetailRow>(new CommandDefinition(
                """
                select detail_id, payable_amount, refund_amount
                  from order_detail
                 where order_id = :OrderId
                """,
                new { OrderId = orderId },
                transaction,
                cancellationToken: cancellationToken))).AsList();

            var refundAmount = details.Sum(detail => detail.PayableAmount - detail.RefundAmount);
            if (refundAmount <= 0)
            {
                throw new BusinessRuleException("ORDER_NOT_REFUNDABLE", "The order has no refundable amount.");
            }

            var playMinutes = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                """
                select coalesce(sum(pl.play_minutes), 0)
                  from player_library pl
                  join order_detail od on od.game_id = pl.game_id
                 where od.order_id = :OrderId
                   and pl.user_id = :UserId
                   and pl.status = 'NORMAL'
                """,
                new { OrderId = orderId, UserId = userId },
                transaction,
                cancellationToken: cancellationToken));

            var refundId = IdGenerator.NewId("R");
            await connection.ExecuteAsync(new CommandDefinition(
                """
                insert into refund_ticket
                  (refund_id, order_id, refund_amount, refund_type, reason, play_time_hours, status, apply_time)
                values
                  (:RefundId, :OrderId, :RefundAmount, 'FULL', :Reason, :PlayTimeHours, 'PENDING', SYSTIMESTAMP)
                """,
                new
                {
                    RefundId = refundId,
                    OrderId = orderId,
                    RefundAmount = refundAmount,
                    Reason = reason,
                    PlayTimeHours = Math.Round(playMinutes / 60m, 2, MidpointRounding.AwayFromZero)
                },
                transaction,
                cancellationToken: cancellationToken));

            foreach (var detail in details.Where(detail => detail.PayableAmount > detail.RefundAmount))
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    """
                    insert into refund_detail
                      (refund_detail_id, refund_id, order_detail_id, refund_amount)
                    values
                      (:RefundDetailId, :RefundId, :OrderDetailId, :RefundAmount)
                    """,
                    new
                    {
                        RefundDetailId = IdGenerator.NewId("RD"),
                        RefundId = refundId,
                        OrderDetailId = detail.DetailId,
                        RefundAmount = detail.PayableAmount - detail.RefundAmount
                    },
                    transaction,
                    cancellationToken: cancellationToken));
            }

            await connection.ExecuteAsync(new CommandDefinition(
                "update game_order set order_status = 'REFUNDING' where order_id = :OrderId",
                new { OrderId = orderId },
                transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
            return await QueryRefundAsync(connection, refundId, userId, cancellationToken)
                ?? throw new ResourceNotFoundException("Refund does not exist.");
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<IReadOnlyList<RefundSummary>> ListRefundsAsync(AuthClaims claims, CancellationToken cancellationToken)
    {
        var userId = NormalizePrincipal(claims);

        await using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<RefundRow>(new CommandDefinition(
            """
            select rt.refund_id,
                   rt.order_id,
                   rt.refund_amount,
                   rt.refund_type,
                   rt.reason,
                   rt.play_time_hours,
                   rt.status,
                   rt.apply_time
              from refund_ticket rt
              join game_order go on go.order_id = rt.order_id
             where go.user_id = :UserId
             order by rt.apply_time desc, rt.refund_id desc
            """,
            new { UserId = userId },
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToSummary()).ToArray();
    }

    public async Task<IReadOnlyList<RefundSummary>> ListAllRefundsAsync(AuthClaims claims, CancellationToken cancellationToken)
    {
        _ = NormalizeAdmin(claims);

        await using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<RefundRow>(new CommandDefinition(
            """
            select rt.refund_id,
                   rt.order_id,
                   rt.refund_amount,
                   rt.refund_type,
                   rt.reason,
                   rt.play_time_hours,
                   rt.status,
                   rt.apply_time
              from refund_ticket rt
             order by rt.apply_time desc, rt.refund_id desc
            """,
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToSummary()).ToArray();
    }

    public async Task<RefundSummary> ApproveRefundAsync(AuthClaims claims, string refundId, AuditRefundRequest request, CancellationToken cancellationToken)
    {
        var adminId = NormalizeAdmin(claims);
        var normalizedRefundId = NormalizeRequired(refundId, nameof(refundId));
        var reason = NormalizeOptional(request?.Reason) ?? "Approved.";

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            var refund = await LoadPendingRefundForUpdateAsync(connection, transaction, normalizedRefundId, cancellationToken);
            var order = await connection.QueryFirstAsync<OrderRefundRow>(new CommandDefinition(
                """
                select order_id, user_id, order_status, payment_status
                  from game_order
                 where order_id = :OrderId
                 for update
                """,
                new { refund.OrderId },
                transaction,
                cancellationToken: cancellationToken));

            var wallet = await LoadWalletForUpdateAsync(connection, transaction, order.UserId, cancellationToken);
            var before = wallet.AvailableBalance;
            var after = before + refund.RefundAmount;

            await connection.ExecuteAsync(new CommandDefinition(
                """
                update wallet_account
                   set available_balance = :After,
                       version = version + 1
                 where wallet_id = :WalletId
                """,
                new { wallet.WalletId, After = after },
                transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                """
                update order_detail od
                   set refund_amount = refund_amount + (
                     select rd.refund_amount
                       from refund_detail rd
                      where rd.refund_id = :RefundId
                        and rd.order_detail_id = od.detail_id
                   )
                 where od.order_id = :OrderId
                   and exists (
                     select 1
                       from refund_detail rd
                      where rd.refund_id = :RefundId
                        and rd.order_detail_id = od.detail_id
                   )
                """,
                new { RefundId = refund.RefundId, refund.OrderId },
                transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                """
                update game_order
                   set order_status = 'CLOSED',
                       payment_status = 'REFUNDED'
                 where order_id = :OrderId
                """,
                new { refund.OrderId },
                transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                """
                update player_library pl
                   set status = 'REVOKED'
                 where pl.user_id = :UserId
                   and pl.status = 'NORMAL'
                   and exists (
                     select 1
                       from order_detail od
                      where od.order_id = :OrderId
                        and od.game_id = pl.game_id
                   )
                """,
                new { order.UserId, refund.OrderId },
                transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                "update refund_ticket set status = 'APPROVED' where refund_id = :RefundId",
                new { refund.RefundId },
                transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                """
                insert into wallet_transaction
                  (txn_id, wallet_id, biz_type, biz_ref_id, funds_direction, amount, avail_bal_before, avail_bal_after, idempotency_key, create_time)
                values
                  (:TxnId, :WalletId, 'REFUND', :RefundId, 'CREDIT', :Amount, :Before, :After, :IdempotencyKey, SYSTIMESTAMP)
                """,
                new
                {
                    TxnId = IdGenerator.NewId("WT"),
                    wallet.WalletId,
                    refund.RefundId,
                    Amount = refund.RefundAmount,
                    Before = before,
                    After = after,
                    IdempotencyKey = $"refund-{refund.RefundId}"
                },
                transaction,
                cancellationToken: cancellationToken));

            await InsertRefundAuditAsync(connection, transaction, refund.RefundId, adminId, "PENDING", "APPROVED", reason, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return await QueryRefundAsync(connection, refund.RefundId, null, cancellationToken)
                ?? throw new ResourceNotFoundException("Refund does not exist.");
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<RefundSummary> RejectRefundAsync(AuthClaims claims, string refundId, AuditRefundRequest request, CancellationToken cancellationToken)
    {
        var adminId = NormalizeAdmin(claims);
        var normalizedRefundId = NormalizeRequired(refundId, nameof(refundId));
        var reason = NormalizeOptional(request?.Reason) ?? "Rejected.";

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            var refund = await LoadPendingRefundForUpdateAsync(connection, transaction, normalizedRefundId, cancellationToken);

            await connection.ExecuteAsync(new CommandDefinition(
                "update refund_ticket set status = 'REJECTED' where refund_id = :RefundId",
                new { refund.RefundId },
                transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                "update game_order set order_status = 'COMPLETED' where order_id = :OrderId and order_status = 'REFUNDING'",
                new { refund.OrderId },
                transaction,
                cancellationToken: cancellationToken));

            await InsertRefundAuditAsync(connection, transaction, refund.RefundId, adminId, "PENDING", "REJECTED", reason, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return await QueryRefundAsync(connection, refund.RefundId, null, cancellationToken)
                ?? throw new ResourceNotFoundException("Refund does not exist.");
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<CdkeyBatchSummary> CreateCdkeyBatchAsync(AuthClaims claims, CreateCdkeyBatchRequest request, CancellationToken cancellationToken)
    {
        NormalizeDeveloperOrAdmin(claims);
        ArgumentNullException.ThrowIfNull(request);
        var gameId = NormalizeRequired(request.GameId, nameof(request.GameId));
        var batchNo = NormalizeRequired(request.BatchNo, nameof(request.BatchNo));
        if (!string.Equals(gameId, "GAME_DST", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("CDKEY_GAME_UNSUPPORTED", "CDKey demo batches are only supported for DST.");
        }

        if (request.ExpireTime <= request.ValidFrom)
        {
            throw new ArgumentException("ExpireTime must be later than ValidFrom.");
        }

        if (request.Quantity is <= 0 or > 100)
        {
            throw new ArgumentException("Quantity must be between 1 and 100.");
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            _ = await LoadOnlineGameAsync(connection, transaction, gameId, cancellationToken);
            var batchId = IdGenerator.NewId("B");
            await connection.ExecuteAsync(new CommandDefinition(
                """
                insert into cdkey_batch
                  (batch_id, game_id, batch_no, valid_from, expire_time)
                values
                  (:BatchId, :GameId, :BatchNo, :ValidFrom, :ExpireTime)
                """,
                new { BatchId = batchId, GameId = gameId, BatchNo = batchNo, request.ValidFrom, request.ExpireTime },
                transaction,
                cancellationToken: cancellationToken));

            var keys = new List<string>(request.Quantity);
            for (var index = 0; index < request.Quantity; index++)
            {
                var plaintext = CreatePlaintextCdkey();
                keys.Add(plaintext);
                await connection.ExecuteAsync(new CommandDefinition(
                    """
                    insert into cdkey
                      (cdkey_hash, batch_id, status, generate_time)
                    values
                      (:CdkeyHash, :BatchId, 'AVAILABLE', SYSTIMESTAMP)
                    """,
                    new { CdkeyHash = HashCdkey(plaintext), BatchId = batchId },
                    transaction,
                    cancellationToken: cancellationToken));
            }

            await transaction.CommitAsync(cancellationToken);
            return new CdkeyBatchSummary(batchId, gameId, batchNo, request.ValidFrom, request.ExpireTime, keys);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<CdkeyRedeemResult> RedeemCdkeyAsync(AuthClaims claims, RedeemCdkeyRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = NormalizePrincipal(claims);
        var plaintext = NormalizeRequired(request.Cdkey, nameof(request.Cdkey));
        var submittedHash = HashCdkey(plaintext);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            var cdkey = await connection.QueryFirstOrDefaultAsync<CdkeyRow>(new CommandDefinition(
                """
                select c.cdkey_hash,
                       c.status,
                       b.batch_id,
                       b.game_id,
                       b.valid_from,
                       b.expire_time
                  from cdkey c
                  join cdkey_batch b on b.batch_id = c.batch_id
                 where c.cdkey_hash = :CdkeyHash
                 for update
                """,
                new { CdkeyHash = submittedHash },
                transaction,
                cancellationToken: cancellationToken));

            if (cdkey is null)
            {
                await InsertCdkeyRedeemLogAsync(connection, transaction, userId, submittedHash, null, "INVALID", "CDKey does not exist.", cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new CdkeyRedeemResult("INVALID", null, null, "CDKey does not exist.");
            }

            if (!string.Equals(cdkey.Status, "AVAILABLE", StringComparison.OrdinalIgnoreCase))
            {
                await InsertCdkeyRedeemLogAsync(connection, transaction, userId, submittedHash, cdkey.CdkeyHash, "REDEEMED", "CDKey has already been redeemed.", cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new CdkeyRedeemResult("REDEEMED", cdkey.GameId, null, "CDKey has already been redeemed.");
            }

            var now = DateTime.UtcNow;
            if (now < cdkey.ValidFrom || now > cdkey.ExpireTime)
            {
                await InsertCdkeyRedeemLogAsync(connection, transaction, userId, submittedHash, cdkey.CdkeyHash, "EXPIRED", "CDKey is outside its valid time window.", cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new CdkeyRedeemResult("EXPIRED", cdkey.GameId, null, "CDKey is outside its valid time window.");
            }

            await EnsureGameNotOwnedAsync(connection, transaction, userId, cdkey.GameId, cancellationToken);
            var libraryId = IdGenerator.NewId("LIB");
            await connection.ExecuteAsync(new CommandDefinition(
                """
                update cdkey
                   set status = 'REDEEMED'
                 where cdkey_hash = :CdkeyHash
                """,
                new { cdkey.CdkeyHash },
                transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                """
                insert into player_library
                  (lib_id, user_id, game_id, acquire_way, status, play_minutes, last_play_time)
                values
                  (:LibraryId, :UserId, :GameId, 'REDEEM', 'NORMAL', 0, null)
                """,
                new { LibraryId = libraryId, UserId = userId, cdkey.GameId },
                transaction,
                cancellationToken: cancellationToken));

            await InsertCdkeyRedeemLogAsync(connection, transaction, userId, submittedHash, cdkey.CdkeyHash, "SUCCESS", null, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new CdkeyRedeemResult("SUCCESS", cdkey.GameId, libraryId, "CDKey redeemed.");
        }
        catch (BusinessRuleException exception) when (exception.Code == "GAME_ALREADY_OWNED")
        {
            await InsertCdkeyRedeemLogAsync(connection, transaction, userId, submittedHash, submittedHash, "REDEEMED", "Player already owns this game.", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new CdkeyRedeemResult("REDEEMED", null, null, "Player already owns this game.");
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task InsertCompletedOrderAsync(
        DbConnection connection,
        DbTransaction transaction,
        string orderId,
        string userId,
        decimal totalAmount,
        string idempotencyKey,
        string detailId,
        GameRow game,
        decimal discountAmount,
        string paymentId,
        string createdLogId,
        string completedLogId,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into game_order
              (order_id, user_id, total_amount, order_type, order_status, payment_status, idempotency_key, expire_time, create_time)
            values
              (:OrderId, :UserId, :TotalAmount, 'BUY_GAME', 'COMPLETED', 'PAID', :IdempotencyKey, SYSTIMESTAMP, SYSTIMESTAMP)
            """,
            new { OrderId = orderId, UserId = userId, TotalAmount = totalAmount, IdempotencyKey = idempotencyKey },
            transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into order_detail
              (detail_id, order_id, game_id, original_price, discount_amount, payable_amount, refund_amount)
            values
              (:DetailId, :OrderId, :GameId, :OriginalPrice, :DiscountAmount, :PayableAmount, 0)
            """,
            new
            {
                DetailId = detailId,
                OrderId = orderId,
                game.GameId,
                OriginalPrice = game.BasePrice,
                DiscountAmount = discountAmount,
                PayableAmount = totalAmount
            },
            transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into payment_transaction
              (payment_id, order_id, provider_trade_no, amount, status, create_time)
            values
              (:PaymentId, :OrderId, :ProviderTradeNo, :Amount, 'SUCCESS', SYSTIMESTAMP)
            """,
            new { PaymentId = paymentId, OrderId = orderId, ProviderTradeNo = $"SIM-{orderId}", Amount = totalAmount },
            transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into order_status_log
              (log_id, order_id, from_status, to_status, create_time)
            values
              (:CreatedLogId, :OrderId, null, 'CREATED', SYSTIMESTAMP)
            """,
            new { CreatedLogId = createdLogId, OrderId = orderId },
            transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into order_status_log
              (log_id, order_id, from_status, to_status, create_time)
            values
              (:CompletedLogId, :OrderId, 'CREATED', 'COMPLETED', SYSTIMESTAMP)
            """,
            new { CompletedLogId = completedLogId, OrderId = orderId },
            transaction,
            cancellationToken: cancellationToken));
    }

    private static async Task<OrderSummary?> FindOrderByIdempotencyAsync(
        DbConnection connection,
        string userId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var orderId = await connection.QueryFirstOrDefaultAsync<string>(new CommandDefinition(
            """
            select order_id
              from game_order
             where user_id = :UserId
               and idempotency_key = :IdempotencyKey
            """,
            new { UserId = userId, IdempotencyKey = idempotencyKey },
            cancellationToken: cancellationToken));

        return orderId is null
            ? null
            : await QueryOrderAsync(connection, userId, orderId, null, cancellationToken);
    }

    private static async Task<WalletTransactionRow?> FindWalletTransactionByIdempotencyAsync(
        DbConnection connection,
        DbTransaction transaction,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return await connection.QueryFirstOrDefaultAsync<WalletTransactionRow>(new CommandDefinition(
            """
            select wallet_id,
                   txn_id,
                   biz_type,
                   biz_ref_id,
                   funds_direction,
                   amount,
                   avail_bal_before,
                   avail_bal_after,
                   idempotency_key,
                   create_time
              from wallet_transaction
             where idempotency_key = :IdempotencyKey
            """,
            new { IdempotencyKey = idempotencyKey },
            transaction,
            cancellationToken: cancellationToken));
    }

    private static async Task<OrderSummary?> QueryOrderAsync(
        DbConnection connection,
        string userId,
        string orderId,
        DbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        var order = await connection.QueryFirstOrDefaultAsync<OrderRow>(new CommandDefinition(
            """
            select order_id,
                   user_id,
                   total_amount,
                   order_type,
                   order_status,
                   payment_status,
                   idempotency_key,
                   create_time
              from game_order
             where order_id = :OrderId
               and user_id = :UserId
            """,
            new { OrderId = orderId, UserId = userId },
            transaction,
            cancellationToken: cancellationToken));

        if (order is null)
        {
            return null;
        }

        var details = await connection.QueryAsync<OrderDetailRow>(new CommandDefinition(
            """
            select od.detail_id,
                   od.game_id,
                   g.game_name,
                   od.original_price,
                   od.discount_amount,
                   od.payable_amount,
                   od.refund_amount
              from order_detail od
              join game g on g.game_id = od.game_id
             where od.order_id = :OrderId
             order by od.detail_id
            """,
            new { OrderId = orderId },
            transaction,
            cancellationToken: cancellationToken));

        return order.ToSummary(details.Select(detail => detail.ToEntry()).ToArray());
    }

    private static async Task<GameRow> LoadOnlineGameAsync(
        DbConnection connection,
        DbTransaction transaction,
        string gameId,
        CancellationToken cancellationToken)
    {
        var game = await connection.QueryFirstOrDefaultAsync<GameRow>(new CommandDefinition(
            """
            select game_id, game_name, base_price, discount_rate, status
              from game
             where game_id = :GameId
            """,
            new { GameId = gameId },
            transaction,
            cancellationToken: cancellationToken));

        if (game is null)
        {
            throw new ResourceNotFoundException("Game does not exist.");
        }

        if (!string.Equals(game.Status, "ONLINE", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("GAME_OFFLINE", "The game is not online.");
        }

        return game;
    }

    private static async Task EnsureGameNotOwnedAsync(
        DbConnection connection,
        DbTransaction transaction,
        string userId,
        string gameId,
        CancellationToken cancellationToken)
    {
        var owned = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            select count(*)
              from player_library
             where user_id = :UserId
               and game_id = :GameId
               and status = 'NORMAL'
            """,
            new { UserId = userId, GameId = gameId },
            transaction,
            cancellationToken: cancellationToken));

        if (owned > 0)
        {
            throw new BusinessRuleException("GAME_ALREADY_OWNED", "The player already owns this game.");
        }
    }

    private static async Task<WalletRow> LoadWalletForUpdateAsync(
        DbConnection connection,
        DbTransaction transaction,
        string userId,
        CancellationToken cancellationToken)
    {
        var wallet = await connection.QueryFirstOrDefaultAsync<WalletRow>(new CommandDefinition(
            """
            select wallet_id, user_id, available_balance, frozen_balance, version
              from wallet_account
             where user_id = :UserId
             for update
            """,
            new { UserId = userId },
            transaction,
            cancellationToken: cancellationToken));

        return wallet ?? throw new ResourceNotFoundException("Wallet does not exist.");
    }

    private static async Task<RefundSummary?> QueryRefundAsync(
        DbConnection connection,
        string refundId,
        string? userId,
        CancellationToken cancellationToken)
    {
        var row = await connection.QueryFirstOrDefaultAsync<RefundRow>(new CommandDefinition(
            """
            select rt.refund_id,
                   rt.order_id,
                   rt.refund_amount,
                   rt.refund_type,
                   rt.reason,
                   rt.play_time_hours,
                   rt.status,
                   rt.apply_time
              from refund_ticket rt
              join game_order go on go.order_id = rt.order_id
             where rt.refund_id = :RefundId
               and (:UserId is null or go.user_id = :UserId)
            """,
            new { RefundId = refundId, UserId = userId },
            cancellationToken: cancellationToken));

        return row?.ToSummary();
    }

    private static async Task<RefundRow> LoadPendingRefundForUpdateAsync(
        DbConnection connection,
        DbTransaction transaction,
        string refundId,
        CancellationToken cancellationToken)
    {
        var refund = await connection.QueryFirstOrDefaultAsync<RefundRow>(new CommandDefinition(
            """
            select refund_id,
                   order_id,
                   refund_amount,
                   refund_type,
                   reason,
                   play_time_hours,
                   status,
                   apply_time
              from refund_ticket
             where refund_id = :RefundId
             for update
            """,
            new { RefundId = refundId },
            transaction,
            cancellationToken: cancellationToken));

        if (refund is null)
        {
            throw new ResourceNotFoundException("Refund does not exist.");
        }

        if (!string.Equals(refund.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("REFUND_STATUS_INVALID", "Only pending refunds can be audited.");
        }

        return refund;
    }

    private static async Task InsertRefundAuditAsync(
        DbConnection connection,
        DbTransaction transaction,
        string refundId,
        string operatorId,
        string fromStatus,
        string toStatus,
        string reason,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into refund_audit_log
              (audit_id, refund_id, operator_id, from_status, to_status, reason, create_time)
            values
              (:AuditId, :RefundId, :OperatorId, :FromStatus, :ToStatus, :Reason, SYSTIMESTAMP)
            """,
            new
            {
                AuditId = IdGenerator.NewId("RA"),
                RefundId = refundId,
                OperatorId = operatorId,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                Reason = reason
            },
            transaction,
            cancellationToken: cancellationToken));
    }

    private static async Task InsertCdkeyRedeemLogAsync(
        DbConnection connection,
        DbTransaction transaction,
        string userId,
        string submittedHash,
        string? cdkeyHash,
        string result,
        string? failReason,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into cdkey_redeem_log
              (log_id, user_id, submitted_hash, cdkey_hash, result, fail_reason, ip_hash, create_time)
            values
              (:LogId, :UserId, :SubmittedHash, :CdkeyHash, :Result, :FailReason, null, SYSTIMESTAMP)
            """,
            new
            {
                LogId = IdGenerator.NewId("CRL"),
                UserId = userId,
                SubmittedHash = submittedHash,
                CdkeyHash = cdkeyHash,
                Result = result,
                FailReason = failReason
            },
            transaction,
            cancellationToken: cancellationToken));
    }

    private static decimal CalculatePayable(GameRow game) =>
        Math.Round(game.BasePrice * game.DiscountRate, 2, MidpointRounding.AwayFromZero);

    private static string NormalizePrincipal(AuthClaims claims)
    {
        ArgumentNullException.ThrowIfNull(claims);
        if (!string.Equals(claims.Role, "PLAYER", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Player role is required.");
        }

        return NormalizeRequired(claims.PrincipalId, nameof(AuthClaims.PrincipalId));
    }

    private static string NormalizeAdmin(AuthClaims claims)
    {
        ArgumentNullException.ThrowIfNull(claims);
        if (!AuthRoles.IsAdminRole(claims.Role))
        {
            throw new UnauthorizedAccessException("Admin role is required.");
        }

        return NormalizeRequired(claims.PrincipalId, nameof(AuthClaims.PrincipalId));
    }

    private static string NormalizeDeveloperOrAdmin(AuthClaims claims)
    {
        ArgumentNullException.ThrowIfNull(claims);
        if (!AuthRoles.IsAdminRole(claims.Role) &&
            !string.Equals(claims.Role, "DEVELOPER", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Developer or admin role is required.");
        }

        return NormalizeRequired(claims.PrincipalId, nameof(AuthClaims.PrincipalId));
    }

    private static string NormalizeRequired(string? value, string fieldName)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? throw new ArgumentException($"{fieldName} is required.")
            : normalized;
    }

    private static string NormalizeIdempotencyKey(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BusinessRuleException("IDEMPOTENCY_KEY_REQUIRED", "IdempotencyKey is required.");
        }

        return normalized.Length <= 64
            ? normalized
            : throw new BusinessRuleException("IDEMPOTENCY_CONFLICT", "IdempotencyKey must be 64 characters or fewer.");
    }

    private static decimal NormalizeRechargeAmount(decimal amount)
    {
        if (amount < RechargeMinAmount || amount > RechargeMaxAmount)
        {
            throw new BusinessRuleException("INVALID_AMOUNT", "Recharge amount must be between 0.01 and 99999.99.");
        }

        if (decimal.Round(amount, 2, MidpointRounding.AwayFromZero) != amount)
        {
            throw new BusinessRuleException("INVALID_AMOUNT", "Recharge amount can have at most two decimal places.");
        }

        return amount;
    }

    private static bool IsSameRecharge(WalletTransactionRow row, string walletId) =>
        string.Equals(row.WalletId, walletId, StringComparison.Ordinal) &&
        string.Equals(row.BizType, "RECHARGE", StringComparison.OrdinalIgnoreCase);

    private static bool IsOracleUniqueConstraintViolation(Exception exception) =>
        TryGetOracleErrorNumber(exception, out var oracleNumber) && oracleNumber == 1;

    private static bool TryGetOracleErrorNumber(Exception exception, out int number)
    {
        number = 0;
        if (exception.GetType().FullName != "Oracle.ManagedDataAccess.Client.OracleException")
        {
            return false;
        }

        var value = exception.GetType().GetProperty("Number")?.GetValue(exception);
        if (value is not int oracleNumber)
        {
            return false;
        }

        number = oracleNumber;
        return true;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string CreatePlaintextCdkey()
    {
        var random = Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
        return $"DST-{random[..4]}-{random[4..8]}-{random[8..12]}-{random[12..]}";
    }

    private static string HashCdkey(string plaintext)
    {
        var normalized = NormalizeRequired(plaintext, nameof(plaintext)).ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private sealed class OrderRefundRow
    {
        public string OrderId { get; set; } = "";
        public string UserId { get; set; } = "";
        public string OrderStatus { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
    }

    private sealed class RefundableDetailRow
    {
        public string DetailId { get; set; } = "";
        public decimal PayableAmount { get; set; }
        public decimal RefundAmount { get; set; }
    }

    private sealed class RefundRow
    {
        public string RefundId { get; set; } = "";
        public string OrderId { get; set; } = "";
        public decimal RefundAmount { get; set; }
        public string RefundType { get; set; } = "";
        public string Reason { get; set; } = "";
        public decimal PlayTimeHours { get; set; }
        public string Status { get; set; } = "";
        public DateTime ApplyTime { get; set; }

        public RefundSummary ToSummary() => new(RefundId, OrderId, RefundAmount, RefundType, Reason, PlayTimeHours, Status, ApplyTime);
    }

    private sealed class CdkeyRow
    {
        public string CdkeyHash { get; set; } = "";
        public string Status { get; set; } = "";
        public string BatchId { get; set; } = "";
        public string GameId { get; set; } = "";
        public DateTime ValidFrom { get; set; }
        public DateTime ExpireTime { get; set; }
    }

    private sealed class WalletRow
    {
        public string WalletId { get; set; } = "";
        public string UserId { get; set; } = "";
        public decimal AvailableBalance { get; set; }
        public decimal FrozenBalance { get; set; }
        public long Version { get; set; }

        public WalletSummary ToSummary() => new(WalletId, UserId, AvailableBalance, FrozenBalance, AvailableBalance + FrozenBalance, Version);
    }

    private sealed class WalletTransactionRow
    {
        public string WalletId { get; set; } = "";
        public string TxnId { get; set; } = "";
        public string BizType { get; set; } = "";
        public string BizRefId { get; set; } = "";
        public string FundsDirection { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal AvailBalBefore { get; set; }
        public decimal AvailBalAfter { get; set; }
        public string? IdempotencyKey { get; set; }
        public DateTime CreateTime { get; set; }

        public WalletTransactionEntry ToEntry() => new(TxnId, BizType, BizRefId, FundsDirection, Amount, AvailBalBefore, AvailBalAfter, IdempotencyKey, CreateTime);
    }

    private sealed class GameRow
    {
        public string GameId { get; set; } = "";
        public string GameName { get; set; } = "";
        public decimal BasePrice { get; set; }
        public decimal DiscountRate { get; set; }
        public string Status { get; set; } = "";
    }

    private sealed class OrderRow
    {
        public string OrderId { get; set; } = "";
        public string UserId { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public string OrderType { get; set; } = "";
        public string OrderStatus { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
        public string? IdempotencyKey { get; set; }
        public DateTime CreateTime { get; set; }

        public OrderSummary ToSummary(IReadOnlyList<OrderDetailEntry> details) =>
            new(OrderId, UserId, TotalAmount, OrderType, OrderStatus, PaymentStatus, IdempotencyKey, CreateTime, details);
    }

    private sealed class OrderDetailRow
    {
        public string DetailId { get; set; } = "";
        public string GameId { get; set; } = "";
        public string GameName { get; set; } = "";
        public decimal OriginalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal PayableAmount { get; set; }
        public decimal RefundAmount { get; set; }

        public OrderDetailEntry ToEntry() => new(DetailId, GameId, GameName, OriginalPrice, DiscountAmount, PayableAmount, RefundAmount);
    }

    private sealed class LibraryRow
    {
        public string LibId { get; set; } = "";
        public string GameId { get; set; } = "";
        public string GameName { get; set; } = "";
        public string AcquireWay { get; set; } = "";
        public string Status { get; set; } = "";
        public int PlayMinutes { get; set; }
        public DateTime? LastPlayTime { get; set; }

        public LibraryEntry ToEntry() => new(LibId, GameId, GameName, AcquireWay, Status, PlayMinutes, LastPlayTime);
    }
}
