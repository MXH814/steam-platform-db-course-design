namespace SteamPlatform.Domain.Community;

public sealed class PlayerAchievement
{
    public string UnlockId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string AchId { get; set; } = "";
    public DateTime UnlockTime { get; set; }
}