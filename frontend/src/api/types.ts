export type LoginRole = 'PLAYER' | 'ADMIN';

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
