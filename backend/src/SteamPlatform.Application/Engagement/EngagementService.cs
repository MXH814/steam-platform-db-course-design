using SteamPlatform.Application.Social;
using SteamPlatform.Shared;

namespace SteamPlatform.Application.Engagement;

public sealed class EngagementService(
    IEngagementRepository repository,
    ISocialRealtimeNotifier realtimeNotifier) : IEngagementService
{
    private static readonly HashSet<string> AvatarKeys = ["AVATAR_BLUE", "AVATAR_ORANGE", "AVATAR_GREEN", "AVATAR_PURPLE"];
    private static readonly HashSet<string> BackgroundKeys = ["BACKGROUND_CS2", "BACKGROUND_DST", "BACKGROUND_LIBRARY"];
    private static readonly HashSet<string> ThemeKeys = ["STEAM_BLUE", "TACTICAL_ORANGE", "SURVIVAL_GREEN"];
    private static readonly HashSet<string> Visibilities = ["PUBLIC", "FRIENDS", "PRIVATE"];
    private static readonly HashSet<string> PostTypes = ["STATUS", "ACHIEVEMENT", "SCREENSHOT", "TRADE"];
    private static readonly HashSet<string> PostVisibilities = ["PUBLIC", "FRIENDS"];
    private static readonly HashSet<string> OfferStatuses = ["PENDING", "ACCEPTED", "DECLINED", "CANCELED", "EXPIRED"];
    private static readonly HashSet<string> OfferActions = ["ACCEPT", "DECLINE", "CANCEL"];
    private static readonly HashSet<string> ReactionTypes = ["LIKE", "AWARD"];

    public Task<PlayerProfileView> GetProfileAsync(string userId, string? viewerUserId, CancellationToken cancellationToken) =>
        repository.GetProfileAsync(Required(userId, nameof(userId)), Optional(viewerUserId), cancellationToken);

    public Task<PlayerProfileView> UpdateProfileAsync(string userId, UpdatePlayerProfileRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalized = request with
        {
            Headline = OptionalLimited(request.Headline, 120, nameof(request.Headline)),
            Bio = OptionalLimited(request.Bio, 2000, nameof(request.Bio)),
            AvatarKey = EnumValue(request.AvatarKey, AvatarKeys, nameof(request.AvatarKey)),
            BackgroundKey = EnumValue(request.BackgroundKey, BackgroundKeys, nameof(request.BackgroundKey)),
            ThemeKey = EnumValue(request.ThemeKey, ThemeKeys, nameof(request.ThemeKey)),
            ShowcaseGameId = OptionalLimited(request.ShowcaseGameId, 32, nameof(request.ShowcaseGameId)),
            ProfileVisibility = EnumValue(request.ProfileVisibility, Visibilities, nameof(request.ProfileVisibility))
        };
        return repository.UpdateProfileAsync(Required(userId, nameof(userId)), normalized, cancellationToken);
    }

    public Task<PlayerProfileView> SetFeaturedBadgeAsync(string userId, SetFeaturedBadgeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return repository.SetFeaturedBadgeAsync(Required(userId, nameof(userId)), Required(request.BadgeId, nameof(request.BadgeId)), cancellationToken);
    }

    public Task<IReadOnlyList<PlayerSearchItem>> SearchPlayersAsync(string userId, string? query, int limit, CancellationToken cancellationToken) =>
        repository.SearchPlayersAsync(Required(userId, nameof(userId)), OptionalLimited(query, 50, nameof(query)) ?? string.Empty, Math.Clamp(limit, 1, 20), cancellationToken);

    public Task<IReadOnlyList<TradeOfferView>> ListTradeOffersAsync(string userId, string? status, CancellationToken cancellationToken)
    {
        var normalizedStatus = Optional(status)?.ToUpperInvariant();
        if (normalizedStatus is not null && !OfferStatuses.Contains(normalizedStatus))
        {
            throw new BusinessRuleException("INVALID_TRADE_OFFER_STATUS", "Unknown trade-offer status.");
        }

        return repository.ListTradeOffersAsync(Required(userId, nameof(userId)), normalizedStatus, cancellationToken);
    }

    public Task<IReadOnlyList<TradeableInventoryItemView>> ListTradeableInventoryAsync(string userId, string targetUserId, CancellationToken cancellationToken) =>
        repository.ListTradeableInventoryAsync(
            Required(userId, nameof(userId)),
            Required(targetUserId, nameof(targetUserId)),
            cancellationToken);

    public async Task<TradeOfferView> CreateTradeOfferAsync(string userId, CreateTradeOfferRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var senderId = Required(userId, nameof(userId));
        var recipientId = Required(request.RecipientId, nameof(request.RecipientId));
        if (string.Equals(senderId, recipientId, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("CANNOT_TRADE_SELF", "A player cannot send a trade offer to themselves.");
        }

        var offered = DistinctIds(request.OfferedItemIds, nameof(request.OfferedItemIds));
        var requested = DistinctIds(request.RequestedItemIds, nameof(request.RequestedItemIds));
        if (offered.Count == 0 || requested.Count == 0 || offered.Count > 8 || requested.Count > 8)
        {
            throw new BusinessRuleException("INVALID_TRADE_ITEMS", "A trade offer requires one to eight items on each side.");
        }

        if (offered.Intersect(requested, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new BusinessRuleException("DUPLICATE_TRADE_ITEM", "The same item cannot appear on both sides of an offer.");
        }

        var normalized = request with
        {
            RecipientId = recipientId,
            OfferedItemIds = offered,
            RequestedItemIds = requested,
            Message = OptionalLimited(request.Message, 500, nameof(request.Message))
        };
        var result = await repository.CreateTradeOfferAsync(senderId, normalized, cancellationToken);
        await realtimeNotifier.NotifyUserAsync(result.NotifyUserId, "TradeOfferReceived", result.Offer, cancellationToken);
        await realtimeNotifier.NotifyUserAsync(result.NotifyUserId, "NotificationReceived", result.Notification, cancellationToken);
        return result.Offer;
    }

    public async Task<TradeOfferView> RespondTradeOfferAsync(string userId, string offerId, TradeOfferActionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var action = EnumValue(request.Action, OfferActions, nameof(request.Action));
        var result = await repository.RespondTradeOfferAsync(Required(userId, nameof(userId)), Required(offerId, nameof(offerId)), action, cancellationToken);
        await realtimeNotifier.NotifyUserAsync(result.NotifyUserId, "TradeOfferChanged", result.Offer, cancellationToken);
        await realtimeNotifier.NotifyUserAsync(result.NotifyUserId, "NotificationReceived", result.Notification, cancellationToken);
        return result.Offer;
    }

    public Task<IReadOnlyList<CommunityPostView>> ListCommunityPostsAsync(string? viewerUserId, string? gameId, int limit, CancellationToken cancellationToken) =>
        repository.ListCommunityPostsAsync(Optional(viewerUserId), OptionalLimited(gameId, 32, nameof(gameId)), Math.Clamp(limit, 1, 50), cancellationToken);

    public Task<CommunityPostView> CreateCommunityPostAsync(string userId, CreateCommunityPostRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var mediaUrl = OptionalLimited(request.MediaUrl, 255, nameof(request.MediaUrl));
        if (mediaUrl is not null && !mediaUrl.StartsWith("/assets/", StringComparison.Ordinal) && !Uri.TryCreate(mediaUrl, UriKind.Absolute, out var uri))
        {
            throw new BusinessRuleException("INVALID_MEDIA_URL", "MediaUrl must be a project asset path or an absolute URL.");
        }

        var normalized = request with
        {
            GameId = OptionalLimited(request.GameId, 32, nameof(request.GameId)),
            PostType = EnumValue(request.PostType, PostTypes, nameof(request.PostType)),
            Content = RequiredLimited(request.Content, 1000, nameof(request.Content)),
            MediaUrl = mediaUrl,
            Visibility = EnumValue(request.Visibility, PostVisibilities, nameof(request.Visibility))
        };
        return repository.CreateCommunityPostAsync(Required(userId, nameof(userId)), normalized, cancellationToken);
    }

    public Task<CommunityPostView> SetPostReactionAsync(string userId, string postId, SetPostReactionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var reaction = Optional(request.ReactionType)?.ToUpperInvariant();
        if (reaction is not null && !ReactionTypes.Contains(reaction))
        {
            throw new BusinessRuleException("INVALID_POST_REACTION", "ReactionType must be LIKE, AWARD, or null.");
        }

        return repository.SetPostReactionAsync(Required(userId, nameof(userId)), Required(postId, nameof(postId)), reaction, cancellationToken);
    }

    public Task<IReadOnlyList<DiscussionTopicView>> ListDiscussionTopicsAsync(string gameId, int limit, CancellationToken cancellationToken) =>
        repository.ListDiscussionTopicsAsync(Required(gameId, nameof(gameId)), Math.Clamp(limit, 1, 50), cancellationToken);

    public Task<DiscussionTopicView> GetDiscussionTopicAsync(string topicId, CancellationToken cancellationToken) =>
        repository.GetDiscussionTopicAsync(Required(topicId, nameof(topicId)), cancellationToken);

    public Task<DiscussionTopicView> CreateDiscussionTopicAsync(string userId, CreateDiscussionTopicRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalized = request with
        {
            GameId = RequiredLimited(request.GameId, 32, nameof(request.GameId)),
            Title = RequiredLimited(request.Title, 160, nameof(request.Title)),
            Body = RequiredLimited(request.Body, 4000, nameof(request.Body))
        };
        return repository.CreateDiscussionTopicAsync(Required(userId, nameof(userId)), normalized, cancellationToken);
    }

    public async Task<DiscussionTopicView> ReplyToDiscussionAsync(string userId, string topicId, CreateDiscussionReplyRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await repository.ReplyToDiscussionAsync(
            Required(userId, nameof(userId)),
            Required(topicId, nameof(topicId)),
            RequiredLimited(request.Body, 4000, nameof(request.Body)),
            cancellationToken);
        if (result.NotifyUserId is not null && result.Notification is not null)
        {
            await realtimeNotifier.NotifyUserAsync(result.NotifyUserId, "DiscussionReplyReceived", result.Topic, cancellationToken);
            await realtimeNotifier.NotifyUserAsync(result.NotifyUserId, "NotificationReceived", result.Notification, cancellationToken);
        }

        return result.Topic;
    }

    private static IReadOnlyList<string> DistinctIds(IReadOnlyList<string>? values, string name) =>
        values is null
            ? throw new ArgumentNullException(name)
            : values.Select(value => Required(value, name)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    private static string EnumValue(string? value, HashSet<string> allowed, string name)
    {
        var normalized = Required(value, name).ToUpperInvariant();
        return allowed.Contains(normalized)
            ? normalized
            : throw new BusinessRuleException("INVALID_ENUM_VALUE", $"{name} has an unsupported value.");
    }

    private static string RequiredLimited(string? value, int maxLength, string name)
    {
        var normalized = Required(value, name);
        return normalized.Length <= maxLength
            ? normalized
            : throw new BusinessRuleException("TEXT_TOO_LONG", $"{name} cannot exceed {maxLength} characters.");
    }

    private static string? OptionalLimited(string? value, int maxLength, string name)
    {
        var normalized = Optional(value);
        return normalized is null || normalized.Length <= maxLength
            ? normalized
            : throw new BusinessRuleException("TEXT_TOO_LONG", $"{name} cannot exceed {maxLength} characters.");
    }

    private static string Required(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{name} is required.") : value.Trim();

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
