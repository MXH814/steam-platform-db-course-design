using SteamPlatform.Application.Auth;
using SteamPlatform.Shared;

namespace SteamPlatform.Application.CoreTransactions;

public static class PaymentMethods
{
    public const string SteamWallet = "STEAM_WALLET";
    public const string WechatPay = "WECHAT_PAY";
    public const string Alipay = "ALIPAY";
    public const string Visa = "VISA";
    public const string Mastercard = "MASTERCARD";
}

public sealed record CreateOrderRequest(string GameId, string IdempotencyKey, string? PaymentMethod = null);

public sealed record UpdatePlaytimeRequest(int MinutesToAdd);

public sealed record CreateRefundRequest(string OrderId, string Reason, string? OrderDetailId = null, string? GameId = null);

public sealed record AuditRefundRequest(string? Reason);

public sealed record CreateCdkeyBatchRequest(string GameId, string BatchNo, DateTime ValidFrom, DateTime ExpireTime, int Quantity);

public sealed record RedeemCdkeyRequest(string Cdkey);

public sealed record RechargeWalletRequest(decimal Amount, string IdempotencyKey, string? PaymentMethod = null);

public sealed record WalletSummary(string WalletId, string UserId, decimal AvailableBalance, decimal FrozenBalance, decimal TotalBalance, long Version);

public sealed record RechargeWalletResult(
    string WalletId,
    string TransactionId,
    decimal AvailableBalance,
    decimal FrozenBalance,
    decimal TotalBalance);

public sealed record WalletTransactionEntry(
    string TxnId,
    string BizType,
    string BizRefId,
    string FundsDirection,
    decimal Amount,
    decimal AvailBalBefore,
    decimal AvailBalAfter,
    string? IdempotencyKey,
    string? PaymentMethod,
    DateTime CreateTime);

public sealed record WalletHistoryEntry(
    string HistoryId,
    string SourceType,
    DateTime CreateTime,
    string ItemName,
    string PaymentMethod,
    decimal OriginalPrice,
    decimal DiscountAmount,
    decimal DiscountRate,
    decimal TotalAmount,
    decimal? WalletChange,
    decimal? WalletBalanceAfter,
    string? OrderId,
    string? OrderDetailId,
    string? RefundId,
    string? WalletTransactionId);

public sealed record OrderDetailEntry(
    string DetailId,
    string GameId,
    string GameName,
    decimal OriginalPrice,
    decimal DiscountAmount,
    decimal PayableAmount,
    decimal RefundAmount);

public sealed record OrderSummary(
    string OrderId,
    string UserId,
    decimal TotalAmount,
    string OrderType,
    string OrderStatus,
    string PaymentStatus,
    string? IdempotencyKey,
    DateTime CreateTime,
    IReadOnlyList<OrderDetailEntry> Details,
    string? PaymentMethod = null);

public sealed record LibraryEntry(
    string LibId,
    string GameId,
    string GameName,
    string AcquireWay,
    string Status,
    int PlayMinutes,
    DateTime? LastPlayTime);

public sealed record RefundSummary(
    string RefundId,
    string OrderId,
    decimal RefundAmount,
    string RefundType,
    string Reason,
    decimal PlayTimeHours,
    string Status,
    DateTime ApplyTime);

public sealed record CdkeyBatchSummary(
    string BatchId,
    string GameId,
    string BatchNo,
    DateTime ValidFrom,
    DateTime ExpireTime,
    IReadOnlyList<string> PlaintextKeys);

public sealed record CdkeyRedeemResult(string Result, string? GameId, string? LibraryId, string Message);

public interface ICoreTransactionService
{
    Task<WalletSummary> GetWalletAsync(AuthClaims claims, CancellationToken cancellationToken);
    Task<RechargeWalletResult> RechargeWalletAsync(AuthClaims claims, RechargeWalletRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<WalletTransactionEntry>> ListWalletTransactionsAsync(AuthClaims claims, int page, int pageSize, CancellationToken cancellationToken);
    Task<PagedResponse<WalletHistoryEntry>> ListWalletHistoryAsync(AuthClaims claims, int page, int pageSize, CancellationToken cancellationToken);
    Task<WalletHistoryEntry> GetWalletHistoryEntryAsync(AuthClaims claims, string historyId, CancellationToken cancellationToken);
    Task<OrderSummary> BuyGameAsync(AuthClaims claims, CreateOrderRequest request, CancellationToken cancellationToken);
    Task<OrderSummary> ClaimFreeGameAsync(AuthClaims claims, string gameId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderSummary>> ListOrdersAsync(AuthClaims claims, CancellationToken cancellationToken);
    Task<OrderSummary> GetOrderAsync(AuthClaims claims, string orderId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LibraryEntry>> ListLibraryAsync(AuthClaims claims, CancellationToken cancellationToken);
    Task<LibraryEntry> AddPlaytimeAsync(AuthClaims claims, string gameId, UpdatePlaytimeRequest request, CancellationToken cancellationToken);
    Task<RefundSummary> CreateRefundAsync(AuthClaims claims, CreateRefundRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<RefundSummary>> ListRefundsAsync(AuthClaims claims, CancellationToken cancellationToken);
    Task<IReadOnlyList<RefundSummary>> ListAllRefundsAsync(AuthClaims claims, CancellationToken cancellationToken);
    Task<RefundSummary> ApproveRefundAsync(AuthClaims claims, string refundId, AuditRefundRequest request, CancellationToken cancellationToken);
    Task<RefundSummary> RejectRefundAsync(AuthClaims claims, string refundId, AuditRefundRequest request, CancellationToken cancellationToken);
    Task<CdkeyBatchSummary> CreateCdkeyBatchAsync(AuthClaims claims, CreateCdkeyBatchRequest request, CancellationToken cancellationToken);
    Task<CdkeyRedeemResult> RedeemCdkeyAsync(AuthClaims claims, RedeemCdkeyRequest request, CancellationToken cancellationToken);
}
