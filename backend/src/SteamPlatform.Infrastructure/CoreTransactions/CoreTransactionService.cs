using System.Data;
using System.Data.Common;
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

    public async Task<IReadOnlyList<WalletTransactionEntry>> ListWalletTransactionsAsync(AuthClaims claims, int limit, CancellationToken cancellationToken)
    {
        var userId = NormalizePrincipal(claims);

        await using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<WalletTransactionRow>(new CommandDefinition(
            """
            select *
            from (
              select wt.txn_id,
                     wt.biz_type,
                     wt.biz_ref_id,
                     wt.funds_direction,
                     wt.amount,
                     wt.avail_bal_before,
                     wt.avail_bal_after,
                     wt.create_time
                from wallet_transaction wt
                join wallet_account wa on wa.wallet_id = wt.wallet_id
               where wa.user_id = :UserId
               order by wt.create_time desc, wt.txn_id desc
            )
            where rownum <= :Limit
            """,
            new { UserId = userId, Limit = Math.Clamp(limit, 1, 100) },
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToEntry()).ToArray();
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

    private static string NormalizeRequired(string? value, string fieldName)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? throw new ArgumentException($"{fieldName} is required.")
            : normalized;
    }

    private sealed class WalletRow
    {
        public string WalletId { get; set; } = "";
        public string UserId { get; set; } = "";
        public decimal AvailableBalance { get; set; }
        public decimal FrozenBalance { get; set; }
        public long Version { get; set; }

        public WalletSummary ToSummary() => new(WalletId, UserId, AvailableBalance, FrozenBalance, Version);
    }

    private sealed class WalletTransactionRow
    {
        public string TxnId { get; set; } = "";
        public string BizType { get; set; } = "";
        public string BizRefId { get; set; } = "";
        public string FundsDirection { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal AvailBalBefore { get; set; }
        public decimal AvailBalAfter { get; set; }
        public DateTime CreateTime { get; set; }

        public WalletTransactionEntry ToEntry() => new(TxnId, BizType, BizRefId, FundsDirection, Amount, AvailBalBefore, AvailBalAfter, CreateTime);
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
