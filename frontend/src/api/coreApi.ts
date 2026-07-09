import { getApiError, http } from './http';

export interface ApiResponse<T> {
  code: number;
  message: string;
  data: T;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface WalletSummary {
  walletId: string;
  userId: string;
  availableBalance: number;
  frozenBalance: number;
  totalBalance: number;
  version: number;
}

export interface RechargeWalletResult {
  walletId: string;
  transactionId: string;
  availableBalance: number;
  frozenBalance: number;
  totalBalance: number;
}

export interface WalletTransactionEntry {
  txnId: string;
  bizType: string;
  bizRefId: string;
  fundsDirection: string;
  amount: number;
  availBalBefore: number;
  availBalAfter: number;
  idempotencyKey: string | null;
  createTime: string;
}

export interface OrderDetailEntry {
  detailId: string;
  gameId: string;
  gameName: string;
  originalPrice: number;
  discountAmount: number;
  payableAmount: number;
  refundAmount: number;
}

export interface OrderSummary {
  orderId: string;
  userId: string;
  totalAmount: number;
  orderType: string;
  orderStatus: string;
  paymentStatus: string;
  idempotencyKey: string | null;
  createTime: string;
  details: OrderDetailEntry[];
}

export interface LibraryEntry {
  libId: string;
  gameId: string;
  gameName: string;
  acquireWay: string;
  status: string;
  playMinutes: number;
  lastPlayTime: string | null;
}

export interface RefundSummary {
  refundId: string;
  orderId: string;
  refundAmount: number;
  refundType: string;
  reason: string;
  playTimeHours: number;
  status: string;
  applyTime: string;
}

export interface CdkeyBatchSummary {
  batchId: string;
  gameId: string;
  batchNo: string;
  validFrom: string;
  expireTime: string;
  plaintextKeys: string[];
}

export interface CdkeyRedeemResult {
  result: string;
  gameId: string | null;
  libraryId: string | null;
  message: string;
}

export interface GameListItem {
  gameId: string;
  gameName: string;
  developerId: string;
  developerName: string;
  basePrice: number;
  discountRate: number;
  finalPrice: number;
  releaseDate: string;
  reputation: string | null;
  status: string;
}

async function unwrapApi<T>(request: Promise<{ data: ApiResponse<T> }>): Promise<T> {
  try {
    const response = await request;
    if (response.data.code !== 0) {
      throw new Error(response.data.message);
    }

    return response.data.data;
  } catch (error) {
    throw new Error(getApiError(error));
  }
}

async function unwrapRaw<T>(request: Promise<{ data: T }>): Promise<T> {
  try {
    const response = await request;
    return response.data;
  } catch (error) {
    throw new Error(getApiError(error));
  }
}

export function getGames() {
  return unwrapApi<PagedResponse<GameListItem>>(http.get('/api/games', { params: { pageSize: 20 } }));
}

export function getWallet() {
  return unwrapApi<WalletSummary>(http.get('/api/wallet'));
}

export function rechargeWallet(amount: number) {
  return unwrapApi<RechargeWalletResult>(http.post('/api/wallet/recharge', {
    amount,
    idempotencyKey: `wallet-${Date.now()}-${Math.random().toString(16).slice(2)}`
  }));
}

export function getWalletTransactions(page = 1, pageSize = 20) {
  return unwrapApi<PagedResponse<WalletTransactionEntry>>(http.get('/api/wallet/transactions', { params: { page, pageSize } }));
}

export function buyGame(gameId: string) {
  return unwrapRaw<OrderSummary>(http.post('/api/orders', {
    gameId,
    idempotencyKey: `order-${gameId}-${Date.now()}-${Math.random().toString(16).slice(2)}`
  }));
}

export function claimFreeGame(gameId: string) {
  return unwrapRaw<OrderSummary>(http.post(`/api/games/${encodeURIComponent(gameId)}/free-claim`, {}));
}

export function getOrders() {
  return unwrapRaw<OrderSummary[]>(http.get('/api/orders'));
}

export function getOrder(orderId: string) {
  return unwrapRaw<OrderSummary>(http.get(`/api/orders/${encodeURIComponent(orderId)}`));
}

export function getLibrary() {
  return unwrapRaw<LibraryEntry[]>(http.get('/api/library'));
}

export function addPlaytime(gameId: string, minutesToAdd: number) {
  return unwrapRaw<LibraryEntry>(http.post(`/api/library/${encodeURIComponent(gameId)}/playtime`, { minutesToAdd }));
}

export function createRefund(orderId: string, reason: string) {
  return unwrapRaw<RefundSummary>(http.post('/api/refunds', { orderId, reason }));
}

export function getRefunds() {
  return unwrapRaw<RefundSummary[]>(http.get('/api/refunds'));
}

export function getAdminRefunds() {
  return unwrapRaw<RefundSummary[]>(http.get('/api/admin/refunds'));
}

export function approveRefund(refundId: string, reason: string | null) {
  return unwrapRaw<RefundSummary>(http.post(`/api/admin/refunds/${encodeURIComponent(refundId)}/approve`, { reason }));
}

export function rejectRefund(refundId: string, reason: string | null) {
  return unwrapRaw<RefundSummary>(http.post(`/api/admin/refunds/${encodeURIComponent(refundId)}/reject`, { reason }));
}

export function createCdkeyBatch(gameId: string, batchNo: string, validFrom: string, expireTime: string, quantity: number) {
  return unwrapRaw<CdkeyBatchSummary>(http.post('/api/developer/cdkey-batches', {
    gameId,
    batchNo,
    validFrom,
    expireTime,
    quantity
  }));
}

export function redeemCdkey(cdkey: string) {
  return unwrapRaw<CdkeyRedeemResult>(http.post('/api/cdkeys/redeem', { cdkey }));
}
