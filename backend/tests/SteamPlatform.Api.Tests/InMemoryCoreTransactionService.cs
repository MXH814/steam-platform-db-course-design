using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SteamPlatform.Application.Auth;
using SteamPlatform.Application.CoreTransactions;
using SteamPlatform.Domain.Notices;
using SteamPlatform.Shared;

namespace SteamPlatform.Infrastructure.CoreTransactions;

// Minimal in-memory implementation for local development and unit tests.
// Simulates real database concurrency semantics without using locks.
public sealed class InMemoryCoreTransactionService : ICoreTransactionService
{
    private readonly ConcurrentDictionary<string, decimal> _wallets = new();
    private readonly ConcurrentQueue<WalletTransactionRecord> _walletTransactions = new();
    private readonly ConcurrentDictionary<(string userId, string gameId), byte> _library = new();
    private readonly ConcurrentDictionary<string, OrderRecord> _orders = new();
    private readonly ConcurrentDictionary<string, RefundRecord> _refunds = new();
    private readonly ConcurrentDictionary<string, CdkeyRecord> _cdkeys = new();
    private readonly ConcurrentDictionary<string, CdkeyBatchRecord> _batches = new();

    public InMemoryCoreTransactionService()
    {
        // seed wallets for P001 and P002 for tests/demo
        _wallets.TryAdd("P001", 100m);
        _wallets.TryAdd("P002", 10m);
        // seed P001 owns DST in demo data
        _library.TryAdd(("P001", "GAME_DST"), 0);
        _library.TryAdd(("P002", "GAME_DST"), 0);
        // seed an existing completed paid order for P001 for refund demo
        var orderId = "O_DST_001";
        _orders.TryAdd(orderId, new OrderRecord
        {
            OrderId = orderId,
            UserId = "P001",
            TotalAmount = 50m,
            OrderStatus = "COMPLETED",
            PaymentStatus = "PAID",
            IdempotencyKey = "seed-001",
            PaymentMethod = PaymentMethods.SteamWallet,
            CreateTime = DateTime.UtcNow.AddDays(-1),
            Details = new List<OrderDetailRecord>
            {
                new OrderDetailRecord { DetailId = "OD_DST_001", GameId = "GAME_DST", OriginalPrice = 100m, DiscountAmount = 50m, PayableAmount = 50m, RefundAmount = 0m }
            }
        });
    }

    private static void EnsurePlayer(AuthClaims claims)
    {
        if (claims is null) throw new UnauthorizedAccessException();
    }

    public Task<WalletSummary> GetWalletAsync(AuthClaims claims, CancellationToken cancellationToken)
    {
        EnsurePlayer(claims);
        var userId = claims.PrincipalId;
        if (!_wallets.TryGetValue(userId, out var bal)) throw new ResourceNotFoundException("Wallet does not exist.");
        return Task.FromResult(new WalletSummary($"W-{userId}", userId, bal, 0m, bal, 1));
    }

    public Task<RechargeWalletResult> RechargeWalletAsync(AuthClaims claims, RechargeWalletRequest request, CancellationToken cancellationToken)
    {
        EnsurePlayer(claims);
        ArgumentNullException.ThrowIfNull(request);

        var userId = claims.PrincipalId;
        var walletId = $"W-{userId}";
        var amount = NormalizeRechargeAmount(request.Amount);
        var idempotencyKey = NormalizeRechargeIdempotencyKey(request.IdempotencyKey);
        var paymentMethod = NormalizeExternalPaymentMethod(request.PaymentMethod);

        var existing = _walletTransactions.FirstOrDefault(t =>
            string.Equals(t.Entry.IdempotencyKey, idempotencyKey, StringComparison.Ordinal));
        if (existing is not null)
        {
            if (!string.Equals(existing.WalletId, walletId, StringComparison.Ordinal)
                || !string.Equals(existing.Entry.BizType, "RECHARGE", StringComparison.OrdinalIgnoreCase)
                || existing.Entry.Amount != amount
                || !string.Equals(existing.Entry.PaymentMethod, paymentMethod, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("IDEMPOTENCY_CONFLICT", "IdempotencyKey is already used by another wallet transaction.");
            }

            return Task.FromResult(new RechargeWalletResult(
                walletId,
                existing.Entry.TxnId,
                existing.Entry.AvailBalAfter,
                0m,
                existing.Entry.AvailBalAfter));
        }

        if (!_wallets.TryGetValue(userId, out var before)) throw new ResourceNotFoundException("Wallet does not exist.");

        var after = before + amount;
        _wallets.TryUpdate(userId, after, before);

        var txnId = "WTN" + Guid.NewGuid().ToString("N")[..8];
        var entry = new WalletTransactionEntry(
            txnId,
            "RECHARGE",
            txnId,
            "CREDIT",
            amount,
            before,
            after,
            idempotencyKey,
            paymentMethod,
            DateTime.UtcNow);
        _walletTransactions.Enqueue(new WalletTransactionRecord(walletId, entry));

        return Task.FromResult(new RechargeWalletResult(walletId, txnId, after, 0m, after));
    }

    public Task<PagedResponse<WalletTransactionEntry>> ListWalletTransactionsAsync(AuthClaims claims, int page, int pageSize, CancellationToken cancellationToken)
    {
        EnsurePlayer(claims);
        var userId = claims.PrincipalId;
        var walletId = $"W-{userId}";
        if (!_wallets.ContainsKey(userId)) throw new ResourceNotFoundException("Wallet does not exist.");

        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var transactions = _walletTransactions.ToArray()
            .Where(t => string.Equals(t.WalletId, walletId, StringComparison.Ordinal))
            .Select(t => t.Entry)
            .OrderByDescending(t => t.CreateTime)
            .ThenByDescending(t => t.TxnId)
            .ToArray();
        var items = transactions
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToArray();

        return Task.FromResult(new PagedResponse<WalletTransactionEntry>(items, normalizedPage, normalizedPageSize, transactions.Length));
    }

    public Task<PagedResponse<WalletHistoryEntry>> ListWalletHistoryAsync(AuthClaims claims, int page, int pageSize, CancellationToken cancellationToken)
    {
        EnsurePlayer(claims);
        var userId = claims.PrincipalId;
        var walletId = $"W-{userId}";
        if (!_wallets.ContainsKey(userId)) throw new ResourceNotFoundException("Wallet does not exist.");

        var walletRows = _walletTransactions
            .Where(transaction => transaction.WalletId == walletId)
            .Select(transaction => new WalletHistoryEntry(
                $"WALLET-{transaction.Entry.TxnId}",
                transaction.Entry.BizType,
                transaction.Entry.CreateTime,
                transaction.Entry.BizType == "RECHARGE" ? "Steam 钱包充值" : transaction.Entry.BizType,
                transaction.Entry.PaymentMethod ?? PaymentMethods.SteamWallet,
                transaction.Entry.Amount,
                0m,
                0m,
                transaction.Entry.Amount,
                transaction.Entry.FundsDirection == "CREDIT" ? transaction.Entry.Amount : -transaction.Entry.Amount,
                transaction.Entry.AvailBalAfter,
                transaction.Entry.BizType == "BUY_GAME" ? transaction.Entry.BizRefId : null,
                null,
                transaction.Entry.BizType == "REFUND" ? transaction.Entry.BizRefId : null,
                transaction.Entry.TxnId));

        var orderRows = _orders.Values
            .Where(order => order.UserId == userId)
            .SelectMany(order => order.Details.Select(detail => new WalletHistoryEntry(
                $"ORDER-{detail.DetailId}",
                "BUY_GAME",
                order.CreateTime,
                detail.GameId == "GAME_DST" ? "Don't Starve Together" : "Counter-Strike 2",
                order.PaymentMethod,
                detail.OriginalPrice,
                detail.DiscountAmount,
                detail.OriginalPrice > 0 ? decimal.Round(detail.DiscountAmount / detail.OriginalPrice, 4) : 0m,
                detail.PayableAmount,
                order.PaymentMethod == PaymentMethods.SteamWallet ? -detail.PayableAmount : null,
                null,
                order.OrderId,
                detail.DetailId,
                null,
                null)));

        var refundRows = _refunds.Values
            .Where(refund => _orders.TryGetValue(refund.OrderId, out var order) && order.UserId == userId)
            .Select(refund => new WalletHistoryEntry(
                $"REFUND-{refund.RefundId}",
                "REFUND",
                refund.ApplyTime,
                "退款",
                _orders[refund.OrderId].PaymentMethod,
                refund.RefundAmount,
                0m,
                0m,
                refund.RefundAmount,
                refund.Status == "APPROVED" && _orders[refund.OrderId].PaymentMethod == PaymentMethods.SteamWallet ? refund.RefundAmount : null,
                null,
                refund.OrderId,
                refund.OrderDetailId,
                refund.RefundId,
                null));

        var orderedGames = _orders.Values
            .Where(order => order.UserId == userId)
            .SelectMany(order => order.Details.Select(detail => detail.GameId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var libraryRows = _library.Keys
            .Where(library => library.userId == userId && !orderedGames.Contains(library.gameId))
            .Select(library => new WalletHistoryEntry(
                $"LIBRARY-{library.userId}-{library.gameId}",
                library.userId == "P002" && library.gameId == "GAME_DST" ? "CDKEY_REDEEM" : "LIBRARY_IMPORT",
                DateTime.UtcNow.AddDays(-2),
                library.gameId == "GAME_DST" ? "Don't Starve Together" : "Counter-Strike 2",
                library.userId == "P002" && library.gameId == "GAME_DST" ? "CDKEY_REDEEM" : "LIBRARY_IMPORT",
                0m,
                0m,
                0m,
                0m,
                null,
                null,
                null,
                null,
                null,
                null));

        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var rows = walletRows.Concat(orderRows).Concat(refundRows).Concat(libraryRows)
            .OrderByDescending(row => row.CreateTime)
            .ThenByDescending(row => row.HistoryId)
            .ToArray();
        var items = rows.Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize).ToArray();

        return Task.FromResult(new PagedResponse<WalletHistoryEntry>(items, normalizedPage, normalizedPageSize, rows.Length));
    }

    public async Task<WalletHistoryEntry> GetWalletHistoryEntryAsync(AuthClaims claims, string historyId, CancellationToken cancellationToken)
    {
        var normalizedHistoryId = NormalizeRequired(historyId, nameof(historyId));
        var page = await ListWalletHistoryAsync(claims, 1, 100, cancellationToken);
        return page.Items.FirstOrDefault(row => string.Equals(row.HistoryId, normalizedHistoryId, StringComparison.Ordinal))
            ?? throw new ResourceNotFoundException("Wallet history entry does not exist.");
    }

    public Task<OrderSummary> BuyGameAsync(AuthClaims claims, CreateOrderRequest request, CancellationToken cancellationToken)
    {
        EnsurePlayer(claims);
        var userId = claims.PrincipalId;
        var paymentMethod = NormalizeOrderPaymentMethod(request.PaymentMethod);
        if (_library.ContainsKey((userId, request.GameId)))
        {
            throw new BusinessRuleException("GAME_ALREADY_OWNED", "The player already owns this game.");
        }

        if (!_wallets.TryGetValue(userId, out var bal)) throw new ResourceNotFoundException("Wallet does not exist.");

        // For simplicity assume GAME_DST price = 50, GAME_CS2 = 0
        var payable = request.GameId == "GAME_CS2" ? 0m : 50m;
        if (payable <= 0) throw new BusinessRuleException("GAME_NOT_PAID", "Free games must use the free-claim endpoint.");

        if (paymentMethod == PaymentMethods.SteamWallet && bal < payable) throw new BusinessRuleException("INSUFFICIENT_BALANCE", "Wallet balance is not enough for this purchase.");

        if (paymentMethod == PaymentMethods.SteamWallet && !_wallets.TryUpdate(userId, bal - payable, bal))
        {
            throw new BusinessRuleException("BUY_GAME_FAILED", "Could not complete purchase due to a conflict. Please try again.");
        }

        if (!_library.TryAdd((userId, request.GameId), 0))
        {
            throw new BusinessRuleException("GAME_ALREADY_OWNED", "The player already owns this game.");
        }

        var orderId = "O_TEST_" + Guid.NewGuid().ToString("N")[..8];

        var originalPrice = request.GameId == "GAME_DST" ? 100m : payable;
        var discountAmount = originalPrice - payable;
        var detail = new OrderDetailEntry("OD_TEST", request.GameId, request.GameId == "GAME_DST" ? "Don't Starve Together" : "CS2", originalPrice, discountAmount, payable, 0m);
        var createTime = DateTime.UtcNow;
        _orders[orderId] = new OrderRecord
        {
            OrderId = orderId,
            UserId = userId,
            TotalAmount = payable,
            OrderStatus = "COMPLETED",
            PaymentStatus = "PAID",
            IdempotencyKey = request.IdempotencyKey,
            PaymentMethod = paymentMethod,
            CreateTime = createTime,
            Details = new List<OrderDetailRecord>
            {
                new()
                {
                    DetailId = detail.DetailId,
                    GameId = detail.GameId,
                    OriginalPrice = originalPrice,
                    DiscountAmount = discountAmount,
                    PayableAmount = payable,
                    RefundAmount = 0m
                }
            }
        };
        var summary = new OrderSummary(orderId, userId, payable, "BUY_GAME", "COMPLETED", "PAID", request.IdempotencyKey, createTime, new[] { detail }, paymentMethod);
        return Task.FromResult(summary);
    }

    public Task<OrderSummary> ClaimFreeGameAsync(AuthClaims claims, string gameId, CancellationToken cancellationToken)
    {
        EnsurePlayer(claims);
        var userId = claims.PrincipalId;
        if (gameId != "GAME_CS2") throw new BusinessRuleException("GAME_NOT_FREE", "Only Counter-Strike 2 uses the free-claim flow.");
        if (!_library.TryAdd((userId, gameId), 0))
        {
            throw new BusinessRuleException("GAME_ALREADY_OWNED", "The player already owns this game.");
        }
        var orderId = "O_TEST_" + Guid.NewGuid().ToString("N")[..8];
        var detail = new OrderDetailEntry("OD_TEST", gameId, "Counter-Strike 2", 0m, 0m, 0m, 0m);
        var summary = new OrderSummary(orderId, userId, 0m, "BUY_GAME", "COMPLETED", "PAID", $"free-{userId}-{gameId}", DateTime.UtcNow, new[] { detail });
        return Task.FromResult(summary);
    }

    public Task<IReadOnlyList<OrderSummary>> ListOrdersAsync(AuthClaims claims, CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyList<OrderSummary>)Array.Empty<OrderSummary>());

    public Task<IReadOnlyList<LibraryEntry>> ListLibraryAsync(AuthClaims claims, CancellationToken cancellationToken)
    {
        EnsurePlayer(claims);
        var userId = claims.PrincipalId;
        var list = _library.Keys.Where(l => l.userId == userId).Select(l => new LibraryEntry("LIB_TEST", l.gameId, l.gameId == "GAME_DST" ? "Don't Starve Together" : "Counter-Strike 2", l.gameId == "GAME_CS2" ? "FREE" : "BUY", "NORMAL", 0, null)).ToArray();
        return Task.FromResult((IReadOnlyList<LibraryEntry>)list);
    }

    public Task<OrderSummary> GetOrderAsync(AuthClaims claims, string orderId, CancellationToken cancellationToken)
    {
        EnsurePlayer(claims);
        var userId = claims.PrincipalId;
        var id = NormalizeRequired(orderId, nameof(orderId));
        if (!_orders.TryGetValue(id, out var order) || order.UserId != userId)
            throw new ResourceNotFoundException("Order does not exist.");

        var details = order.Details.Select(d => new OrderDetailEntry(
            d.DetailId,
            d.GameId,
            d.GameId == "GAME_DST" ? "Don't Starve Together" : "Counter-Strike 2",
            d.OriginalPrice,
            d.DiscountAmount,
            d.PayableAmount,
            d.RefundAmount)).ToArray();

        var summary = new OrderSummary(order.OrderId, order.UserId, order.TotalAmount, "BUY_GAME", order.OrderStatus, order.PaymentStatus, order.IdempotencyKey, order.CreateTime, details, order.PaymentMethod);
        return Task.FromResult(summary);
    }

    public Task<LibraryEntry> AddPlaytimeAsync(AuthClaims claims, string gameId, UpdatePlaytimeRequest request, CancellationToken cancellationToken)
    {
        EnsurePlayer(claims);
        var userId = claims.PrincipalId;
        var normalizedGameId = NormalizeRequired(gameId, nameof(gameId));
        var entryKey = (userId, normalizedGameId);
        if (!_library.ContainsKey(entryKey)) throw new ResourceNotFoundException("Library entry does not exist.");

        // In-memory we don't track play minutes persistently; return a synthetic entry.
        var lib = new LibraryEntry("LIB_TEST", normalizedGameId, normalizedGameId == "GAME_DST" ? "Don't Starve Together" : "Counter-Strike 2", normalizedGameId == "GAME_CS2" ? "FREE" : "BUY", "NORMAL", request.MinutesToAdd, DateTime.UtcNow);
        return Task.FromResult(lib);
    }

    public Task<RefundSummary> CreateRefundAsync(AuthClaims claims, CreateRefundRequest request, CancellationToken cancellationToken)
    {
        EnsurePlayer(claims);
        var userId = claims.PrincipalId;
        var orderId = NormalizeRequired(request.OrderId, nameof(request.OrderId));
        if (!_orders.TryGetValue(orderId, out var order) || order.UserId != userId)
            throw new ResourceNotFoundException("Order does not exist.");

        if (!(order.OrderStatus == "COMPLETED" && (order.PaymentStatus == "PAID" || order.PaymentStatus == "PARTIAL_REFUNDED")))
            throw new BusinessRuleException("ORDER_NOT_REFUNDABLE", "The order is not refundable.");

        if (_refunds.Values.Any(r => r.OrderId == orderId && r.Status == "PENDING"))
            throw new BusinessRuleException("REFUND_STATUS_INVALID", "The order already has a pending refund.");

        var refundable = order.Details.Sum(d => d.PayableAmount - d.RefundAmount);
        if (refundable <= 0) throw new BusinessRuleException("ORDER_NOT_REFUNDABLE", "The order has no refundable amount.");

        var refundId = "R" + Guid.NewGuid().ToString("N")[..8];
        var selectedDetail = order.Details.FirstOrDefault(detail =>
            string.IsNullOrWhiteSpace(request.OrderDetailId) || detail.DetailId == request.OrderDetailId);
        var refund = new RefundRecord
        {
            RefundId = refundId,
            OrderId = orderId,
            RefundAmount = refundable,
            Status = "PENDING",
            Reason = NormalizeRequired(request.Reason, nameof(request.Reason)),
            ApplyTime = DateTime.UtcNow,
            OrderDetailId = selectedDetail?.DetailId
        };
        _refunds.TryAdd(refundId, refund);
        // mark order refunding
        order.OrderStatus = "REFUNDING";

        var summary = new RefundSummary(refund.RefundId, refund.OrderId, refund.RefundAmount, "FULL", refund.Reason, 0m, refund.Status, refund.ApplyTime);
        return Task.FromResult(summary);
    }

    public Task<IReadOnlyList<RefundSummary>> ListRefundsAsync(AuthClaims claims, CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyList<RefundSummary>)Array.Empty<RefundSummary>());

    public Task<IReadOnlyList<RefundSummary>> ListAllRefundsAsync(AuthClaims claims, CancellationToken cancellationToken)
    {
        _ = NormalizeDeveloperOrAdmin(claims);
        var summaries = _refunds.Values
            .OrderByDescending(refund => refund.ApplyTime)
            .Select(refund => new RefundSummary(refund.RefundId, refund.OrderId, refund.RefundAmount, "FULL", refund.Reason, 0m, refund.Status, refund.ApplyTime))
            .ToArray();
        return Task.FromResult((IReadOnlyList<RefundSummary>)summaries);
    }

    public Task<RefundSummary> ApproveRefundAsync(AuthClaims claims, string refundId, AuditRefundRequest request, CancellationToken cancellationToken)
    {
        var operatorId = NormalizeDeveloperOrAdmin(claims);
        var id = NormalizeRequired(refundId, nameof(refundId));
        if (!_refunds.TryGetValue(id, out var refund)) throw new ResourceNotFoundException("Refund does not exist.");
        if (!string.Equals(refund.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("REFUND_STATUS_INVALID", "Refund is not pending.");

        // apply refund: Steam Wallet orders credit the wallet; external payment refunds stay outside the wallet.
        refund.Status = "APPROVED";
        refund.Auditor = operatorId;
        refund.AuditReason = NormalizeOptional(request.Reason);

        if (_orders.TryGetValue(refund.OrderId, out var order))
        {
            var userId = order.UserId;
            if (string.Equals(order.PaymentMethod, PaymentMethods.SteamWallet, StringComparison.OrdinalIgnoreCase)
                && _wallets.TryGetValue(userId, out var bal))
            {
                _wallets.TryUpdate(userId, bal + refund.RefundAmount, bal);
            }

            order.PaymentStatus = "REFUNDED";
            order.OrderStatus = "CLOSED";
        }

        return Task.FromResult(new RefundSummary(refund.RefundId, refund.OrderId, refund.RefundAmount, "FULL", refund.Reason, 0m, refund.Status, refund.ApplyTime));
    }

    public Task<RefundSummary> RejectRefundAsync(AuthClaims claims, string refundId, AuditRefundRequest request, CancellationToken cancellationToken)
    {
        var operatorId = NormalizeDeveloperOrAdmin(claims);
        var id = NormalizeRequired(refundId, nameof(refundId));
        if (!_refunds.TryGetValue(id, out var refund)) throw new ResourceNotFoundException("Refund does not exist.");
        if (!string.Equals(refund.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("REFUND_STATUS_INVALID", "Refund is not pending.");

        refund.Status = "REJECTED";
        refund.Auditor = operatorId;
        refund.AuditReason = NormalizeOptional(request.Reason);

        // restore order status
        if (_orders.TryGetValue(refund.OrderId, out var order))
        {
            order.OrderStatus = "COMPLETED";
        }

        return Task.FromResult(new RefundSummary(refund.RefundId, refund.OrderId, refund.RefundAmount, "FULL", refund.Reason, 0m, refund.Status, refund.ApplyTime));
    }

    public Task<CdkeyBatchSummary> CreateCdkeyBatchAsync(AuthClaims claims, CreateCdkeyBatchRequest request, CancellationToken cancellationToken)
    {
        var operatorId = NormalizeDeveloperOrAdmin(claims);
        ArgumentNullException.ThrowIfNull(request);
        var gameId = NormalizeRequired(request.GameId, nameof(request.GameId));
        var batchNo = NormalizeRequired(request.BatchNo, nameof(request.BatchNo));
        if (!string.Equals(gameId, "GAME_DST", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("CDKEY_GAME_UNSUPPORTED", "CDKey demo batches are only supported for DST.");

        if (request.ExpireTime <= request.ValidFrom) throw new ArgumentException("ExpireTime must be later than ValidFrom.");
        if (request.Quantity is <= 0 or > 100) throw new ArgumentException("Quantity must be between 1 and 100.");

        var batchId = "B" + Guid.NewGuid().ToString("N")[..8];
        var keys = new List<string>(request.Quantity);
        var batch = new CdkeyBatchRecord { BatchId = batchId, GameId = gameId, BatchNo = batchNo, ValidFrom = request.ValidFrom, ExpireTime = request.ExpireTime };
        _batches.TryAdd(batchId, batch);

        for (var i = 0; i < request.Quantity; i++)
        {
            var plaintext = CreatePlaintextCdkey();
            keys.Add(plaintext);
            var hash = HashCdkey(plaintext);
            var id = "CK" + Guid.NewGuid().ToString("N")[..8];
            _cdkeys.TryAdd(id, new CdkeyRecord(hash, batchId, "AVAILABLE", DateTime.UtcNow, gameId, request.ValidFrom, request.ExpireTime));
        }

        return Task.FromResult(new CdkeyBatchSummary(batchId, gameId, batchNo, request.ValidFrom, request.ExpireTime, keys));
    }

    public Task<CdkeyRedeemResult> RedeemCdkeyAsync(AuthClaims claims, RedeemCdkeyRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = NormalizePrincipal(claims);
        var plaintext = NormalizeRequired(request.Cdkey, nameof(request.Cdkey));
        var submittedHash = HashCdkey(plaintext);

        var cd = _cdkeys.Values.FirstOrDefault(c => c.CdkeyHash == submittedHash);
        if (cd is null)
        {
            return Task.FromResult(new CdkeyRedeemResult("INVALID", null, null, "CDKey does not exist."));
        }

        if (!string.Equals(cd.Status, "AVAILABLE", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new CdkeyRedeemResult("REDEEMED", cd.GameId, null, "CDKey has already been redeemed."));
        }

        var now = DateTime.UtcNow;
        if (now < cd.ValidFrom || now > cd.ExpireTime)
        {
            return Task.FromResult(new CdkeyRedeemResult("EXPIRED", cd.GameId, null, "CDKey is outside its valid time window."));
        }

        if (_library.ContainsKey((userId, cd.GameId)))
        {
            return Task.FromResult(new CdkeyRedeemResult("REDEEMED", cd.GameId, null, "Player already owns this game."));
        }

        // Simulate database atomic update: set status='REDEEMED' where cdkey_hash=:CdkeyHash and status='AVAILABLE'
        // Find the dictionary entry to perform compare-and-swap
        var cdkeyEntry = _cdkeys.FirstOrDefault(kvp => kvp.Value.CdkeyHash == submittedHash);
        if (!cdkeyEntry.Value.CdkeyHash.Equals(submittedHash, StringComparison.Ordinal))
        {
            return Task.FromResult(new CdkeyRedeemResult("INVALID", null, null, "CDKey does not exist."));
        }

        var currentRecord = cdkeyEntry.Value;
        if (!string.Equals(currentRecord.Status, "AVAILABLE", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new CdkeyRedeemResult("REDEEMED", currentRecord.GameId, null, "CDKey has already been redeemed."));
        }

        var redeemedRecord = currentRecord with { Status = "REDEEMED" };
        var updateSucceeded = _cdkeys.TryUpdate(cdkeyEntry.Key, redeemedRecord, currentRecord);
        if (!updateSucceeded)
        {
            return Task.FromResult(new CdkeyRedeemResult("REDEEMED", currentRecord.GameId, null, "CDKey has already been redeemed."));
        }

        if (!_library.TryAdd((userId, currentRecord.GameId), 0))
        {
            return Task.FromResult(new CdkeyRedeemResult("REDEEMED", currentRecord.GameId, null, "Player already owns this game."));
        }

        var libraryId = "LIB_TEST_" + Guid.NewGuid().ToString("N")[..8];
        return Task.FromResult(new CdkeyRedeemResult("SUCCESS", currentRecord.GameId, libraryId, "CDKey redeemed."));
    }

    // helpers and internal records
    private static string NormalizeRequired(string? value, string fieldName)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? throw new ArgumentException($"{fieldName} is required.") : normalized!;
    }

    private static string NormalizeRechargeIdempotencyKey(string? value)
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
        if (amount is < 0.01m or > 99999.99m)
        {
            throw new BusinessRuleException("INVALID_AMOUNT", "Recharge amount must be between 0.01 and 99999.99.");
        }

        if (decimal.Round(amount, 2, MidpointRounding.AwayFromZero) != amount)
        {
            throw new BusinessRuleException("INVALID_AMOUNT", "Recharge amount can have at most two decimal places.");
        }

        return amount;
    }

    private static string NormalizeExternalPaymentMethod(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessRuleException("INVALID_PAYMENT_METHOD", "Payment method is required.");
        }

        var normalized = NormalizePaymentMethod(value, PaymentMethods.WechatPay);
        return normalized == PaymentMethods.SteamWallet
            ? throw new BusinessRuleException("INVALID_PAYMENT_METHOD", "Wallet recharge must use an external payment method.")
            : normalized;
    }

    private static string NormalizeOrderPaymentMethod(string? value) =>
        NormalizePaymentMethod(value, PaymentMethods.SteamWallet);

    private static string NormalizePaymentMethod(string? value, string defaultValue)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value.Trim().ToUpperInvariant();

        return normalized is PaymentMethods.SteamWallet
            or PaymentMethods.WechatPay
            or PaymentMethods.Alipay
            or PaymentMethods.Visa
            or PaymentMethods.Mastercard
            ? normalized
            : throw new BusinessRuleException("INVALID_PAYMENT_METHOD", "Payment method is not supported.");
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string NormalizePrincipal(AuthClaims claims)
    {
        if (claims is null) throw new UnauthorizedAccessException("Player role is required.");
        if (!string.Equals(claims.Role, "PLAYER", StringComparison.OrdinalIgnoreCase)) throw new UnauthorizedAccessException("Player role is required.");
        return NormalizeRequired(claims.PrincipalId, nameof(AuthClaims.PrincipalId));
    }

    private static string NormalizeDeveloperOrAdmin(AuthClaims claims)
    {
        if (claims is null) throw new UnauthorizedAccessException();
        if (!AuthRoles.IsAdminRole(claims.Role) && !string.Equals(claims.Role, "DEVELOPER", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Developer or admin role is required.");
        return NormalizeRequired(claims.PrincipalId, nameof(AuthClaims.PrincipalId));
    }

    private static string CreatePlaintextCdkey()
    {
        var random = Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
        return $"DST-{random[..4]}-{random[4..8]}-{random[8..12]}-{random[12..]}";
    }

    private static string HashCdkey(string plaintext)
    {
        var normalized = NormalizeRequired(plaintext, nameof(plaintext)).ToUpperInvariant();
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(normalized)));
    }

    private sealed class OrderRecord
    {
        public string OrderId = "";
        public string UserId = "";
        public decimal TotalAmount;
        public string OrderStatus = "";
        public string PaymentStatus = "";
        public string? IdempotencyKey;
        public string PaymentMethod = PaymentMethods.SteamWallet;
        public DateTime CreateTime = DateTime.UtcNow;
        public List<OrderDetailRecord> Details = new();
    }

    private sealed class OrderDetailRecord
    {
        public string DetailId = "";
        public string GameId = "";
        public decimal OriginalPrice;
        public decimal DiscountAmount;
        public decimal PayableAmount;
        public decimal RefundAmount;
    }

    private sealed class RefundRecord
    {
        public string RefundId = "";
        public string OrderId = "";
        public decimal RefundAmount;
        public string Status = "";
        public string Reason = "";
        public DateTime ApplyTime;
        public string? OrderDetailId;
        public string? Auditor;
        public string? AuditReason;
    }

    private sealed record CdkeyRecord(
        string CdkeyHash,
        string BatchId,
        string Status,
        DateTime GenerateTime,
        string GameId,
        DateTime ValidFrom,
        DateTime ExpireTime
    );

    private sealed class CdkeyBatchRecord
    {
        public string BatchId = "";
        public string GameId = "";
        public string BatchNo = "";
        public DateTime ValidFrom;
        public DateTime ExpireTime;
    }

    private sealed record WalletTransactionRecord(string WalletId, WalletTransactionEntry Entry);
}
