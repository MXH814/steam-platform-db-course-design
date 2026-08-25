using SteamPlatform.Application.Social;
using SteamPlatform.Shared;

namespace SteamPlatform.Api.Tests;

public sealed class SocialServiceTests
{
    [Fact]
    public async Task Send_message_notifies_only_the_recipient_with_persisted_payload()
    {
        var message = new DirectMessageItem("MSG001", "FR001", "P001", "Alice", "hello", DateTime.UtcNow, null);
        var notification = new UserNotificationItem("NTF001", "DIRECT_MESSAGE", "new", "hello", null, false, DateTime.UtcNow, null);
        var repository = new SocialRepositoryStub
        {
            MessageResult = new MessageDispatchResult(message, "P002", notification)
        };
        var notifier = new RealtimeNotifierSpy();
        var service = new SocialService(repository, notifier);

        var result = await service.SendMessageAsync("P001", "P002", new SendDirectMessageRequest(" hello "), CancellationToken.None);

        Assert.Same(message, result);
        Assert.Equal("hello", repository.LastContent);
        Assert.Collection(notifier.Events,
            entry => { Assert.Equal(("P002", "DirectMessageReceived"), (entry.UserId, entry.EventName)); Assert.Same(message, entry.Payload); },
            entry => { Assert.Equal(("P002", "NotificationReceived"), (entry.UserId, entry.EventName)); Assert.Same(notification, entry.Payload); });
    }

    [Fact]
    public async Task Review_vote_is_normalized_before_persistence()
    {
        var interaction = new ReviewInteractionItem("REV001", "UP", false, false, false, 1, 0, 0, 0);
        var repository = new SocialRepositoryStub
        {
            ReviewResult = new ReviewInteractionResult(interaction, null, null)
        };
        var service = new SocialService(repository, new RealtimeNotifierSpy());

        await service.SetReviewInteractionAsync("P001", "REV001", new ReviewInteractionRequest(" up ", false, false, false), CancellationToken.None);

        Assert.Equal("UP", repository.LastReviewRequest?.VoteType);
    }

    [Fact]
    public async Task Review_vote_rejects_unknown_value_before_persistence()
    {
        var repository = new SocialRepositoryStub();
        var service = new SocialService(repository, new RealtimeNotifierSpy());

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.SetReviewInteractionAsync("P001", "REV001", new ReviewInteractionRequest("MAYBE", false, false, false), CancellationToken.None));

        Assert.Equal("INVALID_REVIEW_VOTE", exception.Code);
        Assert.Null(repository.LastReviewRequest);
    }

    private sealed class RealtimeNotifierSpy : ISocialRealtimeNotifier
    {
        public List<(string UserId, string EventName, object Payload)> Events { get; } = [];

        public Task NotifyUserAsync(string userId, string eventName, object payload, CancellationToken cancellationToken)
        {
            Events.Add((userId, eventName, payload));
            return Task.CompletedTask;
        }
    }

    private sealed class SocialRepositoryStub : ISocialRepository
    {
        public MessageDispatchResult? MessageResult { get; init; }
        public ReviewInteractionResult? ReviewResult { get; init; }
        public string? LastContent { get; private set; }
        public ReviewInteractionRequest? LastReviewRequest { get; private set; }

        public Task<MessageDispatchResult> SendMessageAsync(string userId, string friendUserId, string content, CancellationToken cancellationToken)
        {
            LastContent = content;
            return Task.FromResult(MessageResult ?? throw new InvalidOperationException("Message result was not configured."));
        }

        public Task<ReviewInteractionResult> SetReviewInteractionAsync(string userId, string reviewId, ReviewInteractionRequest request, CancellationToken cancellationToken)
        {
            LastReviewRequest = request;
            return Task.FromResult(ReviewResult ?? throw new InvalidOperationException("Review result was not configured."));
        }

        public Task<IReadOnlyList<FriendListItem>> ListFriendsAsync(string userId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FriendMutationResult> RequestFriendAsync(string userId, string targetUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FriendMutationResult> AcceptFriendAsync(string userId, string relationId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<DirectMessageItem>> ListMessagesAsync(string userId, string friendUserId, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<ReviewInteractionItem>> ListReviewInteractionsAsync(string gameId, string? userId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<WorkshopItemView>> ListWorkshopItemsAsync(string gameId, string? userId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<WorkshopItemView> SetWorkshopSubscriptionAsync(string userId, string workshopItemId, bool isSubscribed, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<UserNotificationItem>> ListNotificationsAsync(string userId, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<UserNotificationItem> MarkNotificationReadAsync(string userId, string notificationId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
