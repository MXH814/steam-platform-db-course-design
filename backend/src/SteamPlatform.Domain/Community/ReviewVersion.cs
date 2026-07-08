namespace SteamPlatform.Domain.Community;

public sealed class ReviewVersion
{
    public string VersionId { get; set; } = "";
    public string ReviewId { get; set; } = "";
    public int VersionNo { get; set; }
    public bool IsRecommend { get; set; }
    public string Content { get; set; } = "";
    public DateTime CreateTime { get; set; }
}