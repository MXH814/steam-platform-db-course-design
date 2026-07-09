import { getApiError, http } from './http';

export type ApiResponse<T> = {
  code: number;
  message: string;
  data: T;
};

export type MarketListing = {
  templateId: string;
  gameId: string;
  itemName: string;
  rarity: string;
  imageUrl?: string | null;
  highestBuyPrice?: number | null;
  lowestSellPrice?: number | null;
  activeBuyCount: number;
  activeSellCount: number;
};

export type MarketOrder = {
  marketOrderId: string;
  userId: string;
  templateId: string;
  itemName: string;
  orderType: 'BUY' | 'SELL';
  itemId?: string | null;
  targetPrice: number;
  frozenAmount: number;
  status: string;
  createTime: string;
};

export type MarketTrade = {
  tradeId: string;
  buyOrderId: string;
  sellOrderId: string;
  templateId: string;
  itemName: string;
  itemId: string;
  buyerId: string;
  sellerId: string;
  tradePrice: number;
  platformFee: number;
  currency: string;
  tradeTime: string;
};

export type ItemTransfer = {
  transferId: string;
  itemId: string;
  itemName: string;
  fromUserId?: string | null;
  toUserId: string;
  transferType: string;
  transferTime: string;
};

export type CreateMarketOrderPayload = {
  orderType: 'BUY' | 'SELL';
  templateId: string;
  itemId?: string | null;
  targetPrice: number;
};

async function unwrap<T>(request: Promise<{ data: ApiResponse<T> }>): Promise<T> {
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

export function getMarket(gameId = 'GAME_CS2') {
  return unwrap<MarketListing[]>(http.get('/api/market', { params: { gameId } }));
}

export function getMyOrders() {
  return unwrap<MarketOrder[]>(http.get('/api/market/orders'));
}

export function createOrder(payload: CreateMarketOrderPayload) {
  return unwrap<MarketOrder>(http.post('/api/market/orders', payload));
}

export function cancelOrder(marketOrderId: string) {
  return unwrap<object>(http.post(`/api/market/orders/${marketOrderId}/cancel`, {}));
}

export function matchMarket(templateId?: string) {
  return unwrap<MarketTrade>(http.post('/api/market/match', { templateId: templateId || null }));
}

export function getTrades() {
  return unwrap<MarketTrade[]>(http.get('/api/market/trades'));
}

export function getTransfers(itemId: string) {
  return unwrap<ItemTransfer[]>(http.get(`/api/market/items/${itemId}/transfers`));
}
