import { getApiError, http } from './http';

export type InventoryGameId = 'GAME_CS2' | 'GAME_DST';

export interface ItemTemplate {
  templateId: string;
  gameId: string;
  itemName: string;
  rarity: string;
  imageUrl?: string | null;
}

export interface InventoryItem {
  itemId: string;
  templateId: string;
  gameId: string;
  itemName: string;
  rarity: string;
  imageUrl?: string | null;
  wearRating?: number | null;
  status: string;
  acquireTime: string;
}

export interface ItemTransfer {
  transferId: string;
  itemId: string;
  fromUserId?: string | null;
  toUserId: string;
  transferType: string;
  transferTime: string;
}

async function read<T>(request: Promise<{ data: T }>): Promise<T> {
  try {
    const response = await request;
    return response.data;
  } catch (error) {
    throw new Error(getApiError(error));
  }
}

export function listItemTemplates(gameId?: string) {
  return read<ItemTemplate[]>(http.get('/api/item-templates', { params: { gameId } }));
}

export function listInventory(gameId?: string) {
  return read<InventoryItem[]>(http.get('/api/inventory', { params: { gameId } }));
}

export function dropInventoryItem(gameId: string) {
  return read<InventoryItem>(http.post('/api/inventory/drop', { gameId }));
}

export function listItemTransfers(itemId: string) {
  return read<ItemTransfer[]>(http.get(`/api/items/${encodeURIComponent(itemId)}/transfers`));
}
