namespace SteamPlatform.Application.Community;

public sealed record CreateReviewRequest(bool IsRecommend, string Content);

public sealed record UpdateReviewRequest(bool IsRecommend, string Content);

public sealed record ReviewListItem(
    string ReviewId,
    string UserId,
    string Nickname,
    string GameId,
    int ThumbsUp,
    string Status,
    DateTime CreateTime,
    int VersionNo,
    bool IsRecommend,
    string Content,
    DateTime VersionCreateTime);

public sealed record ReviewVersionItem(
    string VersionId,
    string ReviewId,
    int VersionNo,
    bool IsRecommend,
    string Content,
    DateTime CreateTime);

public interface IReviewRepository
{
    Task<IReadOnlyList<ReviewListItem>> ListByGameAsync(string gameId, int limit, CancellationToken cancellationToken);
    Task<ReviewListItem> CreateAsync(string gameId, string userId, CreateReviewRequest request, CancellationToken cancellationToken);
    Task<ReviewListItem> UpdateAsync(string reviewId, string userId, UpdateReviewRequest request, CancellationToken cancellationToken);
    Task<ReviewListItem> SetStatusAsync(string reviewId, string status, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReviewVersionItem>> ListVersionsAsync(string reviewId, CancellationToken cancellationToken);
}
