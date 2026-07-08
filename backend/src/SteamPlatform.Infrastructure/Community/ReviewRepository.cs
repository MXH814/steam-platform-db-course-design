using System.Data.Common;
using Dapper;
using SteamPlatform.Application.Community;
using SteamPlatform.Infrastructure.Data;
using SteamPlatform.Shared;

namespace SteamPlatform.Infrastructure.Community;

public sealed class ReviewRepository(IDbConnectionFactory connectionFactory) : IReviewRepository
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    public async Task<IReadOnlyList<ReviewListItem>> ListByGameAsync(string gameId, int limit, CancellationToken cancellationToken)
    {
        var normalizedGameId = NormalizeRequired(gameId, nameof(gameId));
        var normalizedLimit = Math.Clamp(limit, 1, 100);

        await using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<ReviewListRow>(new CommandDefinition(
            LatestReviewSql("gr.game_id = :GameId and gr.status = 'VISIBLE'"),
            new { GameId = normalizedGameId, Limit = normalizedLimit },
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToItem()).ToList();
    }

    public async Task<ReviewListItem> CreateAsync(string gameId, string userId, CreateReviewRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalizedGameId = NormalizeRequired(gameId, nameof(gameId));
        var normalizedUserId = NormalizeRequired(userId, nameof(userId));
        var content = NormalizeRequired(request.Content, nameof(request.Content));

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var gameExists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "select count(1) from game where game_id = :GameId",
            new { GameId = normalizedGameId },
            transaction,
            cancellationToken: cancellationToken));

        if (gameExists == 0)
        {
            throw new ResourceNotFoundException("Game does not exist.");
        }

        var ownsGame = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            select count(1)
              from player_library
             where user_id = :UserId
               and game_id = :GameId
               and status = 'NORMAL'
            """,
            new { UserId = normalizedUserId, GameId = normalizedGameId },
            transaction,
            cancellationToken: cancellationToken));

        if (ownsGame == 0)
        {
            throw new BusinessRuleException("GAME_NOT_OWNED", "The player does not own this game.");
        }

        var existingReviewId = await connection.QueryFirstOrDefaultAsync<string?>(new CommandDefinition(
            "select review_id from game_review where user_id = :UserId and game_id = :GameId",
            new { UserId = normalizedUserId, GameId = normalizedGameId },
            transaction,
            cancellationToken: cancellationToken));

        if (existingReviewId is not null)
        {
            throw new BusinessRuleException("REVIEW_ALREADY_EXISTS", "The player already reviewed this game.");
        }

        var reviewId = IdGenerator.NewId("REV");
        var versionId = IdGenerator.NewId("RV");

        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into game_review
              (review_id, user_id, game_id, thumbs_up, status, create_time)
            values
              (:ReviewId, :UserId, :GameId, 0, 'VISIBLE', SYSTIMESTAMP)
            """,
            new { ReviewId = reviewId, UserId = normalizedUserId, GameId = normalizedGameId },
            transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into review_version
              (version_id, review_id, version_no, is_recommend, content, create_time)
            values
              (:VersionId, :ReviewId, 1, :IsRecommend, :Content, SYSTIMESTAMP)
            """,
            new { VersionId = versionId, ReviewId = reviewId, IsRecommend = ToNumber(request.IsRecommend), Content = content },
            transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        return await GetLatestReviewAsync(connection, reviewId, cancellationToken)
            ?? throw new ResourceNotFoundException("Review does not exist.");
    }

    public async Task<ReviewListItem> UpdateAsync(string reviewId, string userId, UpdateReviewRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalizedReviewId = NormalizeRequired(reviewId, nameof(reviewId));
        var normalizedUserId = NormalizeRequired(userId, nameof(userId));
        var content = NormalizeRequired(request.Content, nameof(request.Content));

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var ownerUserId = await connection.QueryFirstOrDefaultAsync<string?>(new CommandDefinition(
            "select user_id from game_review where review_id = :ReviewId for update",
            new { ReviewId = normalizedReviewId },
            transaction,
            cancellationToken: cancellationToken));

        if (ownerUserId is null)
        {
            throw new ResourceNotFoundException("Review does not exist.");
        }

        if (!string.Equals(ownerUserId, normalizedUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("Cannot edit another player's review.");
        }

        var nextVersionNo = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "select nvl(max(version_no), 0) + 1 from review_version where review_id = :ReviewId",
            new { ReviewId = normalizedReviewId },
            transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into review_version
              (version_id, review_id, version_no, is_recommend, content, create_time)
            values
              (:VersionId, :ReviewId, :VersionNo, :IsRecommend, :Content, SYSTIMESTAMP)
            """,
            new
            {
                VersionId = IdGenerator.NewId("RV"),
                ReviewId = normalizedReviewId,
                VersionNo = nextVersionNo,
                IsRecommend = ToNumber(request.IsRecommend),
                Content = content
            },
            transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        return await GetLatestReviewAsync(connection, normalizedReviewId, cancellationToken)
            ?? throw new ResourceNotFoundException("Review does not exist.");
    }

    public async Task<IReadOnlyList<ReviewVersionItem>> ListVersionsAsync(string reviewId, CancellationToken cancellationToken)
    {
        var normalizedReviewId = NormalizeRequired(reviewId, nameof(reviewId));

        await using var connection = _connectionFactory.CreateConnection();
        var exists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "select count(1) from game_review where review_id = :ReviewId",
            new { ReviewId = normalizedReviewId },
            cancellationToken: cancellationToken));

        if (exists == 0)
        {
            throw new ResourceNotFoundException("Review does not exist.");
        }

        var rows = await connection.QueryAsync<ReviewVersionRow>(new CommandDefinition(
            """
            select version_id as VersionId,
                   review_id as ReviewId,
                   version_no as VersionNo,
                   is_recommend as IsRecommendNumber,
                   content as Content,
                   create_time as CreateTime
              from review_version
             where review_id = :ReviewId
             order by version_no desc
            """,
            new { ReviewId = normalizedReviewId },
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToItem()).ToList();
    }

    private static async Task<ReviewListItem?> GetLatestReviewAsync(DbConnection connection, string reviewId, CancellationToken cancellationToken)
    {
        var row = await connection.QueryFirstOrDefaultAsync<ReviewListRow>(new CommandDefinition(
            LatestReviewSql("gr.review_id = :ReviewId"),
            new { ReviewId = reviewId, Limit = 1 },
            cancellationToken: cancellationToken));

        return row?.ToItem();
    }

    private static string LatestReviewSql(string whereClause) =>
        $$"""
        with latest_version as (
          select review_id, max(version_no) as version_no
            from review_version
           group by review_id
        )
        select *
          from (
            select gr.review_id as ReviewId,
                   gr.user_id as UserId,
                   p.nickname as Nickname,
                   gr.game_id as GameId,
                   gr.thumbs_up as ThumbsUp,
                   gr.status as Status,
                   gr.create_time as CreateTime,
                   rv.version_no as VersionNo,
                   rv.is_recommend as IsRecommendNumber,
                   rv.content as Content,
                   rv.create_time as VersionCreateTime
              from game_review gr
              join player p on p.user_id = gr.user_id
              join latest_version lv on lv.review_id = gr.review_id
              join review_version rv
                on rv.review_id = lv.review_id
               and rv.version_no = lv.version_no
             where {{whereClause}}
             order by gr.create_time desc, gr.review_id desc
          )
         where rownum <= :Limit
        """;

    private static string NormalizeRequired(string? value, string fieldName)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? throw new ArgumentException($"{fieldName} is required.")
            : normalized;
    }

    private static int ToNumber(bool value) => value ? 1 : 0;

    private sealed class ReviewListRow
    {
        public string ReviewId { get; init; } = "";
        public string UserId { get; init; } = "";
        public string Nickname { get; init; } = "";
        public string GameId { get; init; } = "";
        public int ThumbsUp { get; init; }
        public string Status { get; init; } = "";
        public DateTime CreateTime { get; init; }
        public int VersionNo { get; init; }
        public int IsRecommendNumber { get; init; }
        public string Content { get; init; } = "";
        public DateTime VersionCreateTime { get; init; }

        public ReviewListItem ToItem() => new(
            ReviewId,
            UserId,
            Nickname,
            GameId,
            ThumbsUp,
            Status,
            CreateTime,
            VersionNo,
            IsRecommendNumber == 1,
            Content,
            VersionCreateTime);
    }

    private sealed class ReviewVersionRow
    {
        public string VersionId { get; init; } = "";
        public string ReviewId { get; init; } = "";
        public int VersionNo { get; init; }
        public int IsRecommendNumber { get; init; }
        public string Content { get; init; } = "";
        public DateTime CreateTime { get; init; }

        public ReviewVersionItem ToItem() => new(VersionId, ReviewId, VersionNo, IsRecommendNumber == 1, Content, CreateTime);
    }
}