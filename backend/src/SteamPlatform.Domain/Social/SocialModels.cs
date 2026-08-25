namespace SteamPlatform.Domain.Social;

public sealed record FriendRelation(
    string RelationId,
    string UserLowId,
    string UserHighId,
    string RequestedBy,
    string Status,
    DateTime CreatedAt,
    DateTime? RespondedAt);

public sealed record DirectMessage(
    string MessageId,
    string RelationId,
    string SenderId,
    string Content,
    string Status,
    DateTime SentAt,
    DateTime? ReadAt);

public sealed record ReviewReaction(
    string ReviewId,
    string UserId,
    string? VoteType,
    bool IsStarred,
    bool IsFunny,
    bool IsAwarded,
    DateTime UpdatedAt);

public sealed record WorkshopPublication(
    string WorkshopItemId,
    string GameId,
    string? CreatorUserId,
    string Title,
    string Category,
    string Summary,
    string Details,
    string? ImageUrl,
    string Status,
    DateTime UpdatedAt);

public sealed record PlayerNotification(
    string NotificationId,
    string UserId,
    string NotificationType,
    string Title,
    string Message,
    string? TargetUrl,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt);
