using System.Data;
using System.Data.Common;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using SteamPlatform.Application.Market;
using SteamPlatform.Infrastructure.Data;
using SteamPlatform.Shared;

namespace SteamPlatform.Infrastructure.Market;

public sealed class MarketRepository : IMarketRepository
{
    private const decimal PlatformFeeRate = 0.05m;
    private readonly IDbConnectionFactory _connectionFactory;

    public MarketRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<MarketListingDto>> GetListingsAsync(string? gameId, CancellationToken cancellationToken)
    {
        var sql = """
            SELECT
              t.template_id AS TemplateId,
              t.game_id AS GameId,
              t.item_name AS ItemName,
              t.rarity AS Rarity,
              t.image_url AS ImageUrl,
              MAX(CASE WHEN o.order_type = 'BUY' AND o.status = 'MATCHING' THEN o.target_price END) AS HighestBuyPrice,
              MIN(CASE WHEN o.order_type = 'SELL' AND o.status = 'MATCHING' THEN o.target_price END) AS LowestSellPrice,
              SUM(CASE WHEN o.order_type = 'BUY' AND o.status = 'MATCHING' THEN 1 ELSE 0 END) AS ActiveBuyCount,
              SUM(CASE WHEN o.order_type = 'SELL' AND o.status = 'MATCHING' THEN 1 ELSE 0 END) AS ActiveSellCount
            FROM ITEM_TEMPLATE t
            LEFT JOIN MARKET_ORDER o ON o.template_id = t.template_id
            WHERE (:GameId IS NULL OR t.game_id = :GameId)
            GROUP BY t.template_id, t.game_id, t.item_name, t.rarity, t.image_url
            ORDER BY t.item_name
            """;

        await using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<MarketListingDto>(
            new CommandDefinition(sql, new { GameId = gameId }, cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<MarketOrderDto>> GetOrdersAsync(string? userId, CancellationToken cancellationToken)
    {
        var sql = """
            SELECT
              o.market_order_id AS MarketOrderId,
              o.user_id AS UserId,
              o.template_id AS TemplateId,
              t.item_name AS ItemName,
              o.order_type AS OrderType,
              o.item_id AS ItemId,
              o.target_price AS TargetPrice,
              o.frozen_amount AS FrozenAmount,
              o.status AS Status,
              o.create_time AS CreateTime
            FROM MARKET_ORDER o
            JOIN ITEM_TEMPLATE t ON t.template_id = o.template_id
            """;

        object? parameters = null;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            sql += " WHERE o.user_id = :UserId";
            parameters = new { UserId = userId };
        }

        sql += " ORDER BY o.create_time DESC";

        await using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<MarketOrderDto>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<MarketTradeDto>> GetTradesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
              tr.trade_id AS TradeId,
              tr.buy_order_id AS BuyOrderId,
              tr.sell_order_id AS SellOrderId,
              tr.template_id AS TemplateId,
              t.item_name AS ItemName,
              tr.item_id AS ItemId,
              tr.buyer_id AS BuyerId,
              tr.seller_id AS SellerId,
              tr.trade_price AS TradePrice,
              tr.platform_fee AS PlatformFee,
              tr.currency AS Currency,
              tr.trade_time AS TradeTime
            FROM MARKET_TRADE tr
            JOIN ITEM_TEMPLATE t ON t.template_id = tr.template_id
            ORDER BY tr.trade_time DESC
            """;

        await using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<MarketTradeDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ItemTransferDto>> GetItemTransfersAsync(string itemId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
              l.transfer_id AS TransferId,
              l.item_id AS ItemId,
              t.item_name AS ItemName,
              l.from_user_id AS FromUserId,
              l.to_user_id AS ToUserId,
              l.transfer_type AS TransferType,
              l.transfer_time AS TransferTime
            FROM ITEM_TRANSFER_LEDGER l
            JOIN INVENTORY_ITEM i ON i.item_id = l.item_id
            JOIN ITEM_TEMPLATE t ON t.template_id = i.template_id
            WHERE l.item_id = :ItemId
            ORDER BY l.transfer_time DESC
            """;

        await using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<ItemTransferDto>(
            new CommandDefinition(sql, new { ItemId = itemId }, cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task<MarketOrderDto> CreateBuyOrderAsync(string userId, CreateMarketOrderRequest request, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            await EnsureTemplateExistsAsync(connection, transaction, request.TemplateId, cancellationToken);
            var wallet = await GetWalletForUpdateAsync(connection, transaction, userId, cancellationToken);

            if (wallet.AvailableBalance < request.TargetPrice)
            {
                throw new BusinessRuleException("INSUFFICIENT_BALANCE", "可用余额不足，无法创建买单。");
            }

            var orderId = NewId();
            var availableAfter = wallet.AvailableBalance - request.TargetPrice;
            var frozenAfter = wallet.FrozenBalance + request.TargetPrice;

            await connection.ExecuteAsync(new CommandDefinition("""
                UPDATE WALLET_ACCOUNT
                SET available_balance = :AvailableBalance,
                    frozen_balance = :FrozenBalance,
                    version = version + 1
                WHERE wallet_id = :WalletId
                """, new
                {
                    AvailableBalance = availableAfter,
                    FrozenBalance = frozenAfter,
                    wallet.WalletId
                }, transaction, cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO MARKET_ORDER
                  (market_order_id, user_id, template_id, item_id, order_type, target_price, frozen_amount, status, create_time)
                VALUES
                  (:MarketOrderId, :UserId, :TemplateId, NULL, 'BUY', :TargetPrice, :FrozenAmount, 'MATCHING', SYSTIMESTAMP)
                """, new
                {
                    MarketOrderId = orderId,
                    UserId = userId,
                    request.TemplateId,
                    request.TargetPrice,
                    FrozenAmount = request.TargetPrice
                }, transaction, cancellationToken: cancellationToken));

            await InsertWalletTransactionAsync(
                connection,
                transaction,
                wallet.WalletId,
                "MARKET_FREEZE",
                orderId,
                "FREEZE",
                request.TargetPrice,
                wallet.AvailableBalance,
                availableAfter,
                $"market-freeze-{orderId}",
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return await GetOrderByIdAsync(orderId, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<MarketOrderDto> CreateSellOrderAsync(string userId, CreateMarketOrderRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ItemId))
        {
            throw new BusinessRuleException("ITEM_ID_REQUIRED", "卖单必须选择要上架的饰品。");
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            var item = await connection.QuerySingleOrDefaultAsync<InventoryItemRow>(new CommandDefinition("""
                SELECT
                  item_id AS ItemId,
                  template_id AS TemplateId,
                  user_id AS UserId,
                  status AS Status
                FROM INVENTORY_ITEM
                WHERE item_id = :ItemId
                FOR UPDATE
                """, new { ItemId = request.ItemId }, transaction, cancellationToken: cancellationToken));

            if (item is null)
            {
                throw new BusinessRuleException("ITEM_NOT_FOUND", "饰品不存在。");
            }

            if (!string.Equals(item.UserId, userId, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("ITEM_NOT_OWNED", "只能上架当前玩家自己的饰品。");
            }

            if (!string.Equals(item.TemplateId, request.TemplateId, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("ITEM_TEMPLATE_MISMATCH", "饰品模板与挂单模板不一致。");
            }

            if (item.Status != "NORMAL")
            {
                throw new BusinessRuleException("ITEM_NOT_SELLABLE", "只有 NORMAL 状态的饰品可以上架。");
            }

            var orderId = NewId();

            await connection.ExecuteAsync(new CommandDefinition("""
                UPDATE INVENTORY_ITEM
                SET status = 'IN_MARKET',
                    version = version + 1
                WHERE item_id = :ItemId
                """, new { item.ItemId }, transaction, cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO MARKET_ORDER
                  (market_order_id, user_id, template_id, item_id, order_type, target_price, frozen_amount, status, create_time)
                VALUES
                  (:MarketOrderId, :UserId, :TemplateId, :ItemId, 'SELL', :TargetPrice, 0, 'MATCHING', SYSTIMESTAMP)
                """, new
                {
                    MarketOrderId = orderId,
                    UserId = userId,
                    request.TemplateId,
                    item.ItemId,
                    request.TargetPrice
                }, transaction, cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
            return await GetOrderByIdAsync(orderId, cancellationToken);
        }
        catch (OracleException ex) when (ex.Number == 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new BusinessRuleException("ACTIVE_SELL_ORDER_EXISTS", "同一饰品同一时刻只能存在一个有效卖单。");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task CancelOrderAsync(string userId, string marketOrderId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            var order = await GetOrderRowForUpdateAsync(connection, transaction, marketOrderId, cancellationToken);
            if (order is null)
            {
                throw new BusinessRuleException("ORDER_NOT_FOUND", "市场挂单不存在。");
            }

            if (!string.Equals(order.UserId, userId, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("ORDER_NOT_OWNED", "只能取消自己的市场挂单。");
            }

            if (order.Status != "MATCHING")
            {
                throw new BusinessRuleException("ORDER_NOT_CANCELABLE", "只有撮合中的挂单可以取消。");
            }

            if (order.OrderType == "BUY")
            {
                var wallet = await GetWalletForUpdateAsync(connection, transaction, userId, cancellationToken);
                if (wallet.FrozenBalance < order.FrozenAmount)
                {
                    throw new BusinessRuleException("WALLET_FROZEN_INCONSISTENT", "冻结余额不足，无法取消买单。");
                }

                var availableAfter = wallet.AvailableBalance + order.FrozenAmount;
                var frozenAfter = wallet.FrozenBalance - order.FrozenAmount;

                await connection.ExecuteAsync(new CommandDefinition("""
                    UPDATE WALLET_ACCOUNT
                    SET available_balance = :AvailableBalance,
                        frozen_balance = :FrozenBalance,
                        version = version + 1
                    WHERE wallet_id = :WalletId
                    """, new
                    {
                        AvailableBalance = availableAfter,
                        FrozenBalance = frozenAfter,
                        wallet.WalletId
                    }, transaction, cancellationToken: cancellationToken));

                await InsertWalletTransactionAsync(
                    connection,
                    transaction,
                    wallet.WalletId,
                    "MARKET_UNFREEZE",
                    marketOrderId,
                    "UNFREEZE",
                    order.FrozenAmount,
                    wallet.AvailableBalance,
                    availableAfter,
                    $"market-unfreeze-{marketOrderId}",
                    cancellationToken);
            }
            else
            {
                await connection.ExecuteAsync(new CommandDefinition("""
                    UPDATE INVENTORY_ITEM
                    SET status = 'NORMAL',
                        version = version + 1
                    WHERE item_id = :ItemId
                    """, new { order.ItemId }, transaction, cancellationToken: cancellationToken));
            }

            await connection.ExecuteAsync(new CommandDefinition("""
                UPDATE MARKET_ORDER
                SET status = 'CANCELED'
                WHERE market_order_id = :MarketOrderId
                """, new { MarketOrderId = marketOrderId }, transaction, cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<MarketTradeDto> MatchAsync(MatchMarketRequest request, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            var match = await connection.QuerySingleOrDefaultAsync<MatchCandidateRow>(new CommandDefinition("""
                SELECT *
                FROM (
                  SELECT
                    b.market_order_id AS BuyOrderId,
                    s.market_order_id AS SellOrderId,
                    b.user_id AS BuyerId,
                    s.user_id AS SellerId,
                    b.template_id AS TemplateId,
                    s.item_id AS ItemId,
                    b.target_price AS BuyPrice,
                    s.target_price AS SellPrice,
                    b.frozen_amount AS FrozenAmount
                  FROM MARKET_ORDER b
                  JOIN MARKET_ORDER s
                    ON s.template_id = b.template_id
                   AND s.order_type = 'SELL'
                   AND s.status = 'MATCHING'
                   AND s.target_price <= b.target_price
                  WHERE b.order_type = 'BUY'
                    AND b.status = 'MATCHING'
                    AND (:TemplateId IS NULL OR b.template_id = :TemplateId)
                  ORDER BY s.target_price ASC, b.target_price DESC, s.create_time ASC, b.create_time ASC
                )
                WHERE ROWNUM = 1
                """, new { request.TemplateId }, transaction, cancellationToken: cancellationToken));

            if (match is null)
            {
                throw new BusinessRuleException("NO_MATCHING_ORDER", "当前没有可撮合的买单和卖单。");
            }

            var buyOrder = await GetOrderRowForUpdateAsync(connection, transaction, match.BuyOrderId, cancellationToken)
                ?? throw new BusinessRuleException("BUY_ORDER_NOT_FOUND", "买单不存在。");
            var sellOrder = await GetOrderRowForUpdateAsync(connection, transaction, match.SellOrderId, cancellationToken)
                ?? throw new BusinessRuleException("SELL_ORDER_NOT_FOUND", "卖单不存在。");

            if (buyOrder.Status != "MATCHING" || sellOrder.Status != "MATCHING")
            {
                throw new BusinessRuleException("ORDER_STATUS_CHANGED", "挂单状态已变化，请重新撮合。");
            }

            var item = await connection.QuerySingleAsync<InventoryItemRow>(new CommandDefinition("""
                SELECT
                  item_id AS ItemId,
                  template_id AS TemplateId,
                  user_id AS UserId,
                  status AS Status
                FROM INVENTORY_ITEM
                WHERE item_id = :ItemId
                FOR UPDATE
                """, new { sellOrder.ItemId }, transaction, cancellationToken: cancellationToken));

            if (item.Status != "IN_MARKET" || item.UserId != sellOrder.UserId)
            {
                throw new BusinessRuleException("ITEM_STATUS_CHANGED", "卖单饰品状态已变化，请重新撮合。");
            }

            var buyerWallet = await GetWalletForUpdateAsync(connection, transaction, buyOrder.UserId, cancellationToken);
            var sellerWallet = await GetWalletForUpdateAsync(connection, transaction, sellOrder.UserId, cancellationToken);

            var tradePrice = sellOrder.TargetPrice;
            if (buyerWallet.FrozenBalance < tradePrice || buyOrder.FrozenAmount < tradePrice)
            {
                throw new BusinessRuleException("FROZEN_BALANCE_INSUFFICIENT", "买单冻结资金不足。");
            }

            var platformFee = decimal.Round(tradePrice * PlatformFeeRate, 2, MidpointRounding.AwayFromZero);
            var sellerIncome = tradePrice - platformFee;
            var buyerRefund = buyOrder.FrozenAmount - tradePrice;
            var buyerAvailableAfter = buyerWallet.AvailableBalance + buyerRefund;
            var buyerFrozenAfter = buyerWallet.FrozenBalance - buyOrder.FrozenAmount;
            var sellerAvailableAfter = sellerWallet.AvailableBalance + sellerIncome;
            var tradeId = NewId();
            var transferId = NewId();

            await connection.ExecuteAsync(new CommandDefinition("""
                UPDATE WALLET_ACCOUNT
                SET available_balance = :AvailableBalance,
                    frozen_balance = :FrozenBalance,
                    version = version + 1
                WHERE wallet_id = :WalletId
                """, new
                {
                    AvailableBalance = buyerAvailableAfter,
                    FrozenBalance = buyerFrozenAfter,
                    buyerWallet.WalletId
                }, transaction, cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition("""
                UPDATE WALLET_ACCOUNT
                SET available_balance = :AvailableBalance,
                    version = version + 1
                WHERE wallet_id = :WalletId
                """, new
                {
                    AvailableBalance = sellerAvailableAfter,
                    sellerWallet.WalletId
                }, transaction, cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition("""
                UPDATE INVENTORY_ITEM
                SET user_id = :BuyerId,
                    status = 'NORMAL',
                    version = version + 1
                WHERE item_id = :ItemId
                """, new
                {
                    BuyerId = buyOrder.UserId,
                    sellOrder.ItemId
                }, transaction, cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition("""
                UPDATE MARKET_ORDER
                SET status = 'TRADED',
                    frozen_amount = CASE WHEN order_type = 'BUY' THEN 0 ELSE frozen_amount END
                WHERE market_order_id IN (:BuyOrderId, :SellOrderId)
                """, new
                {
                    buyOrder.BuyOrderId,
                    sellOrder.SellOrderId
                }, transaction, cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO MARKET_TRADE
                  (trade_id, buy_order_id, sell_order_id, template_id, item_id, buyer_id, seller_id, trade_price, platform_fee, currency, trade_time)
                VALUES
                  (:TradeId, :BuyOrderId, :SellOrderId, :TemplateId, :ItemId, :BuyerId, :SellerId, :TradePrice, :PlatformFee, 'CNY', SYSTIMESTAMP)
                """, new
                {
                    TradeId = tradeId,
                    BuyOrderId = buyOrder.BuyOrderId,
                    SellOrderId = sellOrder.SellOrderId,
                    buyOrder.TemplateId,
                    sellOrder.ItemId,
                    BuyerId = buyOrder.UserId,
                    SellerId = sellOrder.UserId,
                    TradePrice = tradePrice,
                    PlatformFee = platformFee
                }, transaction, cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO ITEM_TRANSFER_LEDGER
                  (transfer_id, item_id, from_user_id, to_user_id, transfer_type, transfer_time)
                VALUES
                  (:TransferId, :ItemId, :FromUserId, :ToUserId, 'TRADE', SYSTIMESTAMP)
                """, new
                {
                    TransferId = transferId,
                    sellOrder.ItemId,
                    FromUserId = sellOrder.UserId,
                    ToUserId = buyOrder.UserId
                }, transaction, cancellationToken: cancellationToken));

            await InsertWalletTransactionAsync(
                connection,
                transaction,
                buyerWallet.WalletId,
                "MARKET_BUY",
                tradeId,
                "DEBIT",
                tradePrice,
                buyerWallet.AvailableBalance,
                buyerAvailableAfter,
                $"market-buy-{tradeId}",
                cancellationToken);

            await InsertWalletTransactionAsync(
                connection,
                transaction,
                sellerWallet.WalletId,
                "MARKET_SELL",
                tradeId,
                "CREDIT",
                sellerIncome,
                sellerWallet.AvailableBalance,
                sellerAvailableAfter,
                $"market-sell-{tradeId}",
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return await GetTradeByIdAsync(tradeId, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<MarketOrderDto> GetOrderByIdAsync(string orderId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
              o.market_order_id AS MarketOrderId,
              o.user_id AS UserId,
              o.template_id AS TemplateId,
              t.item_name AS ItemName,
              o.order_type AS OrderType,
              o.item_id AS ItemId,
              o.target_price AS TargetPrice,
              o.frozen_amount AS FrozenAmount,
              o.status AS Status,
              o.create_time AS CreateTime
            FROM MARKET_ORDER o
            JOIN ITEM_TEMPLATE t ON t.template_id = o.template_id
            WHERE o.market_order_id = :OrderId
            """;

        await using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<MarketOrderDto>(
            new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken));
    }

    private async Task<MarketTradeDto> GetTradeByIdAsync(string tradeId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
              tr.trade_id AS TradeId,
              tr.buy_order_id AS BuyOrderId,
              tr.sell_order_id AS SellOrderId,
              tr.template_id AS TemplateId,
              t.item_name AS ItemName,
              tr.item_id AS ItemId,
              tr.buyer_id AS BuyerId,
              tr.seller_id AS SellerId,
              tr.trade_price AS TradePrice,
              tr.platform_fee AS PlatformFee,
              tr.currency AS Currency,
              tr.trade_time AS TradeTime
            FROM MARKET_TRADE tr
            JOIN ITEM_TEMPLATE t ON t.template_id = tr.template_id
            WHERE tr.trade_id = :TradeId
            """;

        await using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<MarketTradeDto>(
            new CommandDefinition(sql, new { TradeId = tradeId }, cancellationToken: cancellationToken));
    }

    private static async Task EnsureTemplateExistsAsync(
        DbConnection connection,
        IDbTransaction transaction,
        string templateId,
        CancellationToken cancellationToken)
    {
        var exists = await connection.ExecuteScalarAsync<int>(new CommandDefinition("""
            SELECT COUNT(1)
            FROM ITEM_TEMPLATE
            WHERE template_id = :TemplateId
            """, new { TemplateId = templateId }, transaction, cancellationToken: cancellationToken));

        if (exists == 0)
        {
            throw new BusinessRuleException("ITEM_TEMPLATE_NOT_FOUND", "饰品模板不存在。");
        }
    }

    private static async Task<WalletRow> GetWalletForUpdateAsync(
        DbConnection connection,
        IDbTransaction transaction,
        string userId,
        CancellationToken cancellationToken)
    {
        var wallet = await connection.QuerySingleOrDefaultAsync<WalletRow>(new CommandDefinition("""
            SELECT
              wallet_id AS WalletId,
              user_id AS UserId,
              available_balance AS AvailableBalance,
              frozen_balance AS FrozenBalance
            FROM WALLET_ACCOUNT
            WHERE user_id = :UserId
            FOR UPDATE
            """, new { UserId = userId }, transaction, cancellationToken: cancellationToken));

        return wallet ?? throw new BusinessRuleException("WALLET_NOT_FOUND", "当前玩家钱包不存在。");
    }

    private static async Task<MarketOrderRow?> GetOrderRowForUpdateAsync(
        DbConnection connection,
        IDbTransaction transaction,
        string marketOrderId,
        CancellationToken cancellationToken)
    {
        return await connection.QuerySingleOrDefaultAsync<MarketOrderRow>(new CommandDefinition("""
            SELECT
              market_order_id AS MarketOrderId,
              market_order_id AS BuyOrderId,
              market_order_id AS SellOrderId,
              user_id AS UserId,
              template_id AS TemplateId,
              item_id AS ItemId,
              order_type AS OrderType,
              target_price AS TargetPrice,
              frozen_amount AS FrozenAmount,
              status AS Status
            FROM MARKET_ORDER
            WHERE market_order_id = :MarketOrderId
            FOR UPDATE
            """, new { MarketOrderId = marketOrderId }, transaction, cancellationToken: cancellationToken));
    }

    private static Task InsertWalletTransactionAsync(
        DbConnection connection,
        IDbTransaction transaction,
        string walletId,
        string bizType,
        string bizRefId,
        string fundsDirection,
        decimal amount,
        decimal availableBefore,
        decimal availableAfter,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO WALLET_TRANSACTION
              (txn_id, wallet_id, biz_type, biz_ref_id, funds_direction, amount, avail_bal_before, avail_bal_after, idempotency_key, create_time)
            VALUES
              (:TxnId, :WalletId, :BizType, :BizRefId, :FundsDirection, :Amount, :AvailableBefore, :AvailableAfter, :IdempotencyKey, SYSTIMESTAMP)
            """, new
            {
                TxnId = NewId(),
                WalletId = walletId,
                BizType = bizType,
                BizRefId = bizRefId,
                FundsDirection = fundsDirection,
                Amount = amount,
                AvailableBefore = availableBefore,
                AvailableAfter = availableAfter,
                IdempotencyKey = idempotencyKey
            }, transaction, cancellationToken: cancellationToken));
    }

    private static string NewId()
    {
        return Guid.NewGuid().ToString("N").ToUpperInvariant();
    }

    private sealed class WalletRow
    {
        public string WalletId { get; init; } = "";
        public string UserId { get; init; } = "";
        public decimal AvailableBalance { get; init; }
        public decimal FrozenBalance { get; init; }
    }

    private sealed class InventoryItemRow
    {
        public string ItemId { get; init; } = "";
        public string TemplateId { get; init; } = "";
        public string UserId { get; init; } = "";
        public string Status { get; init; } = "";
    }

    private sealed class MarketOrderRow
    {
        public string MarketOrderId { get; init; } = "";
        public string BuyOrderId { get; init; } = "";
        public string SellOrderId { get; init; } = "";
        public string UserId { get; init; } = "";
        public string TemplateId { get; init; } = "";
        public string? ItemId { get; init; }
        public string OrderType { get; init; } = "";
        public decimal TargetPrice { get; init; }
        public decimal FrozenAmount { get; init; }
        public string Status { get; init; } = "";
    }

    private sealed class MatchCandidateRow
    {
        public string BuyOrderId { get; init; } = "";
        public string SellOrderId { get; init; } = "";
        public string BuyerId { get; init; } = "";
        public string SellerId { get; init; } = "";
        public string TemplateId { get; init; } = "";
        public string ItemId { get; init; } = "";
        public decimal BuyPrice { get; init; }
        public decimal SellPrice { get; init; }
        public decimal FrozenAmount { get; init; }
    }
}
