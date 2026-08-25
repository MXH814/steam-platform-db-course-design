using SteamPlatform.Application.Social;

namespace SteamPlatform.Application.Engagement;

public sealed record ProfileBadgeView(
    string BadgeId,
    string BadgeName,
    string Description,
    string IconKey,
    int XpValue,
    string Rarity,
    DateTime EarnedAt,
    bool IsFeatured);

public sealed record PlayerProfileView(
    string UserId,
    string Nickname,
    string? Headline,
    string? Bio,
    string AvatarKey,
    string BackgroundKey,
    string ThemeKey,
    string? ShowcaseGameId,
    string? ShowcaseGameName,
    string ProfileVisibility,
    int FriendCount,
    int TotalXp,
    IReadOnlyList<ProfileBadgeView> Badges,
    DateTime UpdatedAt,
    bool IsOwnProfile);

public sealed record UpdatePlayerProfileRequest(
    string? Headline,
    string? Bio,
    string AvatarKey,
    string BackgroundKey,
    string ThemeKey,
    string? ShowcaseGameId,
    string ProfileVisibility);

public sealed record SetFeaturedBadgeRequest(string BadgeId);

public sealed record PlayerSearchItem(
    string UserId,
    string Nickname,
    string AvatarKey,
    string? Headline,
    string? RelationId,
    string? RelationStatus,
    bool IsIncomingRequest);

public sealed record TradeOfferItemView(
    string ItemId,
    string TemplateId,
    string GameId,
    string ItemName,
    string Rarity,
    string? ImageUrl,
    decimal? WearRating,
    string ItemRole,
    string OwnerIdAtCreate);

public sealed record TradeableInventoryItemView(
    string ItemId,
    string TemplateId,
    string GameId,
    string ItemName,
    string Rarity,
    string? ImageUrl,
    decimal? WearRating);

public sealed record TradeOfferView(
    string OfferId,
    string SenderId,
    string SenderNickname,
    string RecipientId,
    string RecipientNickname,
    string? Message,
    string Status,
    DateTime CreatedAt,
    DateTime? RespondedAt,
    long Version,
    IReadOnlyList<TradeOfferItemView> OfferedItems,
    IReadOnlyList<TradeOfferItemView> RequestedItems,
    bool CanAccept,
    bool CanDecline,
    bool CanCancel);

public sealed record CreateTradeOfferRequest(
    string RecipientId,
    IReadOnlyList<string> OfferedItemIds,
    IReadOnlyList<string> RequestedItemIds,
    string? Message);

public sealed record TradeOfferActionRequest(string Action);

public sealed record TradeOfferMutationResult(
    TradeOfferView Offer,
    string NotifyUserId,
    UserNotificationItem Notification);

public sealed record CommunityPostView(
    string PostId,
    string AuthorId,
    string AuthorNickname,
    string AvatarKey,
    string? GameId,
    string? GameName,
    string PostType,
    string Content,
    string? MediaUrl,
    string Visibility,
    DateTime CreatedAt,
    int LikeCount,
    int AwardCount,
    string? MyReaction);

public sealed record CreateCommunityPostRequest(
    string? GameId,
    string PostType,
    string Content,
    string? MediaUrl,
    string Visibility);

public sealed record SetPostReactionRequest(string? ReactionType);

public sealed record DiscussionReplyView(
    string ReplyId,
    string AuthorId,
    string AuthorNickname,
    string AvatarKey,
    string Body,
    DateTime CreatedAt);

public sealed record DiscussionTopicView(
    string TopicId,
    string GameId,
    string GameName,
    string AuthorId,
    string AuthorNickname,
    string AvatarKey,
    string Title,
    string Body,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int ReplyCount,
    IReadOnlyList<DiscussionReplyView> Replies);

public sealed record CreateDiscussionTopicRequest(string GameId, string Title, string Body);
public sealed record CreateDiscussionReplyRequest(string Body);

public sealed record DiscussionReplyMutationResult(
    DiscussionTopicView Topic,
    string? NotifyUserId,
    UserNotificationItem? Notification);

public interface IEngagementRepository
{
    Task<PlayerProfileView> GetProfileAsync(string userId, string? viewerUserId, CancellationToken cancellationToken);
    Task<PlayerProfileView> UpdateProfileAsync(string userId, UpdatePlayerProfileRequest request, CancellationToken cancellationToken);
    Task<PlayerProfileView> SetFeaturedBadgeAsync(string userId, string badgeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlayerSearchItem>> SearchPlayersAsync(string userId, string query, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<TradeOfferView>> ListTradeOffersAsync(string userId, string? status, CancellationToken cancellationToken);
    Task<IReadOnlyList<TradeableInventoryItemView>> ListTradeableInventoryAsync(string userId, string targetUserId, CancellationToken cancellationToken);
    Task<TradeOfferMutationResult> CreateTradeOfferAsync(string userId, CreateTradeOfferRequest request, CancellationToken cancellationToken);
    Task<TradeOfferMutationResult> RespondTradeOfferAsync(string userId, string offerId, string action, CancellationToken cancellationToken);
    Task<IReadOnlyList<CommunityPostView>> ListCommunityPostsAsync(string? viewerUserId, string? gameId, int limit, CancellationToken cancellationToken);
    Task<CommunityPostView> CreateCommunityPostAsync(string userId, CreateCommunityPostRequest request, CancellationToken cancellationToken);
    Task<CommunityPostView> SetPostReactionAsync(string userId, string postId, string? reactionType, CancellationToken cancellationToken);
    Task<IReadOnlyList<DiscussionTopicView>> ListDiscussionTopicsAsync(string gameId, int limit, CancellationToken cancellationToken);
    Task<DiscussionTopicView> GetDiscussionTopicAsync(string topicId, CancellationToken cancellationToken);
    Task<DiscussionTopicView> CreateDiscussionTopicAsync(string userId, CreateDiscussionTopicRequest request, CancellationToken cancellationToken);
    Task<DiscussionReplyMutationResult> ReplyToDiscussionAsync(string userId, string topicId, string body, CancellationToken cancellationToken);
}

public interface IEngagementService
{
    Task<PlayerProfileView> GetProfileAsync(string userId, string? viewerUserId, CancellationToken cancellationToken);
    Task<PlayerProfileView> UpdateProfileAsync(string userId, UpdatePlayerProfileRequest request, CancellationToken cancellationToken);
    Task<PlayerProfileView> SetFeaturedBadgeAsync(string userId, SetFeaturedBadgeRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlayerSearchItem>> SearchPlayersAsync(string userId, string? query, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<TradeOfferView>> ListTradeOffersAsync(string userId, string? status, CancellationToken cancellationToken);
    Task<IReadOnlyList<TradeableInventoryItemView>> ListTradeableInventoryAsync(string userId, string targetUserId, CancellationToken cancellationToken);
    Task<TradeOfferView> CreateTradeOfferAsync(string userId, CreateTradeOfferRequest request, CancellationToken cancellationToken);
    Task<TradeOfferView> RespondTradeOfferAsync(string userId, string offerId, TradeOfferActionRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<CommunityPostView>> ListCommunityPostsAsync(string? viewerUserId, string? gameId, int limit, CancellationToken cancellationToken);
    Task<CommunityPostView> CreateCommunityPostAsync(string userId, CreateCommunityPostRequest request, CancellationToken cancellationToken);
    Task<CommunityPostView> SetPostReactionAsync(string userId, string postId, SetPostReactionRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<DiscussionTopicView>> ListDiscussionTopicsAsync(string gameId, int limit, CancellationToken cancellationToken);
    Task<DiscussionTopicView> GetDiscussionTopicAsync(string topicId, CancellationToken cancellationToken);
    Task<DiscussionTopicView> CreateDiscussionTopicAsync(string userId, CreateDiscussionTopicRequest request, CancellationToken cancellationToken);
    Task<DiscussionTopicView> ReplyToDiscussionAsync(string userId, string topicId, CreateDiscussionReplyRequest request, CancellationToken cancellationToken);
}
