namespace SteamPlatform.Application.Community;

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
    Task<IReadOnlyList<ReviewVersionItem>> ListVersionsAsync(string reviewId, CancellationToken cancellationToken);
}