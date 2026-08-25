using SteamPlatform.Application.Engagement;
using SteamPlatform.Application.Social;
using SteamPlatform.Shared;

namespace SteamPlatform.Api.Tests;

public sealed class EngagementServiceTests
{
    [Fact]
    public async Task Create_trade_offer_normalizes_items_and_notifies_recipient()
    {
        var offer = TradeOffer();
        var notification = new UserNotificationItem("NTF001", "TRADE_OFFER", "trade", "message", "/trade-offers", false, DateTime.UtcNow, null);
        var repository = new EngagementRepositoryStub
        {
            CreateResult = new TradeOfferMutationResult(offer, "P002", notification)
        };
        var notifier = new RealtimeNotifierSpy();
        var service = new EngagementService(repository, notifier);

        var result = await service.CreateTradeOfferAsync(
            "P001",
            new CreateTradeOfferRequest(" P002 ", [" ITEM001 ", "ITEM001"], [" ITEM002 "], " hello "),
            CancellationToken.None);

        Assert.Same(offer, result);
        Assert.Equal(["ITEM001"], repository.LastCreateRequest?.OfferedItemIds);
        Assert.Equal(["ITEM002"], repository.LastCreateRequest?.RequestedItemIds);
        Assert.Equal("hello", repository.LastCreateRequest?.Message);
        Assert.Collection(notifier.Events,
            entry => Assert.Equal(("P002", "TradeOfferReceived"), (entry.UserId, entry.EventName)),
            entry => Assert.Equal(("P002", "NotificationReceived"), (entry.UserId, entry.EventName)));
    }

    [Fact]
    public async Task Create_trade_offer_rejects_self_and_cross_side_duplicates()
    {
        var service = new EngagementService(new EngagementRepositoryStub(), new RealtimeNotifierSpy());

        var self = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateTradeOfferAsync(
            "P001", new CreateTradeOfferRequest("P001", ["ITEM001"], ["ITEM002"], null), CancellationToken.None));
        Assert.Equal("CANNOT_TRADE_SELF", self.Code);

        var duplicate = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateTradeOfferAsync(
            "P001", new CreateTradeOfferRequest("P002", ["ITEM001"], ["ITEM001"], null), CancellationToken.None));
        Assert.Equal("DUPLICATE_TRADE_ITEM", duplicate.Code);
    }

    [Fact]
    public async Task Profile_presets_and_community_reactions_are_strictly_validated()
    {
        var repository = new EngagementRepositoryStub();
        var service = new EngagementService(repository, new RealtimeNotifierSpy());

        var profile = await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateProfileAsync(
            "P001",
            new UpdatePlayerProfileRequest(null, null, "UPLOADED_FILE", "BACKGROUND_CS2", "STEAM_BLUE", null, "PUBLIC"),
            CancellationToken.None));
        Assert.Equal("INVALID_ENUM_VALUE", profile.Code);

        var reaction = await Assert.ThrowsAsync<BusinessRuleException>(() => service.SetPostReactionAsync(
            "P001", "POST001", new SetPostReactionRequest("DISLIKE"), CancellationToken.None));
        Assert.Equal("INVALID_POST_REACTION", reaction.Code);
    }

    [Fact]
    public async Task Discussion_reply_dispatches_realtime_and_persisted_notification()
    {
        var topic = Topic();
        var notification = new UserNotificationItem("NTF002", "COMMUNITY_REPLY", "reply", "message", "/community", false, DateTime.UtcNow, null);
        var repository = new EngagementRepositoryStub
        {
            ReplyResult = new DiscussionReplyMutationResult(topic, "P001", notification)
        };
        var notifier = new RealtimeNotifierSpy();
        var service = new EngagementService(repository, notifier);

        var result = await service.ReplyToDiscussionAsync("P002", "TOPIC001", new CreateDiscussionReplyRequest(" reply body "), CancellationToken.None);

        Assert.Same(topic, result);
        Assert.Equal("reply body", repository.LastReplyBody);
        Assert.Collection(notifier.Events,
            entry => Assert.Equal(("P001", "DiscussionReplyReceived"), (entry.UserId, entry.EventName)),
            entry => Assert.Equal(("P001", "NotificationReceived"), (entry.UserId, entry.EventName)));
    }

    private static TradeOfferView TradeOffer() => new(
        "TO001", "P001", "Alice", "P002", "Bob", null, "PENDING", DateTime.UtcNow, null, 0, [], [], false, false, true);

    private static DiscussionTopicView Topic() => new(
        "TOPIC001", "GAME_CS2", "Counter-Strike 2", "P001", "Alice", "AVATAR_BLUE",
        "Title", "Body", "OPEN", DateTime.UtcNow, DateTime.UtcNow, 1, []);

    private sealed class RealtimeNotifierSpy : ISocialRealtimeNotifier
    {
        public List<(string UserId, string EventName, object Payload)> Events { get; } = [];
        public Task NotifyUserAsync(string userId, string eventName, object payload, CancellationToken cancellationToken)
        {
            Events.Add((userId, eventName, payload));
            return Task.CompletedTask;
        }
    }

    private sealed class EngagementRepositoryStub : IEngagementRepository
    {
        public TradeOfferMutationResult? CreateResult { get; init; }
        public DiscussionReplyMutationResult? ReplyResult { get; init; }
        public CreateTradeOfferRequest? LastCreateRequest { get; private set; }
        public string? LastReplyBody { get; private set; }

        public Task<TradeOfferMutationResult> CreateTradeOfferAsync(string userId, CreateTradeOfferRequest request, CancellationToken cancellationToken)
        {
            LastCreateRequest = request;
            return Task.FromResult(CreateResult ?? throw new InvalidOperationException("Create result is not configured."));
        }

        public Task<DiscussionReplyMutationResult> ReplyToDiscussionAsync(string userId, string topicId, string body, CancellationToken cancellationToken)
        {
            LastReplyBody = body;
            return Task.FromResult(ReplyResult ?? throw new InvalidOperationException("Reply result is not configured."));
        }

        public Task<PlayerProfileView> GetProfileAsync(string userId, string? viewerUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PlayerProfileView> UpdateProfileAsync(string userId, UpdatePlayerProfileRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PlayerProfileView> SetFeaturedBadgeAsync(string userId, string badgeId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<PlayerSearchItem>> SearchPlayersAsync(string userId, string query, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<TradeOfferView>> ListTradeOffersAsync(string userId, string? status, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<TradeableInventoryItemView>> ListTradeableInventoryAsync(string userId, string targetUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TradeOfferMutationResult> RespondTradeOfferAsync(string userId, string offerId, string action, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<CommunityPostView>> ListCommunityPostsAsync(string? viewerUserId, string? gameId, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<CommunityPostView> CreateCommunityPostAsync(string userId, CreateCommunityPostRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<CommunityPostView> SetPostReactionAsync(string userId, string postId, string? reactionType, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<DiscussionTopicView>> ListDiscussionTopicsAsync(string gameId, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<DiscussionTopicView> GetDiscussionTopicAsync(string topicId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<DiscussionTopicView> CreateDiscussionTopicAsync(string userId, CreateDiscussionTopicRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
