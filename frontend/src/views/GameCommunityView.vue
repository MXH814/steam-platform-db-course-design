<template>
  <section class="view-stack community-page">
    <div class="section-heading">
      <p class="eyebrow">Community</p>
      <h1>{{ gameId }} 社区与成就</h1>
    </div>

    <div v-if="message" class="state-panel success">{{ message }}</div>
    <div v-if="error" class="state-panel error">{{ error }}</div>

    <section class="community-grid">
      <form class="form-panel" @submit.prevent="submitReview">
        <h2>{{ editingReviewId ? '修改评价' : '发表评价' }}</h2>
        <label class="toggle-row">
          <input v-model="reviewForm.isRecommend" type="checkbox" />
          <span>推荐这款游戏</span>
        </label>
        <label>
          <span>评价内容</span>
          <textarea v-model.trim="reviewForm.content" rows="6" required />
        </label>
        <div class="action-row">
          <button class="primary-button" type="submit" :disabled="submitting || !auth.isAuthenticated">
            {{ submitting ? '提交中...' : editingReviewId ? '保存修改' : '发表评价' }}
          </button>
          <button v-if="editingReviewId" class="ghost-button" type="button" @click="cancelEdit">取消</button>
        </div>
      </form>

      <section class="details-panel achievement-panel">
        <div class="panel-heading">
          <h2>成就</h2>
          <button class="ghost-button fit" type="button" @click="loadAchievements">刷新</button>
        </div>
        <div v-if="loadingAchievements" class="state-panel compact">正在加载成就...</div>
        <div v-else-if="achievements.length === 0" class="state-panel compact">暂无成就数据。</div>
        <article v-for="achievement in achievements" :key="achievement.achId" class="achievement-item">
          <div>
            <h3>{{ achievement.achName }}</h3>
            <p>{{ achievement.description || '暂无描述' }}</p>
          </div>
          <div class="achievement-side">
            <span class="status-pill" :class="{ unlocked: achievement.isUnlocked }">
              {{ achievement.isUnlocked ? '已解锁' : '未解锁' }}
            </span>
            <span class="rate">{{ formatRate(achievement.globalRate) }}</span>
            <button
              class="secondary-button"
              type="button"
              :disabled="achievement.isUnlocked || !auth.isAuthenticated || unlockingAchId === achievement.achId"
              @click="unlock(achievement.achId)"
            >
              {{ unlockingAchId === achievement.achId ? '解锁中...' : '解锁' }}
            </button>
          </div>
        </article>
      </section>
    </section>

    <section class="view-stack">
      <div class="panel-heading">
        <div class="section-heading compact">
          <p class="eyebrow">Reviews</p>
          <h2>玩家评价</h2>
        </div>
        <button class="ghost-button fit" type="button" @click="loadReviews">刷新</button>
      </div>

      <div v-if="loadingReviews" class="state-panel">正在加载评价...</div>
      <div v-else-if="reviews.length === 0" class="state-panel">暂无评价。</div>
      <article v-for="review in reviews" v-else :key="review.reviewId" class="notice-item review-item">
        <div>
          <div class="review-title-row">
            <h3>{{ review.nickname }}</h3>
            <span class="status-pill" :class="{ unlocked: review.isRecommend }">
              {{ review.isRecommend ? '推荐' : '不推荐' }}
            </span>
          </div>
          <p>{{ review.content }}</p>
          <small>版本 {{ review.versionNo }} · {{ formatTime(review.versionCreateTime) }}</small>
        </div>
        <div class="review-actions">
          <button class="ghost-button fit" type="button" @click="startEdit(review)">修改</button>
          <button class="ghost-button fit" type="button" @click="loadVersions(review.reviewId)">历史版本</button>
        </div>
      </article>
    </section>

    <section v-if="selectedReviewId" class="details-panel">
      <div class="panel-heading">
        <h2>历史版本</h2>
        <button class="ghost-button fit" type="button" @click="selectedReviewId = ''">关闭</button>
      </div>
      <div v-if="loadingVersions" class="state-panel compact">正在加载历史版本...</div>
      <article v-for="version in versions" v-else :key="version.versionId" class="version-item">
        <div class="review-title-row">
          <strong>版本 {{ version.versionNo }}</strong>
          <span class="status-pill" :class="{ unlocked: version.isRecommend }">
            {{ version.isRecommend ? '推荐' : '不推荐' }}
          </span>
        </div>
        <p>{{ version.content }}</p>
        <small>{{ formatTime(version.createTime) }}</small>
      </article>
    </section>
  </section>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import {
  createGameReview,
  listGameAchievements,
  listGameReviews,
  listReviewVersions,
  unlockAchievement,
  updateGameReview
} from '../api/communityApi';
import { getApiError } from '../api/http';
import type { AchievementListItem, ReviewListItem, ReviewVersionItem } from '../api/types';
import { useAuthStore } from '../stores/auth';

const route = useRoute();
const auth = useAuthStore();
const gameId = computed(() => String(route.params.gameId || 'GAME_DST'));

const reviews = ref<ReviewListItem[]>([]);
const achievements = ref<AchievementListItem[]>([]);
const versions = ref<ReviewVersionItem[]>([]);
const loadingReviews = ref(false);
const loadingAchievements = ref(false);
const loadingVersions = ref(false);
const submitting = ref(false);
const unlockingAchId = ref('');
const selectedReviewId = ref('');
const editingReviewId = ref('');
const message = ref('');
const error = ref('');

const reviewForm = reactive({
  isRecommend: true,
  content: ''
});

watch(gameId, loadCommunity, { immediate: true });

async function loadCommunity() {
  message.value = '';
  error.value = '';
  await Promise.all([loadReviews(), loadAchievements()]);
}

async function loadReviews() {
  loadingReviews.value = true;
  try {
    reviews.value = await listGameReviews(gameId.value);
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    loadingReviews.value = false;
  }
}

async function loadAchievements() {
  loadingAchievements.value = true;
  try {
    achievements.value = await listGameAchievements(gameId.value);
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    loadingAchievements.value = false;
  }
}

async function submitReview() {
  message.value = '';
  error.value = '';
  submitting.value = true;

  try {
    if (editingReviewId.value) {
      await updateGameReview(editingReviewId.value, { ...reviewForm });
      message.value = '评价已更新。';
    } else {
      const created = await createGameReview(gameId.value, { ...reviewForm });
      editingReviewId.value = created.reviewId;
      message.value = '评价已发表。';
    }

    reviewForm.content = '';
    editingReviewId.value = '';
    await loadReviews();
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    submitting.value = false;
  }
}

function startEdit(review: ReviewListItem) {
  editingReviewId.value = review.reviewId;
  reviewForm.isRecommend = review.isRecommend;
  reviewForm.content = review.content;
}

function cancelEdit() {
  editingReviewId.value = '';
  reviewForm.isRecommend = true;
  reviewForm.content = '';
}

async function loadVersions(reviewId: string) {
  selectedReviewId.value = reviewId;
  loadingVersions.value = true;
  error.value = '';

  try {
    versions.value = await listReviewVersions(reviewId);
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    loadingVersions.value = false;
  }
}

async function unlock(achId: string) {
  unlockingAchId.value = achId;
  message.value = '';
  error.value = '';

  try {
    const result = await unlockAchievement(achId);
    message.value = result.alreadyUnlocked ? '该成就已经解锁。' : '成就已解锁。';
    await loadAchievements();
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    unlockingAchId.value = '';
  }
}

function formatTime(value: string) {
  return new Date(value).toLocaleString();
}

function formatRate(value: number | null) {
  return value === null ? '解锁率 --' : `解锁率 ${value.toFixed(2)}%`;
}
</script>

<style scoped>
.community-grid {
  display: grid;
  grid-template-columns: minmax(280px, 420px) minmax(0, 1fr);
  gap: 18px;
  align-items: start;
}

.panel-heading,
.review-title-row,
.action-row,
.achievement-side,
.review-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.panel-heading {
  justify-content: space-between;
}

.action-row,
.review-actions {
  flex-wrap: wrap;
}

.toggle-row {
  display: flex;
  grid-template-columns: none;
  align-items: center;
}

.toggle-row input {
  width: auto;
}

.achievement-panel {
  display: grid;
  gap: 14px;
}

.achievement-item,
.version-item {
  display: grid;
  gap: 10px;
  border-top: 1px solid rgba(151, 170, 195, 0.16);
  padding-top: 14px;
}

.achievement-item {
  grid-template-columns: minmax(0, 1fr) auto;
}

.achievement-item h3,
.achievement-item p,
.version-item p {
  margin: 0;
}

.achievement-item p,
.version-item p,
.review-item small,
.version-item small {
  color: #aebbd0;
}

.achievement-side {
  justify-content: flex-end;
  flex-wrap: wrap;
}

.status-pill {
  border: 1px solid rgba(151, 170, 195, 0.24);
  border-radius: 999px;
  padding: 0.18rem 0.55rem;
  color: #dce6f5;
  font-size: 0.78rem;
  font-weight: 800;
}

.status-pill.unlocked {
  color: #07120c;
  border-color: transparent;
  background: #7ee2a8;
}

.rate {
  color: #7fc6ff;
  font-size: 0.85rem;
  font-weight: 800;
}

.state-panel.compact {
  box-shadow: none;
}

@media (max-width: 860px) {
  .community-grid,
  .achievement-item {
    grid-template-columns: 1fr;
  }

  .achievement-side {
    justify-content: flex-start;
  }
}
</style>