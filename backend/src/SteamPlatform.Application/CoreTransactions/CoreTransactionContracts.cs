using SteamPlatform.Application.Auth;

namespace SteamPlatform.Application.CoreTransactions;

public sealed record CreateOrderRequest(string GameId, string IdempotencyKey);

public sealed record UpdatePlaytimeRequest(int MinutesToAdd);

public sealed record WalletSummary(string WalletId, string UserId, decimal AvailableBalance, decimal FrozenBalance, long Version);

public sealed record WalletTransactionEntry(
    string TxnId,
    string BizType,
    string BizRefId,
    string FundsDirection,
    decimal Amount,
    decimal AvailBalBefore,
    decimal AvailBalAfter,
    DateTime CreateTime);

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
    IReadOnlyList<OrderDetailEntry> Details);

public sealed record LibraryEntry(
    string LibId,
    string GameId,
    string GameName,
    string AcquireWay,
    string Status,
    int PlayMinutes,
    DateTime? LastPlayTime);

public interface ICoreTransactionService
{
    Task<WalletSummary> GetWalletAsync(AuthClaims claims, CancellationToken cancellationToken);
    Task<IReadOnlyList<WalletTransactionEntry>> ListWalletTransactionsAsync(AuthClaims claims, int limit, CancellationToken cancellationToken);
    Task<OrderSummary> BuyGameAsync(AuthClaims claims, CreateOrderRequest request, CancellationToken cancellationToken);
    Task<OrderSummary> ClaimFreeGameAsync(AuthClaims claims, string gameId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderSummary>> ListOrdersAsync(AuthClaims claims, CancellationToken cancellationToken);
    Task<OrderSummary> GetOrderAsync(AuthClaims claims, string orderId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LibraryEntry>> ListLibraryAsync(AuthClaims claims, CancellationToken cancellationToken);
    Task<LibraryEntry> AddPlaytimeAsync(AuthClaims claims, string gameId, UpdatePlaytimeRequest request, CancellationToken cancellationToken);
}
