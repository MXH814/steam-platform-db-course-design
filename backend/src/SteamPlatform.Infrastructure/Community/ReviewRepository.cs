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
            """
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
                 where gr.game_id = :GameId
                   and gr.status = 'VISIBLE'
                 order by gr.create_time desc, gr.review_id desc
              )
             where rownum <= :Limit
            """,
            new { GameId = normalizedGameId, Limit = normalizedLimit },
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToItem()).ToList();
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

    private static string NormalizeRequired(string? value, string fieldName)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? throw new ArgumentException($"{fieldName} is required.")
            : normalized;
    }

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