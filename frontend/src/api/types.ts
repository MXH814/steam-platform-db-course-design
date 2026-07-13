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
