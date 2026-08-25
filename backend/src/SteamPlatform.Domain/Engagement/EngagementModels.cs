namespace SteamPlatform.Domain.Engagement;

public sealed record PlayerProfile(
    string UserId,
    string? Headline,
    string? Bio,
    string AvatarKey,
    string BackgroundKey,
    string ThemeKey,
    string? ShowcaseGameId,
    string Visibility,
    DateTime UpdatedAt);

public sealed record PlayerBadge(
    string UserId,
    string BadgeId,
    DateTime EarnedAt,
    bool IsFeatured);

public sealed record TradeOffer(
    string OfferId,
    string SenderId,
    string RecipientId,
    string? Message,
    string Status,
    DateTime CreatedAt,
    DateTime? RespondedAt,
    long Version);

public sealed record CommunityPost(
    string PostId,
    string AuthorId,
    string? GameId,
    string PostType,
    string Content,
    string? MediaUrl,
    string Visibility,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record DiscussionTopic(
    string TopicId,
    string GameId,
    string AuthorId,
    string Title,
    string Body,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record DiscussionReply(
    string ReplyId,
    string TopicId,
    string AuthorId,
    string Body,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
