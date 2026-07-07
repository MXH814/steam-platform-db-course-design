import { http } from './http';
import type {
  AchievementListItem,
  CreateReviewRequest,
  ReviewListItem,
  ReviewVersionItem,
  UnlockAchievementResult,
  UpdateReviewRequest
} from './types';

export async function listGameReviews(gameId: string, limit = 50): Promise<ReviewListItem[]> {
  const { data } = await http.get<ReviewListItem[]>(`/api/games/${encodeURIComponent(gameId)}/reviews`, {
    params: { limit }
  });
  return data;
}

export async function createGameReview(gameId: string, request: CreateReviewRequest): Promise<ReviewListItem> {
  const { data } = await http.post<ReviewListItem>(`/api/games/${encodeURIComponent(gameId)}/reviews`, request);
  return data;
}

export async function updateGameReview(reviewId: string, request: UpdateReviewRequest): Promise<ReviewListItem> {
  const { data } = await http.put<ReviewListItem>(`/api/reviews/${encodeURIComponent(reviewId)}`, request);
  return data;
}

export async function listReviewVersions(reviewId: string): Promise<ReviewVersionItem[]> {
  const { data } = await http.get<ReviewVersionItem[]>(`/api/reviews/${encodeURIComponent(reviewId)}/versions`);
  return data;
}

export async function listGameAchievements(gameId: string): Promise<AchievementListItem[]> {
  const { data } = await http.get<AchievementListItem[]>(`/api/games/${encodeURIComponent(gameId)}/achievements`);
  return data;
}

export async function unlockAchievement(achId: string): Promise<UnlockAchievementResult> {
  const { data } = await http.post<UnlockAchievementResult>(`/api/achievements/${encodeURIComponent(achId)}/unlock`, {});
  return data;
}