namespace SteamPlatform.Api.Features.Notices;

public sealed class SysNotice
{
    public string NoticeId { get; set; } = "";
    public string PublisherType { get; set; } = "";
    public string? PublisherId { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public int Priority { get; set; }
    public string Status { get; set; } = "";
    public DateTime PublishTime { get; set; }
    public DateTime? ExpireTime { get; set; }
}

public sealed record CreateNoticeRequest(string? PublisherType, string? PublisherId, string Title, string Content, int Priority, DateTime? ExpireTime);

public sealed record UpdateNoticeRequest(string Title, string Content, int Priority, string Status, DateTime? ExpireTime);

public interface INoticeRepository
{
    Task<IReadOnlyList<SysNotice>> ListPublishedAsync(int limit, CancellationToken cancellationToken);
    Task<SysNotice> CreateAsync(CreateNoticeRequest request, CancellationToken cancellationToken);
    Task<SysNotice> UpdateAsync(string noticeId, UpdateNoticeRequest request, CancellationToken cancellationToken);
}
