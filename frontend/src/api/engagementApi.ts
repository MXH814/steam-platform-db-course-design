import { getApiError, http } from './http';
import type {
  CommunityPostView,
  DiscussionTopicView,
  PlayerProfileView,
  PlayerSearchItem,
  TradeOfferView,
  TradeableInventoryItemView,
  UpdatePlayerProfileRequest
} from './types';

async function read<T>(request: Promise<{ data: T }>): Promise<T> {
  try {
    return (await request).data;
  } catch (error) {
    throw new Error(getApiError(error));
  }
}

export const getProfile = (userId: string) =>
  read<PlayerProfileView>(http.get(`/api/profiles/${encodeURIComponent(userId)}`));

export const getOwnProfile = () => read<PlayerProfileView>(http.get('/api/profile'));

export const updateOwnProfile = (request: UpdatePlayerProfileRequest) =>
  read<PlayerProfileView>(http.put('/api/profile', request));

export const setFeaturedBadge = (badgeId: string) =>
  read<PlayerProfileView>(http.put('/api/profile/featured-badge', { badgeId }));

export const searchPlayers = (query = '', limit = 12) =>
  read<PlayerSearchItem[]>(http.get('/api/players/search', { params: { query, limit } }));

export const listTradeOffers = (status?: string) =>
  read<TradeOfferView[]>(http.get('/api/trade-offers', { params: { status } }));

export const listTradeableInventory = (targetUserId: string) =>
  read<TradeableInventoryItemView[]>(http.get(`/api/players/${encodeURIComponent(targetUserId)}/tradeable-inventory`));

export const createTradeOffer = (request: {
  recipientId: string;
  offeredItemIds: string[];
  requestedItemIds: string[];
  message: string | null;
}) => read<TradeOfferView>(http.post('/api/trade-offers', request));

export const actOnTradeOffer = (offerId: string, action: 'ACCEPT' | 'DECLINE' | 'CANCEL') =>
  read<TradeOfferView>(http.post(`/api/trade-offers/${encodeURIComponent(offerId)}/actions`, { action }));

export const listCommunityPosts = (gameId?: string, limit = 30) =>
  read<CommunityPostView[]>(http.get('/api/community/posts', { params: { gameId, limit } }));

export const createCommunityPost = (request: {
  gameId: string | null;
  postType: 'STATUS' | 'ACHIEVEMENT' | 'SCREENSHOT' | 'TRADE';
  content: string;
  mediaUrl: string | null;
  visibility: 'PUBLIC' | 'FRIENDS';
}) => read<CommunityPostView>(http.post('/api/community/posts', request));

export const setCommunityPostReaction = (postId: string, reactionType: 'LIKE' | 'AWARD' | null) =>
  read<CommunityPostView>(http.put(`/api/community/posts/${encodeURIComponent(postId)}/reaction`, { reactionType }));

export const listDiscussionTopics = (gameId: string, limit = 30) =>
  read<DiscussionTopicView[]>(http.get(`/api/games/${encodeURIComponent(gameId)}/discussions`, { params: { limit } }));

export const getDiscussionTopic = (topicId: string) =>
  read<DiscussionTopicView>(http.get(`/api/community/discussions/${encodeURIComponent(topicId)}`));

export const createDiscussionTopic = (request: { gameId: string; title: string; body: string }) =>
  read<DiscussionTopicView>(http.post('/api/community/discussions', request));

export const replyToDiscussion = (topicId: string, body: string) =>
  read<DiscussionTopicView>(http.post(`/api/community/discussions/${encodeURIComponent(topicId)}/replies`, { body }));
