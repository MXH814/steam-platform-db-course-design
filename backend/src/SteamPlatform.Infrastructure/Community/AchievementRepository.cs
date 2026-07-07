using Dapper;
using SteamPlatform.Application.Community;
using SteamPlatform.Infrastructure.Data;
using SteamPlatform.Shared;

namespace SteamPlatform.Infrastructure.Community;

public sealed class AchievementRepository(IDbConnectionFactory connectionFactory) : IAchievementRepository
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    public async Task<IReadOnlyList<AchievementListItem>> ListByGameAsync(string gameId, string? userId, CancellationToken cancellationToken)
    {
        var normalizedGameId = NormalizeRequired(gameId, nameof(gameId));
        var normalizedUserId = NormalizeOptional(userId);

        await using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<AchievementRow>(new CommandDefinition(
            """
            select a.ach_id as AchId,
                   a.game_id as GameId,
                   a.ach_name as AchName,
                   a.description as Description,
                   a.global_rate as GlobalRate,
                   pa.unlock_id as UnlockId,
                   pa.unlock_time as UnlockTime
              from achievement a
              left join player_achievement pa
                on pa.ach_id = a.ach_id
               and pa.user_id = :UserId
             where a.game_id = :GameId
             order by a.ach_name asc, a.ach_id asc
            """,
            new { GameId = normalizedGameId, UserId = normalizedUserId },
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToItem()).ToList();
    }

    public async Task<UnlockAchievementResult> UnlockAsync(string userId, string achId, CancellationToken cancellationToken)
    {
        var normalizedUserId = NormalizeRequired(userId, nameof(userId));
        var normalizedAchId = NormalizeRequired(achId, nameof(achId));

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var lockedAchievementId = await connection.QueryFirstOrDefaultAsync<string?>(new CommandDefinition(
            "select ach_id from achievement where ach_id = :AchId for update",
            new { AchId = normalizedAchId },
            transaction,
            cancellationToken: cancellationToken));

        if (lockedAchievementId is null)
        {
            throw new ResourceNotFoundException("Achievement does not exist.");
        }

        var existing = await connection.QueryFirstOrDefaultAsync<UnlockRow>(new CommandDefinition(
            """
            select unlock_id as UnlockId,
                   user_id as UserId,
                   ach_id as AchId,
                   unlock_time as UnlockTime
              from player_achievement
             where user_id = :UserId
               and ach_id = :AchId
            """,
            new { UserId = normalizedUserId, AchId = normalizedAchId },
            transaction,
            cancellationToken: cancellationToken));

        if (existing is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return existing.ToResult(alreadyUnlocked: true);
        }

        var unlock = new UnlockRow
        {
            UnlockId = IdGenerator.NewId("PA"),
            UserId = normalizedUserId,
            AchId = normalizedAchId,
            UnlockTime = DateTime.UtcNow
        };

        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into player_achievement
              (unlock_id, user_id, ach_id, unlock_time)
            values
              (:UnlockId, :UserId, :AchId, :UnlockTime)
            """,
            unlock,
            transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return unlock.ToResult(alreadyUnlocked: false);
    }

    private static string NormalizeRequired(string? value, string fieldName)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? throw new ArgumentException($"{fieldName} is required.")
            : normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private sealed class AchievementRow
    {
        public string AchId { get; init; } = "";
        public string GameId { get; init; } = "";
        public string AchName { get; init; } = "";
        public string? Description { get; init; }
        public decimal? GlobalRate { get; init; }
        public string? UnlockId { get; init; }
        public DateTime? UnlockTime { get; init; }

        public AchievementListItem ToItem() => new(
            AchId,
            GameId,
            AchName,
            Description,
            GlobalRate,
            UnlockId is not null,
            UnlockTime);
    }

    private sealed class UnlockRow
    {
        public string UnlockId { get; init; } = "";
        public string UserId { get; init; } = "";
        public string AchId { get; init; } = "";
        public DateTime UnlockTime { get; init; }

        public UnlockAchievementResult ToResult(bool alreadyUnlocked) => new(UnlockId, UserId, AchId, alreadyUnlocked, UnlockTime);
    }
}