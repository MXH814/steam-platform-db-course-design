namespace SteamPlatform.Domain.Community;

public sealed class GameReview
{
    public string ReviewId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string GameId { get; set; } = "";
    public int ThumbsUp { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreateTime { get; set; }
}