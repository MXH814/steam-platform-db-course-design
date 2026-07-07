using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SteamPlatform.Application.Auth;
using SteamPlatform.Application.CoreTransactions;
using SteamPlatform.Domain.Notices;
using SteamPlatform.Shared;

namespace SteamPlatform.Infrastructure.CoreTransactions;

// Minimal in-memory implementation for local development and unit tests.
public sealed class InMemoryCoreTransactionService : ICoreTransactionService
{
    private readonly Dictionary<string, decimal> _wallets = new();
    private readonly HashSet<(string userId, string gameId)> _library = new();
    private readonly Dictionary<string, OrderRecord> _orders = new();
    private readonly Dictionary<string, RefundRecord> _refunds = new();
    private readonly Dictionary<string, CdkeyRecord> _cdkeys = new();
    private readonly Dictionary<string, CdkeyBatchRecord> _batches = new();

    public InMemoryCoreTransactionService()
    {
        // seed wallets for P001 and P002 for tests/demo
        _wallets["P001"] = 100m;
        _wallets["P002"] = 10m;
        // seed P001 owns DST in demo data
        _library.Add(("P001", "GAME_DST"));
        // seed an existing completed paid order for P001 for refund demo
        var orderId = "O_DST_001";
        _orders[orderId] = new OrderRecord
        {
            OrderId = orderId,
            UserId = "P001",
            TotalAmount = 50m,
            OrderStatus = "COMPLETED",
            PaymentStatus = "PAID",
            IdempotencyKey = "seed-001",
            Details = new List<OrderDetailRecord>
            {
                new OrderDetailRecord { DetailId = "OD_DST_001", GameId = "GAME_DST", PayableAmount = 50m, RefundAmount = 0m }
            }
        };
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
        return Task.FromResult(new WalletSummary($"W-{userId}", userId, bal, 0m, 1));
    }

    public Task<IReadOnlyList<WalletTransactionEntry>> ListWalletTransactionsAsync(AuthClaims claims, int limit, CancellationToken cancellationToken)
    {
        EnsurePlayer(claims);
        return Task.FromResult((IReadOnlyList<WalletTransactionEntry>)Array.Empty<WalletTransactionEntry>());
    }

    public Task<OrderSummary> BuyGameAsync(AuthClaims claims, CreateOrderRequest request, CancellationToken cancellationToken)
    {
        EnsurePlayer(claims);
        var userId = claims.PrincipalId;
        if (_library.Contains((userId, request.GameId)))
        {
            throw new BusinessRuleException("GAME_ALREADY_OWNED", "The player already owns this game.");
        }

        if (!_wallets.TryGetValue(userId, out var bal)) throw new ResourceNotFoundException("Wallet does not exist.");

        // For simplicity assume GAME_DST price = 50, GAME_CS2 = 0
        var payable = request.GameId == "GAME_CS2" ? 0m : 50m;
        if (payable <= 0) throw new BusinessRuleException("GAME_NOT_PAID", "Free games must use the free-claim endpoint.");

        if (bal < payable) throw new BusinessRuleException("INSUFFICIENT_BALANCE", "Wallet balance is not enough for this purchase.");

        _wallets[userId] = bal - payable;
        var orderId = "O_TEST_" + Guid.NewGuid().ToString("N")[..8];
        _library.Add((userId, request.GameId));

        var detail = new OrderDetailEntry("OD_TEST", request.GameId, request.GameId == "GAME_DST" ? "Don't Starve Together" : "CS2", payable, 0m, payable, 0m);
        var summary = new OrderSummary(orderId, userId, payable, "BUY_GAME", "COMPLETED", "PAID", request.IdempotencyKey, DateTime.UtcNow, new[] { detail });
        return Task.FromResult(summary);
    }

    public Task<OrderSummary> ClaimFreeGameAsync(AuthClaims claims, string gameId, CancellationToken cancellationToken)
    {
        EnsurePlayer(claims);
        var userId = claims.PrincipalId;
        if (gameId != "GAME_CS2") throw new BusinessRuleException("GAME_NOT_FREE", "Only Counter-Strike 2 uses the free-claim flow.");
        if (_library.Contains((userId, gameId))) throw new BusinessRuleException("GAME_ALREADY_OWNED", "The player already owns this game.");
        _library.Add((userId, gameId));
        var orderId = "O_TEST_" + Guid.NewGuid().ToString("N")[..8];
        var detail = new OrderDetailEntry("OD_TEST", gameId, "Counter-Strike 2", 0m, 0m, 0m, 0m);
        var summary = new OrderSummary(orderId, userId, 0m, "BUY_GAME", "COMPLETED", "PAID", $"free-{userId}-{gameId}", DateTime.UtcNow, new[] { detail });
        return Task.FromResult(summary);
    }

    public Task<IReadOnlyList<OrderSummary>> ListOrdersAsync(AuthClaims claims, CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyList<OrderSummary>)Array.Empty<OrderSummary>());

    public Task<OrderSummary> GetOrderAsync(AuthClaims claims, string orderId, CancellationToken cancellationToken)
        => Task.FromException<OrderSummary>(new NotImplementedException());

    public Task<IReadOnlyList<LibraryEntry>> ListLibraryAsync(AuthClaims claims, CancellationToken cancellationToken)
    {
        EnsurePlayer(claims);
        var userId = claims.PrincipalId;
        var list = _library.Where(l => l.userId == userId).Select(l => new LibraryEntry("LIB_TEST", l.gameId, l.gameId == "GAME_DST" ? "Don't Starve Together" : "Counter-Strike 2", l.gameId == "GAME_CS2" ? "FREE" : "BUY", "NORMAL", 0, null)).ToArray();
        return Task.FromResult((IReadOnlyList<LibraryEntry>)list);
    }

    public Task<LibraryEntry> AddPlaytimeAsync(AuthClaims claims, string gameId, UpdatePlaytimeRequest request, CancellationToken cancellationToken)
        => Task.FromException<LibraryEntry>(new NotImplementedException());

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
        var refund = new RefundRecord
        {
            RefundId = refundId,
            OrderId = orderId,
            RefundAmount = refundable,
            Status = "PENDING",
            Reason = NormalizeRequired(request.Reason, nameof(request.Reason)),
            ApplyTime = DateTime.UtcNow
        };
        _refunds[refundId] = refund;
        // mark order refunding
        order.OrderStatus = "REFUNDING";

        var summary = new RefundSummary(refund.RefundId, refund.OrderId, refund.RefundAmount, "FULL", refund.Reason, 0m, refund.Status, refund.ApplyTime);
        return Task.FromResult(summary);
    }

    public Task<IReadOnlyList<RefundSummary>> ListRefundsAsync(AuthClaims claims, CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyList<RefundSummary>)Array.Empty<RefundSummary>());

    public Task<RefundSummary> ApproveRefundAsync(AuthClaims claims, string refundId, AuditRefundRequest request, CancellationToken cancellationToken)
        => Task.FromException<RefundSummary>(new NotImplementedException());

    public Task<RefundSummary> RejectRefundAsync(AuthClaims claims, string refundId, AuditRefundRequest request, CancellationToken cancellationToken)
        => Task.FromException<RefundSummary>(new NotImplementedException());

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
        _batches[batchId] = batch;

        for (var i = 0; i < request.Quantity; i++)
        {
            var plaintext = CreatePlaintextCdkey();
            keys.Add(plaintext);
            var hash = HashCdkey(plaintext);
            var id = "CK" + Guid.NewGuid().ToString("N")[..8];
            _cdkeys[id] = new CdkeyRecord { CdkeyHash = hash, BatchId = batchId, Status = "AVAILABLE", GenerateTime = DateTime.UtcNow, GameId = gameId, ValidFrom = request.ValidFrom, ExpireTime = request.ExpireTime };
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

        if (_library.Contains((userId, cd.GameId)))
        {
            cd.Status = "REDEEMED"; // still mark
            return Task.FromResult(new CdkeyRedeemResult("REDEEMED", null, null, "Player already owns this game."));
        }

        // redeem
        cd.Status = "REDEEMED";
        var libraryId = "LIB_TEST_" + Guid.NewGuid().ToString("N")[..8];
        _library.Add((userId, cd.GameId));
        return Task.FromResult(new CdkeyRedeemResult("SUCCESS", cd.GameId, libraryId, "CDKey redeemed."));
    }

    // helpers and internal records
    private static string NormalizeRequired(string? value, string fieldName)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? throw new ArgumentException($"{fieldName} is required.") : normalized!;
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
        public List<OrderDetailRecord> Details = new();
    }

    private sealed class OrderDetailRecord
    {
        public string DetailId = "";
        public string GameId = "";
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
        public string? Auditor;
        public string? AuditReason;
    }

    private sealed class CdkeyRecord
    {
        public string CdkeyHash = "";
        public string BatchId = "";
        public string Status = "";
        public DateTime GenerateTime;
        public string GameId = "";
        public DateTime ValidFrom;
        public DateTime ExpireTime;
    }

    private sealed class CdkeyBatchRecord
    {
        public string BatchId = "";
        public string GameId = "";
        public string BatchNo = "";
        public DateTime ValidFrom;
        public DateTime ExpireTime;
    }
}

