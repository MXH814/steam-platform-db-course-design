import { http } from './http';
import type {
  DirectMessageItem,
  FriendListItem,
  ReviewInteractionItem,
  UserNotificationItem,
  WorkshopItemView
} from './types';

export async function listFriends(): Promise<FriendListItem[]> {
  const { data } = await http.get<FriendListItem[]>('/api/friends');
  return data;
}

export async function requestFriend(targetUserId: string): Promise<FriendListItem> {
  const { data } = await http.post<FriendListItem>(`/api/friends/${encodeURIComponent(targetUserId)}`);
  return data;
}

export async function acceptFriend(relationId: string): Promise<FriendListItem> {
  const { data } = await http.post<FriendListItem>(`/api/friends/requests/${encodeURIComponent(relationId)}/accept`);
  return data;
}

export async function listMessages(friendUserId: string, limit = 50): Promise<DirectMessageItem[]> {
  const { data } = await http.get<DirectMessageItem[]>(`/api/friends/${encodeURIComponent(friendUserId)}/messages`, { params: { limit } });
  return data;
}

export async function sendMessage(friendUserId: string, content: string): Promise<DirectMessageItem> {
  const { data } = await http.post<DirectMessageItem>(`/api/friends/${encodeURIComponent(friendUserId)}/messages`, { content });
  return data;
}

export async function listReviewInteractions(gameId: string): Promise<ReviewInteractionItem[]> {
  const { data } = await http.get<ReviewInteractionItem[]>(`/api/games/${encodeURIComponent(gameId)}/review-interactions`);
  return data;
}

export async function setReviewInteraction(
  reviewId: string,
  request: { voteType: 'UP' | 'DOWN' | null; isStarred: boolean; isFunny: boolean; isAwarded: boolean }
): Promise<ReviewInteractionItem> {
  const { data } = await http.put<ReviewInteractionItem>(`/api/reviews/${encodeURIComponent(reviewId)}/interaction`, request);
  return data;
}

export async function listWorkshopItems(gameId: string): Promise<WorkshopItemView[]> {
  const { data } = await http.get<WorkshopItemView[]>(`/api/games/${encodeURIComponent(gameId)}/workshop`);
  return data;
}

export async function setWorkshopSubscription(workshopItemId: string, isSubscribed: boolean): Promise<WorkshopItemView> {
  const { data } = await http.put<WorkshopItemView>(`/api/workshop/${encodeURIComponent(workshopItemId)}/subscription`, { isSubscribed });
  return data;
}

export async function listNotifications(limit = 50): Promise<UserNotificationItem[]> {
  const { data } = await http.get<UserNotificationItem[]>('/api/notifications', { params: { limit } });
  return data;
}

export async function markNotificationRead(notificationId: string): Promise<UserNotificationItem> {
  const { data } = await http.put<UserNotificationItem>(`/api/notifications/${encodeURIComponent(notificationId)}/read`);
  return data;
}
