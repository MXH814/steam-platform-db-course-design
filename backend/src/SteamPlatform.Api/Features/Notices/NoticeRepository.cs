using Dapper;
using SteamPlatform.Api.Data;
using SteamPlatform.Api.Infrastructure;

namespace SteamPlatform.Api.Features.Notices;

public sealed class NoticeRepository(IDbConnectionFactory connectionFactory) : INoticeRepository
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    public async Task<IReadOnlyList<SysNotice>> ListPublishedAsync(int limit, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var notices = await connection.QueryAsync<SysNotice>(new CommandDefinition(
            """
            select *
            from (
              select *
                from sys_notice
               where status = 'PUBLISHED'
                 and (expire_time is null or expire_time > :Now)
               order by priority desc, publish_time desc, notice_id desc
            )
            where rownum <= :Limit
            """,
            new { Limit = Math.Clamp(limit, 1, 100), Now = DateTime.UtcNow },
            cancellationToken: cancellationToken));

        return notices.AsList();
    }

    public async Task<SysNotice> CreateAsync(CreateNoticeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var publisherType = NormalizeRequired(request.PublisherType, nameof(request.PublisherType)).ToUpperInvariant();
        if (publisherType is not ("SYSTEM" or "ADMIN" or "DEVELOPER"))
        {
            throw new ArgumentException("PublisherType must be SYSTEM, ADMIN or DEVELOPER.");
        }

        var publisherId = NormalizeOptional(request.PublisherId);
        if (publisherType != "SYSTEM" && publisherId is null)
        {
            throw new ArgumentException("PublisherId is required for ADMIN and DEVELOPER notices.");
        }

        if (publisherType == "SYSTEM" && publisherId is not null)
        {
            throw new ArgumentException("System notices must not specify PublisherId.");
        }

        var notice = new SysNotice
        {
            NoticeId = IdGenerator.NewId("N"),
            PublisherType = publisherType,
            PublisherId = publisherId,
            Title = NormalizeRequired(request.Title, nameof(request.Title)),
            Content = NormalizeRequired(request.Content, nameof(request.Content)),
            Priority = NormalizePriority(request.Priority),
            Status = "PUBLISHED",
            PublishTime = DateTime.UtcNow,
            ExpireTime = request.ExpireTime
        };

        await using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into sys_notice
              (notice_id, publisher_type, publisher_id, title, content, priority, status, publish_time, expire_time)
            values
              (:NoticeId, :PublisherType, :PublisherId, :Title, :Content, :Priority, :Status, :PublishTime, :ExpireTime)
            """,
            notice,
            cancellationToken: cancellationToken));

        return notice;
    }

    public async Task<SysNotice> UpdateAsync(string noticeId, UpdateNoticeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedNoticeId = NormalizeRequired(noticeId, nameof(noticeId));
        var title = NormalizeRequired(request.Title, nameof(request.Title));
        var content = NormalizeRequired(request.Content, nameof(request.Content));
        var status = NormalizeStatus(request.Status);
        var priority = NormalizePriority(request.Priority);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            update sys_notice
               set title = :Title,
                   content = :Content,
                   priority = :Priority,
                   status = :Status,
                   publish_time = case
                     when :Status = 'PUBLISHED' then SYSTIMESTAMP
                     else publish_time
                   end,
                   expire_time = :ExpireTime
             where notice_id = :NoticeId
            """,
            new
            {
                NoticeId = normalizedNoticeId,
                Title = title,
                Content = content,
                Priority = priority,
                Status = status,
                request.ExpireTime
            },
            cancellationToken: cancellationToken));

        var notice = await connection.QueryFirstOrDefaultAsync<SysNotice>(new CommandDefinition(
            "select * from sys_notice where notice_id = :NoticeId",
            new { NoticeId = normalizedNoticeId },
            cancellationToken: cancellationToken));

        return notice ?? throw new InvalidOperationException("Notice does not exist.");
    }

    private static string NormalizeStatus(string? value)
    {
        var status = NormalizeRequired(value, nameof(UpdateNoticeRequest.Status)).ToUpperInvariant();
        return status is "DRAFT" or "PUBLISHED" or "EXPIRED" or "REVOKED"
            ? status
            : throw new ArgumentException("Notice status must be DRAFT, PUBLISHED, EXPIRED or REVOKED.");
    }

    private static int NormalizePriority(int priority) =>
        priority is >= 0 and <= 9
            ? priority
            : throw new ArgumentException("Priority must be between 0 and 9.");

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
}
