using SteamPlatform.Shared;

namespace SteamPlatform.Application.Social;

public sealed class SocialService(ISocialRepository repository, ISocialRealtimeNotifier realtimeNotifier) : ISocialService
{
    public Task<IReadOnlyList<FriendListItem>> ListFriendsAsync(string userId, CancellationToken cancellationToken) =>
        repository.ListFriendsAsync(Required(userId, nameof(userId)), cancellationToken);

    public Task<IReadOnlyList<FriendGameActivityItem>> ListFriendsWhoPlayAsync(string userId, string gameId, CancellationToken cancellationToken) =>
        repository.ListFriendsWhoPlayAsync(Required(userId, nameof(userId)), Required(gameId, nameof(gameId)), cancellationToken);

    public async Task<FriendListItem> RequestFriendAsync(string userId, string targetUserId, CancellationToken cancellationToken)
    {
        var result = await repository.RequestFriendAsync(Required(userId, nameof(userId)), Required(targetUserId, nameof(targetUserId)), cancellationToken);
        await realtimeNotifier.NotifyUserAsync(result.NotifyUserId, "FriendRequestReceived", result.Notification, cancellationToken);
        return result.Friend;
    }

    public async Task<FriendListItem> AcceptFriendAsync(string userId, string relationId, CancellationToken cancellationToken)
    {
        var result = await repository.AcceptFriendAsync(Required(userId, nameof(userId)), Required(relationId, nameof(relationId)), cancellationToken);
        await realtimeNotifier.NotifyUserAsync(result.NotifyUserId, "FriendRequestAccepted", result.Notification, cancellationToken);
        return result.Friend;
    }

    public Task<IReadOnlyList<DirectMessageItem>> ListMessagesAsync(string userId, string friendUserId, int limit, CancellationToken cancellationToken) =>
        repository.ListMessagesAsync(Required(userId, nameof(userId)), Required(friendUserId, nameof(friendUserId)), Math.Clamp(limit, 1, 100), cancellationToken);

    public async Task<DirectMessageItem> SendMessageAsync(string userId, string friendUserId, SendDirectMessageRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var content = Required(request.Content, nameof(request.Content));
        if (content.Length > 1000)
        {
            throw new BusinessRuleException("MESSAGE_TOO_LONG", "A direct message cannot exceed 1000 characters.");
        }

        var result = await repository.SendMessageAsync(Required(userId, nameof(userId)), Required(friendUserId, nameof(friendUserId)), content, cancellationToken);
        await realtimeNotifier.NotifyUserAsync(result.RecipientUserId, "DirectMessageReceived", result.Message, cancellationToken);
        await realtimeNotifier.NotifyUserAsync(result.RecipientUserId, "NotificationReceived", result.Notification, cancellationToken);
        return result.Message;
    }

    public Task<IReadOnlyList<ReviewInteractionItem>> ListReviewInteractionsAsync(string gameId, string? userId, CancellationToken cancellationToken) =>
        repository.ListReviewInteractionsAsync(Required(gameId, nameof(gameId)), userId, cancellationToken);

    public async Task<ReviewInteractionItem> SetReviewInteractionAsync(string userId, string reviewId, ReviewInteractionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var voteType = request.VoteType?.Trim().ToUpperInvariant();
        if (voteType is not null and not "UP" and not "DOWN")
        {
            throw new BusinessRuleException("INVALID_REVIEW_VOTE", "VoteType must be UP, DOWN, or null.");
        }

        var normalizedRequest = request with { VoteType = voteType };
        var result = await repository.SetReviewInteractionAsync(Required(userId, nameof(userId)), Required(reviewId, nameof(reviewId)), normalizedRequest, cancellationToken);
        if (result.ReviewOwnerId is not null && result.Notification is not null)
        {
            await realtimeNotifier.NotifyUserAsync(result.ReviewOwnerId, "ReviewInteractionReceived", result.Notification, cancellationToken);
        }

        return result.Interaction;
    }

    public Task<IReadOnlyList<WorkshopItemView>> ListWorkshopItemsAsync(string gameId, string? userId, CancellationToken cancellationToken) =>
        repository.ListWorkshopItemsAsync(Required(gameId, nameof(gameId)), userId, cancellationToken);

    public async Task<WorkshopItemView> SetWorkshopSubscriptionAsync(string userId, string workshopItemId, WorkshopSubscriptionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await repository.SetWorkshopSubscriptionAsync(Required(userId, nameof(userId)), Required(workshopItemId, nameof(workshopItemId)), request.IsSubscribed, cancellationToken);
        await realtimeNotifier.NotifyUserAsync(userId, "WorkshopSubscriptionChanged", item, cancellationToken);
        return item;
    }

    public Task<IReadOnlyList<UserNotificationItem>> ListNotificationsAsync(string userId, int limit, CancellationToken cancellationToken) =>
        repository.ListNotificationsAsync(Required(userId, nameof(userId)), Math.Clamp(limit, 1, 100), cancellationToken);

    public Task<UserNotificationItem> MarkNotificationReadAsync(string userId, string notificationId, CancellationToken cancellationToken) =>
        repository.MarkNotificationReadAsync(Required(userId, nameof(userId)), Required(notificationId, nameof(notificationId)), cancellationToken);

    private static string Required(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{name} is required.") : value.Trim();
}
