using SteamPlatform.Domain.Notices;

namespace SteamPlatform.Application.Notices;

public sealed record CreateNoticeRequest(string? PublisherType, string? PublisherId, string Title, string Content, int Priority, DateTime? ExpireTime);

public sealed record UpdateNoticeRequest(string Title, string Content, int Priority, string Status, DateTime? ExpireTime);

public interface INoticeRepository
{
    Task<IReadOnlyList<SysNotice>> ListPublishedAsync(int limit, CancellationToken cancellationToken);
    Task<SysNotice> CreateAsync(CreateNoticeRequest request, CancellationToken cancellationToken);
    Task<SysNotice> UpdateAsync(string noticeId, UpdateNoticeRequest request, CancellationToken cancellationToken);
}
