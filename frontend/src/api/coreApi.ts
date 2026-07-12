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

export type PaymentMethod = 'STEAM_WALLET' | 'WECHAT_PAY' | 'ALIPAY' | 'VISA' | 'MASTERCARD';

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
  paymentMethod?: PaymentMethod | null;
  createTime: string;
}

export interface WalletHistoryEntry {
  historyId: string;
  sourceType: string;
  createTime: string;
  itemName: string;
  paymentMethod: PaymentMethod | string;
  orderStatus?: string | null;
  paymentStatus?: string | null;
  refundStatus?: string | null;
  originalPrice: number;
  discountAmount: number;
  discountRate: number;
  totalAmount: number;
  walletChange: number | null;
  walletBalanceAfter: number | null;
  orderId: string | null;
  orderDetailId: string | null;
  refundId: string | null;
  walletTransactionId: string | null;
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
  paymentMethod?: PaymentMethod | null;
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

export function rechargeWallet(amount: number, paymentMethod: Exclude<PaymentMethod, 'STEAM_WALLET'>) {
  return unwrapApi<RechargeWalletResult>(http.post('/api/wallet/recharge', {
    amount,
    paymentMethod,
    idempotencyKey: `wallet-${Date.now()}-${Math.random().toString(16).slice(2)}`
  }));
}

export function getWalletTransactions(page = 1, pageSize = 20) {
  return unwrapApi<PagedResponse<WalletTransactionEntry>>(http.get('/api/wallet/transactions', { params: { page, pageSize } }));
}

export async function getWalletHistory(page = 1, pageSize = 50) {
  return buildWalletHistoryFromReadmeApis(page, pageSize);
}

export async function getWalletHistoryEntry(historyId: string) {
  const page = await buildWalletHistoryFromReadmeApis(1, 100);
  const entry = page.items.find(item => item.historyId === historyId);
  if (entry) {
    return entry;
  }

  return unwrapApi<WalletHistoryEntry>(http.get(`/api/wallet/history/${encodeURIComponent(historyId)}`));
}

export function buyGame(gameId: string, paymentMethod: PaymentMethod = 'STEAM_WALLET') {
  return unwrapRaw<OrderSummary>(http.post('/api/orders', {
    gameId,
    paymentMethod,
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

export function createRefund(orderId: string, reason: string, orderDetailId?: string | null, gameId?: string | null) {
  return unwrapRaw<RefundSummary>(http.post('/api/refunds', { orderId, reason, orderDetailId, gameId }));
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

async function buildWalletHistoryFromReadmeApis(page: number, pageSize: number): Promise<PagedResponse<WalletHistoryEntry>> {
  const normalizedPage = Math.max(1, page);
  const normalizedPageSize = Math.min(Math.max(1, pageSize), 100);
  const [transactionPage, orders, refunds, library] = await Promise.all([
    getAllWalletTransactions(),
    getOrders(),
    getRefunds(),
    getLibrary()
  ]);

  const buyWalletTransactions = new Map(
    transactionPage.items
      .filter(transaction => transaction.bizType === 'BUY_GAME')
      .map(transaction => [transaction.bizRefId, transaction])
  );
  const refundWalletTransactions = new Map(
    transactionPage.items
      .filter(transaction => transaction.bizType === 'REFUND')
      .map(transaction => [transaction.bizRefId, transaction])
  );
  const orderRows = orders.flatMap(order => order.details.map(detail => orderDetailToHistory(
    order,
    detail,
    buyWalletTransactions.get(order.orderId) ?? null
  )));
  const orderGameIds = new Set(orders.flatMap(order => order.details.map(detail => detail.gameId)));
  const refundRows = refunds.map(refund => refundToHistory(
    refund,
    orders.find(order => order.orderId === refund.orderId),
    refundWalletTransactions.get(refund.refundId) ?? null
  ));
  const walletRows = transactionPage.items
    .filter(transaction => transaction.bizType === 'RECHARGE')
    .map(walletTransactionToHistory);
  const libraryRows = library
    .filter(entry => !orderGameIds.has(entry.gameId))
    .map(libraryEntryToHistory);

  const rows = [...walletRows, ...orderRows, ...refundRows, ...libraryRows]
    .sort((left, right) => {
      const timeDiff = new Date(right.createTime).getTime() - new Date(left.createTime).getTime();
      return timeDiff || right.historyId.localeCompare(left.historyId);
    });
  const start = (normalizedPage - 1) * normalizedPageSize;

  return {
    items: rows.slice(start, start + normalizedPageSize),
    page: normalizedPage,
    pageSize: normalizedPageSize,
    total: rows.length
  };
}

async function getAllWalletTransactions(): Promise<PagedResponse<WalletTransactionEntry>> {
  const pageSize = 100;
  const firstPage = await getWalletTransactions(1, pageSize);
  if (firstPage.total <= firstPage.items.length) {
    return firstPage;
  }

  const pageCount = Math.ceil(firstPage.total / pageSize);
  const restPages = await Promise.all(
    Array.from({ length: pageCount - 1 }, (_, index) => getWalletTransactions(index + 2, pageSize))
  );
  const items = [firstPage, ...restPages].flatMap(result => result.items);
  return { ...firstPage, page: 1, pageSize, items };
}

function walletTransactionToHistory(transaction: WalletTransactionEntry): WalletHistoryEntry {
  const sourceType = transaction.bizType;
  const isCredit = transaction.fundsDirection === 'CREDIT' || transaction.fundsDirection === 'UNFREEZE';
  const walletChange = isCredit ? transaction.amount : -transaction.amount;

  return {
    historyId: `WALLET-${transaction.txnId}`,
    sourceType,
    createTime: transaction.createTime,
    itemName: sourceType === 'RECHARGE' ? 'Steam 钱包充值' : sourceType,
    paymentMethod: transaction.paymentMethod || (sourceType === 'RECHARGE' ? 'SIMULATED_EXTERNAL' : 'STEAM_WALLET'),
    orderStatus: null,
    paymentStatus: null,
    refundStatus: null,
    originalPrice: transaction.amount,
    discountAmount: 0,
    discountRate: 0,
    totalAmount: transaction.amount,
    walletChange,
    walletBalanceAfter: transaction.availBalAfter,
    orderId: sourceType === 'BUY_GAME' ? transaction.bizRefId : null,
    orderDetailId: null,
    refundId: sourceType === 'REFUND' ? transaction.bizRefId : null,
    walletTransactionId: transaction.txnId
  };
}

function orderDetailToHistory(order: OrderSummary, detail: OrderDetailEntry, walletTransaction: WalletTransactionEntry | null): WalletHistoryEntry {
  const isFree = detail.payableAmount === 0;
  const paymentMethod = isFree ? 'FREE_CLAIM' : (order.paymentMethod || walletTransaction?.paymentMethod || 'STEAM_WALLET');

  return {
    historyId: `ORDER-${detail.detailId}`,
    sourceType: isFree ? 'FREE_CLAIM' : 'BUY_GAME',
    createTime: order.createTime,
    itemName: detail.gameName,
    paymentMethod,
    orderStatus: order.orderStatus,
    paymentStatus: order.paymentStatus,
    refundStatus: null,
    originalPrice: detail.originalPrice,
    discountAmount: detail.discountAmount,
    discountRate: detail.originalPrice > 0 ? detail.discountAmount / detail.originalPrice : 0,
    totalAmount: detail.payableAmount,
    walletChange: isFree ? null : walletChangeFromTransaction(walletTransaction),
    walletBalanceAfter: walletTransaction?.availBalAfter ?? null,
    orderId: order.orderId,
    orderDetailId: detail.detailId,
    refundId: null,
    walletTransactionId: walletTransaction?.txnId ?? null
  };
}

function refundToHistory(refund: RefundSummary, order: OrderSummary | undefined, walletTransaction: WalletTransactionEntry | null): WalletHistoryEntry {
  const firstDetail = order?.details[0];
  const paymentMethod = walletTransaction?.paymentMethod || order?.paymentMethod || 'STEAM_WALLET';

  return {
    historyId: `REFUND-${refund.refundId}`,
    sourceType: 'REFUND',
    createTime: refund.applyTime,
    itemName: firstDetail?.gameName || `订单 ${refund.orderId} 退款`,
    paymentMethod,
    orderStatus: order?.orderStatus ?? null,
    paymentStatus: order?.paymentStatus ?? null,
    refundStatus: refund.status,
    originalPrice: refund.refundAmount,
    discountAmount: 0,
    discountRate: 0,
    totalAmount: refund.refundAmount,
    walletChange: refund.status === 'APPROVED' && paymentMethod === 'STEAM_WALLET'
      ? walletChangeFromTransaction(walletTransaction) ?? refund.refundAmount
      : null,
    walletBalanceAfter: walletTransaction?.availBalAfter ?? null,
    orderId: refund.orderId,
    orderDetailId: firstDetail?.detailId || null,
    refundId: refund.refundId,
    walletTransactionId: walletTransaction?.txnId ?? null
  };
}

function libraryEntryToHistory(entry: LibraryEntry): WalletHistoryEntry {
  const sourceType = librarySourceType(entry.acquireWay);

  return {
    historyId: `LIBRARY-${entry.libId}`,
    sourceType,
    createTime: entry.lastPlayTime || new Date(0).toISOString(),
    itemName: entry.gameName,
    paymentMethod: sourceType,
    orderStatus: null,
    paymentStatus: null,
    refundStatus: null,
    originalPrice: 0,
    discountAmount: 0,
    discountRate: 0,
    totalAmount: 0,
    walletChange: null,
    walletBalanceAfter: null,
    orderId: null,
    orderDetailId: null,
    refundId: null,
    walletTransactionId: null
  };
}

function librarySourceType(acquireWay: string) {
  const normalized = acquireWay.toUpperCase();
  if (normalized === 'FREE') return 'FREE_CLAIM';
  if (normalized === 'REDEEM') return 'CDKEY_REDEEM';
  if (normalized === 'GIFT') return 'GIFT';
  return 'LIBRARY_IMPORT';
}

function walletChangeFromTransaction(transaction: WalletTransactionEntry | null) {
  if (!transaction) {
    return null;
  }

  return transaction.fundsDirection === 'CREDIT' || transaction.fundsDirection === 'UNFREEZE'
    ? transaction.amount
    : -transaction.amount;
}
