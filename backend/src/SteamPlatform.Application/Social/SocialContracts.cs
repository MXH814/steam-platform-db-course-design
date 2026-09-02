namespace SteamPlatform.Application.Social;

public sealed record FriendListItem(
    string RelationId,
    string UserId,
    string Nickname,
    string RelationStatus,
    bool IsIncomingRequest,
    string? LatestMessage,
    DateTime? LatestMessageAt);

public sealed record FriendGameActivityItem(
    string UserId,
    string Nickname,
    int PlayMinutes,
    DateTime? LastPlayTime);

public sealed record DirectMessageItem(
    string MessageId,
    string RelationId,
    string SenderId,
    string SenderNickname,
    string Content,
    DateTime SentAt,
    DateTime? ReadAt);

public sealed record SendDirectMessageRequest(string Content);

public sealed record ReviewInteractionRequest(
    string? VoteType,
    bool IsStarred,
    bool IsFunny,
    bool IsAwarded);

public sealed record ReviewInteractionItem(
    string ReviewId,
    string? VoteType,
    bool IsStarred,
    bool IsFunny,
    bool IsAwarded,
    int UpVotes,
    int DownVotes,
    int FunnyCount,
    int AwardCount);

public sealed record WorkshopItemView(
    string WorkshopItemId,
    string GameId,
    string? CreatorUserId,
    string? CreatorNickname,
    string Title,
    string Category,
    string Summary,
    string Details,
    string? ImageUrl,
    int SubscriberCount,
    bool IsSubscribed,
    DateTime UpdatedAt);

public sealed record WorkshopSubscriptionRequest(bool IsSubscribed);

public sealed record UserNotificationItem(
    string NotificationId,
    string NotificationType,
    string Title,
    string Message,
    string? TargetUrl,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt);

public sealed record FriendMutationResult(FriendListItem Friend, string NotifyUserId, UserNotificationItem Notification);

public sealed record MessageDispatchResult(DirectMessageItem Message, string RecipientUserId, UserNotificationItem Notification);

public sealed record ReviewInteractionResult(ReviewInteractionItem Interaction, string? ReviewOwnerId, UserNotificationItem? Notification);

public interface ISocialRepository
{
    Task<IReadOnlyList<FriendListItem>> ListFriendsAsync(string userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FriendGameActivityItem>> ListFriendsWhoPlayAsync(string userId, string gameId, CancellationToken cancellationToken);
    Task<FriendMutationResult> RequestFriendAsync(string userId, string targetUserId, CancellationToken cancellationToken);
    Task<FriendMutationResult> AcceptFriendAsync(string userId, string relationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DirectMessageItem>> ListMessagesAsync(string userId, string friendUserId, int limit, CancellationToken cancellationToken);
    Task<MessageDispatchResult> SendMessageAsync(string userId, string friendUserId, string content, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReviewInteractionItem>> ListReviewInteractionsAsync(string gameId, string? userId, CancellationToken cancellationToken);
    Task<ReviewInteractionResult> SetReviewInteractionAsync(string userId, string reviewId, ReviewInteractionRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkshopItemView>> ListWorkshopItemsAsync(string gameId, string? userId, CancellationToken cancellationToken);
    Task<WorkshopItemView> SetWorkshopSubscriptionAsync(string userId, string workshopItemId, bool isSubscribed, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserNotificationItem>> ListNotificationsAsync(string userId, int limit, CancellationToken cancellationToken);
    Task<UserNotificationItem> MarkNotificationReadAsync(string userId, string notificationId, CancellationToken cancellationToken);
}

public interface ISocialRealtimeNotifier
{
    Task NotifyUserAsync(string userId, string eventName, object payload, CancellationToken cancellationToken);
}

public interface ISocialService
{
    Task<IReadOnlyList<FriendListItem>> ListFriendsAsync(string userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FriendGameActivityItem>> ListFriendsWhoPlayAsync(string userId, string gameId, CancellationToken cancellationToken);
    Task<FriendListItem> RequestFriendAsync(string userId, string targetUserId, CancellationToken cancellationToken);
    Task<FriendListItem> AcceptFriendAsync(string userId, string relationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DirectMessageItem>> ListMessagesAsync(string userId, string friendUserId, int limit, CancellationToken cancellationToken);
    Task<DirectMessageItem> SendMessageAsync(string userId, string friendUserId, SendDirectMessageRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReviewInteractionItem>> ListReviewInteractionsAsync(string gameId, string? userId, CancellationToken cancellationToken);
    Task<ReviewInteractionItem> SetReviewInteractionAsync(string userId, string reviewId, ReviewInteractionRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkshopItemView>> ListWorkshopItemsAsync(string gameId, string? userId, CancellationToken cancellationToken);
    Task<WorkshopItemView> SetWorkshopSubscriptionAsync(string userId, string workshopItemId, WorkshopSubscriptionRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserNotificationItem>> ListNotificationsAsync(string userId, int limit, CancellationToken cancellationToken);
    Task<UserNotificationItem> MarkNotificationReadAsync(string userId, string notificationId, CancellationToken cancellationToken);
}
