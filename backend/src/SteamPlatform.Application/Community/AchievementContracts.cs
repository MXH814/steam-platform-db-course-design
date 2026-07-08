namespace SteamPlatform.Application.Community;

public sealed record AchievementListItem(
    string AchId,
    string GameId,
    string AchName,
    string? Description,
    decimal? GlobalRate,
    bool IsUnlocked,
    DateTime? UnlockTime);

public sealed record UnlockAchievementResult(
    string UnlockId,
    string UserId,
    string AchId,
    bool AlreadyUnlocked,
    DateTime UnlockTime);

public interface IAchievementRepository
{
    Task<IReadOnlyList<AchievementListItem>> ListByGameAsync(string gameId, string? userId, CancellationToken cancellationToken);
    Task<UnlockAchievementResult> UnlockAsync(string userId, string achId, CancellationToken cancellationToken);
}