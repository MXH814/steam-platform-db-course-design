<template>
  <section class="steam-community-page">
    <header class="steam-game-header" :class="`tone-${game.heroTone}`">
      <div class="game-capsule" aria-hidden="true">
        <span>{{ game.capsuleLabel }}</span>
      </div>
      <div class="game-header-copy">
        <p class="steam-kicker">社区中心</p>
        <h1>{{ game.title }}</h1>
        <p>{{ game.subtitle }}</p>
        <nav class="steam-tabs" aria-label="游戏页面导航">
          <RouterLink :to="`/games/${gameId}/store`">商店页面</RouterLink>
          <RouterLink :to="`/library/${gameId}`">库内详情</RouterLink>
          <RouterLink :to="`/games/${gameId}/community`">评测与成就</RouterLink>
        </nav>
      </div>
      <aside class="header-achievement-summary">
        <span>成就进度</span>
        <strong>{{ unlockedAchievements.length }}/{{ achievements.length }}</strong>
        <div class="steam-progress"><i :style="{ width: achievementPercent + '%' }" /></div>
      </aside>
    </header>

    <div v-if="message" class="steam-alert success">{{ message }}</div>
    <div v-if="error" class="steam-alert error">{{ error }}</div>

    <section class="community-layout">
      <main class="review-main">
        <section class="review-composer steam-panel">
          <div class="composer-head">
            <div>
              <p class="steam-kicker">撰写评测</p>
              <h2>{{ editingReviewId ? '编辑你的评测' : '你会推荐这款游戏吗？' }}</h2>
            </div>
            <div class="recommend-switch" role="group" aria-label="推荐态度">
              <button
                type="button"
                :class="{ active: reviewForm.isRecommend }"
                @click="reviewForm.isRecommend = true"
              >
                推荐
              </button>
              <button
                type="button"
                :class="{ active: !reviewForm.isRecommend }"
                @click="reviewForm.isRecommend = false"
              >
                不推荐
              </button>
            </div>
          </div>

          <textarea
            v-model.trim="reviewForm.content"
            class="steam-review-box"
            rows="7"
            maxlength="1200"
            placeholder="告诉其他玩家这款游戏哪里值得推荐，或哪里需要谨慎。"
            required
          />

          <div class="composer-actions">
            <span>{{ reviewForm.content.length }}/1200</span>
            <div>
              <button v-if="editingReviewId" class="steam-button secondary" type="button" @click="cancelEdit">取消编辑</button>
              <button class="steam-button primary" type="button" :disabled="submitting || !auth.isAuthenticated || !reviewForm.content" @click="submitReview">
                {{ submitting ? '正在提交...' : editingReviewId ? '保存评测' : '发表评测' }}
              </button>
            </div>
          </div>
          <p v-if="!auth.isAuthenticated" class="composer-hint">登录后才能发表评测；后端会继续校验你是否拥有该游戏。</p>
        </section>

        <section class="review-toolbar steam-panel">
          <div>
            <p class="steam-kicker">玩家评测</p>
            <h2>{{ recommendedCount }} 篇推荐，{{ notRecommendedCount }} 篇不推荐</h2>
          </div>
          <div class="review-controls">
            <label>
              <span>评价类型</span>
              <select v-model="reviewFilter">
                <option value="all">全部</option>
                <option value="recommended">推荐</option>
                <option value="notRecommended">不推荐</option>
              </select>
            </label>
            <label>
              <span>显示顺序</span>
              <select v-model="reviewSort">
                <option value="helpful">最有价值</option>
                <option value="recent">最近发布</option>
              </select>
            </label>
            <button class="steam-button secondary" type="button" @click="loadReviews">刷新</button>
          </div>
        </section>

        <div v-if="loadingReviews" class="steam-panel steam-state">正在加载玩家评测...</div>
        <div v-else-if="orderedReviews.length === 0" class="steam-panel steam-state">当前筛选条件下暂无评测。</div>

        <section v-else class="review-columns">
          <div class="review-column-large">
            <h3>最有价值的评测</h3>
            <article v-for="review in primaryReviews" :key="review.reviewId" class="steam-review-card">
              <aside class="review-author">
                <div class="avatar-box">{{ review.nickname.slice(0, 1).toUpperCase() }}</div>
                <strong>{{ review.nickname }}</strong>
                <span>{{ review.userId }}</span>
                <small>{{ review.versionNo }} 个版本</small>
              </aside>
              <div class="review-body">
                <header>
                  <div class="vote-block" :class="{ negative: !review.isRecommend }">
                    <span>{{ review.isRecommend ? '推荐' : '不推荐' }}</span>
                    <small>{{ estimatedPlaytime(review) }} 小时游戏时间记录</small>
                  </div>
                  <button class="star-button" type="button" @click="showFrontendOnlyNotice('收藏评测需要后端收藏接口。')">☆</button>
                </header>
                <small>发布于：{{ formatDate(review.versionCreateTime) }}</small>
                <p>{{ review.content }}</p>
                <button v-if="review.content.length > 160" class="read-more" type="button" @click="showFrontendOnlyNotice('全文弹窗可前端继续增强，当前先直接展示完整内容。')">阅读全文</button>
                <footer class="review-footer">
                  <span>这篇评测是否有价值？</span>
                  <button type="button" @click="showFrontendOnlyNotice('“有价值”投票需要后端记录。')">是</button>
                  <button type="button" @click="showFrontendOnlyNotice('“无价值”投票需要后端记录。')">否</button>
                  <button type="button" @click="showFrontendOnlyNotice('欢乐/奖励需要后端互动接口。')">欢乐</button>
                  <button type="button" @click="showFrontendOnlyNotice('奖励评测需要后端奖励接口。')">奖励</button>
                  <button type="button" @click="loadVersions(review.reviewId)">版本历史</button>
                  <button v-if="canEdit(review)" type="button" @click="startEdit(review)">编辑</button>
                </footer>
              </div>
            </article>
          </div>

          <aside class="recent-column">
            <h3>最近发布</h3>
            <article v-for="review in recentReviews" :key="`recent-${review.reviewId}`" class="recent-review-card">
              <header>
                <span :class="['mini-vote', { negative: !review.isRecommend }]">{{ review.isRecommend ? '荐' : '否' }}</span>
                <strong>{{ review.nickname }}</strong>
                <small>{{ estimatedPlaytime(review) }} 小时</small>
                <button type="button" @click="showFrontendOnlyNotice('收藏评测需要后端收藏接口。')">☆</button>
              </header>
              <small>发布于：{{ formatDate(review.versionCreateTime) }}</small>
              <p>{{ review.content }}</p>
              <footer>
                <button type="button" @click="showFrontendOnlyNotice('“有价值”投票需要后端记录。')">是</button>
                <button type="button" @click="showFrontendOnlyNotice('“无价值”投票需要后端记录。')">否</button>
                <button type="button" @click="loadVersions(review.reviewId)">历史</button>
              </footer>
            </article>
          </aside>
        </section>
      </main>

      <aside class="community-sidebar">
        <section class="steam-panel achievement-sidebar">
          <div class="sidebar-head">
            <div>
              <p class="steam-kicker">Steam 成就</p>
              <h2>{{ unlockedAchievements.length }}/{{ achievements.length }} 已解锁</h2>
            </div>
            <button class="steam-button secondary" type="button" @click="loadAchievements">刷新</button>
          </div>
          <div class="steam-progress large"><i :style="{ width: achievementPercent + '%' }" /></div>
          <div v-if="loadingAchievements" class="steam-state inline">正在加载成就...</div>
          <div v-else-if="achievements.length === 0" class="steam-state inline">暂无成就数据。</div>
          <div v-else class="achievement-list">
            <article v-for="achievement in achievements" :key="achievement.achId" class="achievement-row">
              <img :class="['achievement-icon', { locked: !achievement.isUnlocked }]" :src="achievement.iconUrl" :alt="achievement.achName" />
              <div>
                <strong>{{ achievement.achName }}</strong>
                <span>{{ achievement.description || '暂无描述' }}</span>
                <small>{{ formatRate(achievement.globalRate) }}</small>
              </div>
              <button
                class="unlock-button"
                type="button"
                :disabled="achievement.source !== 'api' || achievement.isUnlocked || !auth.isAuthenticated || unlockingAchId === achievement.achId"
                @click="unlock(achievement.achId)"
              >
                {{ achievement.source !== 'api' ? '展示' : achievement.isUnlocked ? '已解锁' : unlockingAchId === achievement.achId ? '...' : '解锁' }}
              </button>
            </article>
          </div>
        </section>

        <section v-if="selectedReviewId" class="steam-panel history-panel">
          <div class="sidebar-head">
            <h2>评价版本历史</h2>
            <button class="steam-button secondary" type="button" @click="selectedReviewId = ''">关闭</button>
          </div>
          <div v-if="loadingVersions" class="steam-state inline">正在加载历史版本...</div>
          <article v-for="version in versions" v-else :key="version.versionId" class="version-row">
            <header>
              <strong>版本 {{ version.versionNo }}</strong>
              <span :class="['mini-status', { positive: version.isRecommend }]">{{ version.isRecommend ? '推荐' : '不推荐' }}</span>
            </header>
            <p>{{ version.content }}</p>
            <small>{{ formatTime(version.createTime) }}</small>
          </article>
        </section>
      </aside>
    </section>
  </section>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import { RouterLink, useRoute } from 'vue-router';
import {
  createGameReview,
  listGameAchievements,
  listGameReviews,
  listReviewVersions,
  unlockAchievement,
  updateGameReview
} from '../api/communityApi';
import { getApiError } from '../api/http';
import type { ReviewListItem, ReviewVersionItem } from '../api/types';
import { mergeAchievementCatalog, type AchievementDisplayItem } from '../data/achievementCatalog';
import { getGameMeta } from '../data/gameCatalog';
import { useAuthStore } from '../stores/auth';

const route = useRoute();
const auth = useAuthStore();
const gameId = computed(() => String(route.params.gameId || 'GAME_DST'));
const game = computed(() => getGameMeta(gameId.value));

const reviews = ref<ReviewListItem[]>([]);
const achievements = ref<AchievementDisplayItem[]>([]);
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
const reviewFilter = ref<'all' | 'recommended' | 'notRecommended'>('all');
const reviewSort = ref<'helpful' | 'recent'>('helpful');

const reviewForm = reactive({
  isRecommend: true,
  content: ''
});

const unlockedAchievements = computed(() => achievements.value.filter((achievement) => achievement.isUnlocked));
const achievementPercent = computed(() => {
  if (achievements.value.length === 0) {
    return 0;
  }

  return Math.round((unlockedAchievements.value.length / achievements.value.length) * 100);
});
const recommendedCount = computed(() => reviews.value.filter((review) => review.isRecommend).length);
const notRecommendedCount = computed(() => reviews.value.length - recommendedCount.value);

const filteredReviews = computed(() => reviews.value.filter((review) => {
  if (reviewFilter.value === 'recommended') {
    return review.isRecommend;
  }

  if (reviewFilter.value === 'notRecommended') {
    return !review.isRecommend;
  }

  return true;
}));

const orderedReviews = computed(() => [...filteredReviews.value].sort((left, right) => {
  if (reviewSort.value === 'recent') {
    return new Date(right.versionCreateTime).getTime() - new Date(left.versionCreateTime).getTime();
  }

  return estimateHelpfulness(right) - estimateHelpfulness(left);
}));

const primaryReviews = computed(() => orderedReviews.value.slice(0, 6));
const recentReviews = computed(() => [...filteredReviews.value]
  .sort((left, right) => new Date(right.versionCreateTime).getTime() - new Date(left.versionCreateTime).getTime())
  .slice(0, 8));

watch(gameId, loadCommunity, { immediate: true });

async function loadCommunity() {
  message.value = '';
  error.value = '';
  await Promise.all([loadReviews(), loadAchievements()]);
}

async function loadReviews() {
  loadingReviews.value = true;
  try {
    reviews.value = await listGameReviews(gameId.value, 80);
  } catch (requestError) {
    error.value = friendlyError(requestError);
  } finally {
    loadingReviews.value = false;
  }
}

async function loadAchievements() {
  loadingAchievements.value = true;
  try {
    const achievementRows = await listGameAchievements(gameId.value);
    achievements.value = mergeAchievementCatalog(gameId.value, achievementRows);
  } catch (requestError) {
    achievements.value = mergeAchievementCatalog(gameId.value, []);
    error.value = friendlyError(requestError);
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
      message.value = '评价已更新，新的历史版本已经生成。';
    } else {
      await createGameReview(gameId.value, { ...reviewForm });
      message.value = '评价已发表。';
    }

    reviewForm.content = '';
    editingReviewId.value = '';
    await loadReviews();
  } catch (requestError) {
    error.value = friendlyError(requestError);
  } finally {
    submitting.value = false;
  }
}

function startEdit(review: ReviewListItem) {
  editingReviewId.value = review.reviewId;
  reviewForm.isRecommend = review.isRecommend;
  reviewForm.content = review.content;
  window.scrollTo({ top: 0, behavior: 'smooth' });
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
    error.value = friendlyError(requestError);
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
    error.value = friendlyError(requestError);
  } finally {
    unlockingAchId.value = '';
  }
}

function canEdit(review: ReviewListItem) {
  return auth.currentUser?.principalId === review.userId;
}

function showFrontendOnlyNotice(text: string) {
  message.value = text;
}

function friendlyError(requestError: unknown) {
  const apiError = getApiError(requestError);
  if (apiError.includes('GAME_NOT_OWNED')) {
    return '你还没有正常拥有这款游戏，不能发表评价或解锁该游戏成就。';
  }

  if (apiError.includes('REVIEW_ALREADY_EXISTS')) {
    return '你已经评价过这款游戏，可以在自己的评价上继续编辑。';
  }

  return apiError;
}

function estimateHelpfulness(review: ReviewListItem) {
  return review.thumbsUp * 10 + review.content.length + (review.isRecommend ? 12 : 0);
}

function estimatedPlaytime(review: ReviewListItem) {
  return Math.max(3, Math.round((review.content.length * 0.7 + review.versionNo * 8) * 10) / 10).toFixed(1);
}

function formatTime(value: string) {
  return new Date(value).toLocaleString();
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString();
}

function formatRate(value: number | null) {
  return value === null ? '全球解锁率 --' : `全球解锁率 ${value.toFixed(2)}%`;
}
</script>

<style scoped>
.steam-community-page,
.review-main,
.community-sidebar,
.review-column-large,
.recent-column,
.achievement-list {
  display: grid;
  gap: 14px;
}

.steam-game-header {
  display: grid;
  grid-template-columns: 220px minmax(0, 1fr) 260px;
  gap: 18px;
  align-items: stretch;
  min-height: 190px;
  padding: 18px;
  border: 1px solid rgba(102, 192, 244, 0.18);
  border-radius: 2px;
  background:
    linear-gradient(90deg, rgba(13, 24, 35, 0.94), rgba(33, 54, 72, 0.84)),
    radial-gradient(circle at 80% 0%, rgba(102, 192, 244, 0.28), transparent 34rem);
  box-shadow: 0 24px 80px rgba(0, 0, 0, 0.28);
}

.steam-game-header.tone-tactical {
  background:
    linear-gradient(90deg, rgba(16, 22, 31, 0.94), rgba(63, 71, 83, 0.82)),
    radial-gradient(circle at 80% 0%, rgba(235, 175, 72, 0.26), transparent 32rem);
}

.game-capsule {
  display: grid;
  place-items: center;
  min-height: 154px;
  overflow: hidden;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 2px;
  background:
    linear-gradient(135deg, rgba(43, 92, 122, 0.96), rgba(15, 20, 29, 0.8)),
    repeating-linear-gradient(45deg, rgba(255, 255, 255, 0.06) 0 12px, transparent 12px 24px);
}

.game-capsule span {
  color: #ffffff;
  font-size: 3.6rem;
  font-weight: 900;
  letter-spacing: 0;
  text-shadow: 0 6px 24px rgba(0, 0, 0, 0.5);
}

.game-header-copy {
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  min-width: 0;
}

.game-header-copy h1,
.game-header-copy p,
.header-achievement-summary span,
.header-achievement-summary strong,
.review-toolbar h2,
.review-composer h2,
.sidebar-head h2,
.review-column-large h3,
.recent-column h3 {
  margin: 0;
}

.game-header-copy h1 {
  font-size: clamp(2rem, 4vw, 3.2rem);
  line-height: 1.05;
}

.game-header-copy > p:not(.steam-kicker) {
  max-width: 760px;
  color: #c7d5e0;
}

.steam-kicker {
  margin: 0 0 5px;
  color: #66c0f4;
  font-size: 0.75rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.steam-tabs {
  display: flex;
  flex-wrap: wrap;
  gap: 2px;
  margin-top: 16px;
}

.steam-tabs a {
  min-height: 38px;
  padding: 9px 16px;
  color: #dbe7f3;
  background: rgba(43, 78, 104, 0.88);
}

.steam-tabs a.router-link-active,
.steam-tabs a:hover {
  color: #ffffff;
  background: linear-gradient(90deg, #2a6fa0, #66c0f4);
}

.header-achievement-summary,
.steam-panel {
  border: 1px solid rgba(102, 192, 244, 0.12);
  border-radius: 2px;
  background: rgba(11, 18, 26, 0.8);
}

.header-achievement-summary {
  display: grid;
  align-content: center;
  gap: 10px;
  padding: 18px;
}

.header-achievement-summary span {
  color: #8f98a0;
}

.header-achievement-summary strong {
  color: #ffffff;
  font-size: 2rem;
}

.community-layout {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 360px;
  gap: 18px;
  align-items: start;
}

.steam-panel {
  padding: 16px;
  box-shadow: 0 14px 40px rgba(0, 0, 0, 0.22);
}

.review-composer {
  background: linear-gradient(180deg, rgba(31, 50, 65, 0.92), rgba(18, 30, 42, 0.92));
}

.composer-head,
.review-toolbar,
.sidebar-head,
.composer-actions,
.review-body header,
.review-footer,
.recent-review-card header,
.recent-review-card footer,
.version-row header {
  display: flex;
  align-items: center;
  gap: 10px;
}

.composer-head,
.review-toolbar,
.sidebar-head,
.composer-actions,
.review-body header {
  justify-content: space-between;
}

.recommend-switch {
  display: grid;
  grid-template-columns: repeat(2, minmax(92px, 1fr));
  gap: 2px;
  padding: 2px;
  background: rgba(0, 0, 0, 0.35);
}

.recommend-switch button,
.review-footer button,
.recent-review-card footer button,
.read-more,
.star-button,
.unlock-button,
.steam-button {
  border: 0;
  border-radius: 2px;
  cursor: pointer;
  font-weight: 800;
}

.recommend-switch button {
  min-height: 34px;
  color: #b8c6d3;
  background: transparent;
}

.recommend-switch button.active {
  color: #ffffff;
  background: #1a9fff;
}

.steam-review-box,
.review-controls select {
  border: 1px solid rgba(103, 193, 245, 0.25);
  border-radius: 2px;
  color: #dfe3e6;
  background: rgba(8, 13, 18, 0.86);
}

.steam-review-box {
  min-height: 150px;
  margin-top: 14px;
  resize: vertical;
}

.composer-actions {
  margin-top: 12px;
  color: #8f98a0;
}

.composer-actions > div,
.review-controls {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: flex-end;
}

.composer-hint {
  margin: 10px 0 0;
  color: #c7d5e0;
}

.steam-button {
  min-height: 34px;
  padding: 0 13px;
}

.steam-button.primary {
  color: #062033;
  background: linear-gradient(90deg, #75b022, #a4d007);
}

.steam-button.secondary {
  color: #c7d5e0;
  background: rgba(103, 193, 245, 0.16);
}

.steam-button:hover:not(:disabled),
.review-footer button:hover,
.recent-review-card footer button:hover,
.unlock-button:hover:not(:disabled) {
  filter: brightness(1.12);
}

button:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

.review-toolbar {
  flex-wrap: wrap;
}

.review-toolbar h2 {
  color: #c7d5e0;
  font-size: 1rem;
}

.review-controls label {
  display: grid;
  gap: 4px;
  color: #8f98a0;
  font-size: 0.76rem;
  font-weight: 800;
}

.review-columns {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 330px;
  gap: 18px;
  align-items: start;
}

.steam-review-card {
  display: grid;
  grid-template-columns: 230px minmax(0, 1fr);
  background: rgba(20, 35, 50, 0.82);
}

.review-author,
.review-body,
.recent-review-card,
.achievement-row,
.version-row {
  padding: 13px;
}

.review-author {
  display: grid;
  align-content: start;
  gap: 7px;
  color: #8f98a0;
  background: rgba(9, 14, 20, 0.56);
}

.avatar-box,
.achievement-icon {
  display: grid;
  place-items: center;
  border-radius: 2px;
  color: #ffffff;
  background: linear-gradient(135deg, #2f89bc, #1b2838);
}

.avatar-box {
  width: 58px;
  height: 58px;
  font-size: 1.6rem;
  font-weight: 900;
}

.review-author strong {
  color: #c7d5e0;
}

.review-body {
  min-width: 0;
}

.vote-block {
  display: grid;
  gap: 2px;
  min-width: min(100%, 330px);
  padding: 9px 12px;
  color: #c7d5e0;
  background: rgba(41, 100, 137, 0.65);
}

.vote-block.negative {
  background: rgba(122, 48, 56, 0.66);
}

.vote-block span {
  color: #ffffff;
  font-size: 1.15rem;
  font-weight: 900;
}

.star-button {
  width: 34px;
  height: 34px;
  color: #6f8ba3;
  background: transparent;
  font-size: 1.5rem;
}

.review-body > small,
.recent-review-card > small,
.version-row small {
  display: block;
  margin-top: 12px;
  color: #6f8ba3;
}

.review-body p,
.recent-review-card p,
.version-row p,
.achievement-row span {
  color: #c7d5e0;
}

.review-body p {
  margin: 12px 0;
  white-space: pre-wrap;
}

.read-more {
  float: right;
  min-height: 32px;
  padding: 0 12px;
  color: #dfe3e6;
  background: rgba(255, 255, 255, 0.12);
}

.review-footer {
  clear: both;
  flex-wrap: wrap;
  margin-top: 18px;
  padding-top: 13px;
  border-top: 1px solid rgba(103, 193, 245, 0.16);
  color: #6f8ba3;
}

.review-footer button,
.recent-review-card footer button {
  min-height: 30px;
  padding: 0 10px;
  color: #66c0f4;
  background: rgba(103, 193, 245, 0.12);
}

.recent-review-card {
  background: linear-gradient(90deg, rgba(34, 61, 83, 0.92), rgba(22, 39, 55, 0.9));
}

.recent-review-card header {
  justify-content: flex-start;
}

.recent-review-card header small {
  margin-left: auto;
  color: #8f98a0;
}

.recent-review-card header button {
  color: #6f8ba3;
  background: transparent;
}

.mini-vote,
.mini-status {
  display: inline-grid;
  place-items: center;
  min-width: 32px;
  height: 28px;
  color: #ffffff;
  background: #1a9fff;
}

.mini-vote.negative,
.mini-status:not(.positive) {
  background: #8e3b46;
}

.recent-review-card p {
  margin: 8px 0 12px;
  overflow-wrap: anywhere;
}

.recent-review-card footer {
  border-top: 1px solid rgba(103, 193, 245, 0.14);
  padding-top: 10px;
}

.sidebar-head {
  align-items: flex-start;
}

.steam-progress {
  height: 8px;
  overflow: hidden;
  border-radius: 999px;
  background: rgba(0, 0, 0, 0.64);
}

.steam-progress.large {
  height: 12px;
  margin: 12px 0 8px;
}

.steam-progress i {
  display: block;
  height: 100%;
  background: linear-gradient(90deg, #66c0f4, #a4d007);
}

.achievement-row {
  display: grid;
  grid-template-columns: 54px minmax(0, 1fr) auto;
  gap: 11px;
  align-items: center;
  background: rgba(255, 255, 255, 0.04);
}

.achievement-icon {
  width: 54px;
  height: 54px;
  object-fit: cover;
  font-size: 1.4rem;
  font-weight: 900;
}

.achievement-icon.locked {
  filter: grayscale(1);
  opacity: 0.48;
}

.achievement-row div:nth-child(2) {
  display: grid;
  gap: 3px;
  min-width: 0;
}

.achievement-row strong {
  overflow: hidden;
  color: #dfe3e6;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.achievement-row span,
.achievement-row small {
  color: #8f98a0;
  font-size: 0.82rem;
}

.unlock-button {
  min-height: 30px;
  padding: 0 10px;
  color: #c7d5e0;
  background: rgba(103, 193, 245, 0.15);
}

.version-row {
  background: rgba(255, 255, 255, 0.04);
}

.version-row header {
  justify-content: space-between;
}

.version-row p {
  margin: 8px 0;
}

.steam-alert,
.steam-state {
  padding: 13px 16px;
  border-radius: 2px;
  background: rgba(11, 18, 26, 0.86);
}

.steam-alert.success {
  color: #b6f38f;
  border-left: 4px solid #a4d007;
}

.steam-alert.error {
  color: #ffc4c4;
  border-left: 4px solid #d85c5c;
}

.steam-state.inline {
  box-shadow: none;
}

@media (max-width: 1180px) {
  .steam-game-header,
  .community-layout,
  .review-columns {
    grid-template-columns: 1fr;
  }

  .header-achievement-summary {
    align-content: stretch;
  }
}

@media (max-width: 740px) {
  .steam-game-header,
  .steam-review-card,
  .achievement-row {
    grid-template-columns: 1fr;
  }

  .composer-head,
  .review-toolbar,
  .composer-actions,
  .review-body header {
    align-items: flex-start;
    flex-direction: column;
  }

  .review-controls,
  .review-controls label,
  .review-controls select,
  .composer-actions > div,
  .steam-button {
    width: 100%;
  }
}
</style>
