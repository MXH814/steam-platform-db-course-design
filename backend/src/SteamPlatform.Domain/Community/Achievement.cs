namespace SteamPlatform.Domain.Community;

public sealed class Achievement
{
    public string AchId { get; set; } = "";
    public string GameId { get; set; } = "";
    public string AchName { get; set; } = "";
    public string? Description { get; set; }
    public decimal? GlobalRate { get; set; }
}