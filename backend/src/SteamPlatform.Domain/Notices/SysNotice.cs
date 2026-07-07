namespace SteamPlatform.Domain.Notices;

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
