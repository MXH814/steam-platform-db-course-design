using SteamPlatform.Shared;

namespace SteamPlatform.Application.Games;

public sealed record GameListQuery(
    string? Keyword,
    string? Status,
    string? DeveloperId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Reputation,
    int Page = 1,
    int PageSize = 20);

public sealed record GameListItemResponse(
    string GameId,
    string GameName,
    string DeveloperId,
    string DeveloperName,
    decimal BasePrice,
    decimal DiscountRate,
    decimal FinalPrice,
    DateTime ReleaseDate,
    string? Reputation,
    string Status);

public sealed record GameDetailResponse(
    string GameId,
    string GameName,
    string DeveloperId,
    string DeveloperName,
    decimal BasePrice,
    decimal DiscountRate,
    decimal FinalPrice,
    DateTime ReleaseDate,
    string? Reputation,
    string Status);

public sealed record ReviewSummaryResponse(
    int ReviewCount,
    int RecommendCount,
    decimal RecommendRate,
    string? LatestReviewContent);

public sealed record AchievementSummaryResponse(
    int AchievementCount,
    decimal? AverageGlobalRate,
    IReadOnlyList<AchievementSummaryItemResponse> Achievements);

public sealed record AchievementSummaryItemResponse(
    string AchievementId,
    string AchievementName,
    string? Description,
    decimal? GlobalRate);

public sealed record GameContentPackageResponse(
    string PackageId,
    string GameId,
    string PackageName,
    string PackageType,
    decimal BasePrice,
    decimal DiscountRate,
    decimal FinalPrice,
    string? ImageUrl,
    string SourceKind);

public sealed record GameItemSummaryResponse(
    string GameId,
    int TemplateCount,
    int InventoryItemCount,
    int ActiveBuyOrderCount,
    int ActiveSellOrderCount,
    int TradeCount,
    decimal? HighestBuyPrice,
    decimal? LowestSellPrice,
    decimal? LastTradePrice,
    IReadOnlyList<GameItemSummaryEntryResponse> Items);

public sealed record GameItemSummaryEntryResponse(
    string TemplateId,
    string ItemName,
    string Rarity,
    string? ImageUrl,
    int InventoryItemCount,
    int ActiveBuyOrderCount,
    int ActiveSellOrderCount,
    int TradeCount,
    decimal? HighestBuyPrice,
    decimal? LowestSellPrice,
    decimal? LastTradePrice);

public sealed record CreateGameRequest(
    string DevId,
    string GameName,
    decimal BasePrice,
    decimal DiscountRate,
    DateTime ReleaseDate,
    string? Reputation);

public sealed record UpdateGameRequest(
    string GameName,
    decimal BasePrice,
    decimal DiscountRate,
    DateTime ReleaseDate,
    string? Reputation);

public interface IGameService
{
    Task<PagedResponse<GameListItemResponse>> ListAsync(GameListQuery query, CancellationToken cancellationToken);
    Task<GameDetailResponse> GetDetailAsync(string gameId, CancellationToken cancellationToken);
    Task<ReviewSummaryResponse> GetReviewSummaryAsync(string gameId, CancellationToken cancellationToken);
    Task<AchievementSummaryResponse> GetAchievementSummaryAsync(string gameId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GameContentPackageResponse>> GetContentPackagesAsync(string gameId, CancellationToken cancellationToken);
    Task<GameItemSummaryResponse> GetItemSummaryAsync(string gameId, CancellationToken cancellationToken);
    Task<GameDetailResponse> CreateAsync(CreateGameRequest request, CancellationToken cancellationToken);
    Task<GameDetailResponse> UpdateAsync(string gameId, string developerId, UpdateGameRequest request, CancellationToken cancellationToken);
    Task<GameDetailResponse> SetStatusAsync(string gameId, string status, CancellationToken cancellationToken);
}

public interface IGameRepository
{
    Task<PagedResponse<GameListItemResponse>> ListAsync(GameListQuery query, CancellationToken cancellationToken);
    Task<GameDetailResponse?> GetDetailAsync(string gameId, CancellationToken cancellationToken);
    Task<ReviewSummaryResponse> GetReviewSummaryAsync(string gameId, CancellationToken cancellationToken);
    Task<AchievementSummaryResponse> GetAchievementSummaryAsync(string gameId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GameContentPackageResponse>> GetContentPackagesAsync(string gameId, CancellationToken cancellationToken);
    Task<GameItemSummaryResponse> GetItemSummaryAsync(string gameId, CancellationToken cancellationToken);
    Task<bool> DeveloperExistsAsync(string developerId, CancellationToken cancellationToken);
    Task<GameDetailResponse> CreateAsync(string gameId, CreateGameRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(string gameId, string developerId, UpdateGameRequest request, CancellationToken cancellationToken);
    Task<bool> SetStatusAsync(string gameId, string status, CancellationToken cancellationToken);
}
