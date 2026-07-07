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
