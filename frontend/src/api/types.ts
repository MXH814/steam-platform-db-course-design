export type LoginRole = 'PLAYER' | 'ADMIN' | 'DEVELOPER';

export interface LoginRequest {
  role: LoginRole;
  account: string;
  password: string;
}

export interface RegisterPlayerRequest {
  account: string;
  password: string;
  nickname: string;
}

export interface AuthClaims {
  role: string;
  principalId: string;
  account: string;
  expiresAt: string;
}

export interface AuthResponse {
  token: string;
  claims: AuthClaims;
}

export interface SysNotice {
  noticeId: string;
  publisherType: string;
  publisherId: string | null;
  title: string;
  content: string;
  priority: number;
  status: string;
  publishTime: string;
  expireTime: string | null;
}

export interface CreateNoticeRequest {
  title: string;
  content: string;
  priority: number;
  expireTime: string | null;
}

export interface UpdateNoticeRequest extends CreateNoticeRequest {
  status: 'DRAFT' | 'PUBLISHED' | 'EXPIRED' | 'REVOKED';
}
export interface CreateReviewRequest {
  isRecommend: boolean;
  content: string;
}

export interface UpdateReviewRequest extends CreateReviewRequest {}

export interface ReviewListItem {
  reviewId: string;
  userId: string;
  nickname: string;
  gameId: string;
  thumbsUp: number;
  status: string;
  createTime: string;
  versionNo: number;
  isRecommend: boolean;
  content: string;
  versionCreateTime: string;
}

export interface ReviewVersionItem {
  versionId: string;
  reviewId: string;
  versionNo: number;
  isRecommend: boolean;
  content: string;
  createTime: string;
}

export interface AchievementListItem {
  achId: string;
  gameId: string;
  achName: string;
  description: string | null;
  globalRate: number | null;
  isUnlocked: boolean;
  unlockTime: string | null;
}

export interface UnlockAchievementResult {
  unlockId: string;
  userId: string;
  achId: string;
  alreadyUnlocked: boolean;
  unlockTime: string;
}

export interface FriendListItem {
  relationId: string;
  userId: string;
  nickname: string;
  relationStatus: 'PENDING' | 'ACCEPTED' | 'BLOCKED' | string;
  isIncomingRequest: boolean;
  latestMessage: string | null;
  latestMessageAt: string | null;
}

export interface DirectMessageItem {
  messageId: string;
  relationId: string;
  senderId: string;
  senderNickname: string;
  content: string;
  sentAt: string;
  readAt: string | null;
}

export interface ReviewInteractionItem {
  reviewId: string;
  voteType: 'UP' | 'DOWN' | null;
  isStarred: boolean;
  isFunny: boolean;
  isAwarded: boolean;
  upVotes: number;
  downVotes: number;
  funnyCount: number;
  awardCount: number;
}

export interface WorkshopItemView {
  workshopItemId: string;
  gameId: string;
  creatorUserId: string | null;
  creatorNickname: string | null;
  title: string;
  category: string;
  summary: string;
  details: string;
  imageUrl: string | null;
  subscriberCount: number;
  isSubscribed: boolean;
  updatedAt: string;
}

export interface UserNotificationItem {
  notificationId: string;
  notificationType: string;
  title: string;
  message: string;
  targetUrl: string | null;
  isRead: boolean;
  createdAt: string;
  readAt: string | null;
}

export type ProfileVisibility = 'PUBLIC' | 'FRIENDS' | 'PRIVATE';
export type ProfileAvatarKey = 'AVATAR_BLUE' | 'AVATAR_ORANGE' | 'AVATAR_GREEN' | 'AVATAR_PURPLE';
export type ProfileBackgroundKey = 'BACKGROUND_CS2' | 'BACKGROUND_DST' | 'BACKGROUND_LIBRARY';
export type ProfileThemeKey = 'STEAM_BLUE' | 'TACTICAL_ORANGE' | 'SURVIVAL_GREEN';

export interface ProfileBadgeView {
  badgeId: string;
  badgeName: string;
  description: string;
  iconKey: string;
  xpValue: number;
  rarity: 'COMMON' | 'RARE' | 'EPIC' | string;
  earnedAt: string;
  isFeatured: boolean;
}

export interface PlayerProfileView {
  userId: string;
  nickname: string;
  headline: string | null;
  bio: string | null;
  avatarKey: ProfileAvatarKey;
  backgroundKey: ProfileBackgroundKey;
  themeKey: ProfileThemeKey;
  showcaseGameId: string | null;
  showcaseGameName: string | null;
  profileVisibility: ProfileVisibility;
  friendCount: number;
  totalXp: number;
  badges: ProfileBadgeView[];
  updatedAt: string;
  isOwnProfile: boolean;
}

export interface UpdatePlayerProfileRequest {
  headline: string | null;
  bio: string | null;
  avatarKey: ProfileAvatarKey;
  backgroundKey: ProfileBackgroundKey;
  themeKey: ProfileThemeKey;
  showcaseGameId: string | null;
  profileVisibility: ProfileVisibility;
}

export interface PlayerSearchItem {
  userId: string;
  nickname: string;
  avatarKey: ProfileAvatarKey;
  headline: string | null;
  relationId: string | null;
  relationStatus: string | null;
  isIncomingRequest: boolean;
}

export interface TradeOfferItemView {
  itemId: string;
  templateId: string;
  gameId: string;
  itemName: string;
  rarity: string;
  imageUrl: string | null;
  wearRating: number | null;
  itemRole: 'OFFERED' | 'REQUESTED';
  ownerIdAtCreate: string;
}

export interface TradeableInventoryItemView {
  itemId: string;
  templateId: string;
  gameId: string;
  itemName: string;
  rarity: string;
  imageUrl: string | null;
  wearRating: number | null;
}

export interface TradeOfferView {
  offerId: string;
  senderId: string;
  senderNickname: string;
  recipientId: string;
  recipientNickname: string;
  message: string | null;
  status: 'PENDING' | 'ACCEPTED' | 'DECLINED' | 'CANCELED' | 'EXPIRED' | string;
  createdAt: string;
  respondedAt: string | null;
  version: number;
  offeredItems: TradeOfferItemView[];
  requestedItems: TradeOfferItemView[];
  canAccept: boolean;
  canDecline: boolean;
  canCancel: boolean;
}

export interface CommunityPostView {
  postId: string;
  authorId: string;
  authorNickname: string;
  avatarKey: ProfileAvatarKey;
  gameId: string | null;
  gameName: string | null;
  postType: 'STATUS' | 'ACHIEVEMENT' | 'SCREENSHOT' | 'TRADE' | string;
  content: string;
  mediaUrl: string | null;
  visibility: 'PUBLIC' | 'FRIENDS';
  createdAt: string;
  likeCount: number;
  awardCount: number;
  myReaction: 'LIKE' | 'AWARD' | null;
}

export interface DiscussionReplyView {
  replyId: string;
  authorId: string;
  authorNickname: string;
  avatarKey: ProfileAvatarKey;
  body: string;
  createdAt: string;
}

export interface DiscussionTopicView {
  topicId: string;
  gameId: string;
  gameName: string;
  authorId: string;
  authorNickname: string;
  avatarKey: ProfileAvatarKey;
  title: string;
  body: string;
  status: 'OPEN' | 'LOCKED' | string;
  createdAt: string;
  updatedAt: string;
  replyCount: number;
  replies: DiscussionReplyView[];
}

export interface ApiEnvelope<T> {
  code: number;
  message: string;
  data: T;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export type DataSource = 'api' | 'fallback';

export interface FallbackAwarePagedResult<T> extends PagedResult<T> {
  source: DataSource;
  warning?: string;
}

export type GameStatus = 'ONLINE' | 'OFFLINE' | 'DRAFT' | string;

export type GamePriceFilter = 'all' | 'free' | 'paid' | 'discount' | 'market' | 'packages';

export type GameSortKey = 'default' | 'price' | 'releaseDate' | 'reputation';

export interface GameQuery {
  keyword?: string;
  search?: string;
  tag?: string;
  priceFilter?: GamePriceFilter;
  sort?: GameSortKey;
  status?: string;
  developerId?: string;
  minPrice?: number;
  maxPrice?: number;
  reputation?: string;
  page?: number;
  pageSize?: number;
}

export interface GameListItem {
  gameId: string;
  gameName: string;
  shortName: string;
  developerId: string;
  developerName: string;
  coverTone: 'cs2' | 'dst' | 'background';
  tags: string[];
  basePrice: number;
  discountRate: number;
  finalPrice: number;
  releaseDate: string;
  reputation: string;
  status: GameStatus;
  supportsMarket: boolean;
  hasContentPackages: boolean;
  summary: string;
}

export interface GameDetail extends GameListItem {
  description: string;
}

export interface CreateGameRequest {
  devId: string;
  gameName: string;
  basePrice: number;
  discountRate: number;
  releaseDate: string;
  reputation: string | null;
}

export interface UpdateGameRequest {
  gameName: string;
  basePrice: number;
  discountRate: number;
  releaseDate: string;
  reputation: string | null;
}

export interface GameManageForm {
  gameName: string;
  basePrice: number;
  discountRate: number;
  releaseDate: string;
  reputation: string;
}

export interface GameContentPackage {
  packageId: string;
  gameId: string;
  packageName: string;
  packageType: string;
  basePrice: number;
  discountRate: number;
  finalPrice: number;
  imageUrl: string | null;
  sourceKind: string;
}

export interface GameItemSummaryEntry {
  templateId: string;
  itemName: string;
  rarity: string;
  imageUrl: string | null;
  inventoryItemCount: number;
  activeBuyOrderCount: number;
  activeSellOrderCount: number;
  tradeCount: number;
  highestBuyPrice: number | null;
  lowestSellPrice: number | null;
  lastTradePrice: number | null;
}

export interface GameItemSummary {
  gameId: string;
  templateCount: number;
  inventoryItemCount: number;
  activeBuyOrderCount: number;
  activeSellOrderCount: number;
  tradeCount: number;
  highestBuyPrice: number | null;
  lowestSellPrice: number | null;
  lastTradePrice: number | null;
  items: GameItemSummaryEntry[];
}

export interface GameReviewSummary {
  reviewCount: number;
  recommendCount: number;
  recommendRate: number;
  latestReviewContent: string | null;
  ratingText: string;
}

export interface GameAchievementSummaryItem {
  achievementId: string;
  achievementName: string;
  description: string | null;
  globalRate: number | null;
}

export interface GameAchievementSummary {
  achievementCount: number;
  averageGlobalRate: number | null;
  achievements: GameAchievementSummaryItem[];
}
