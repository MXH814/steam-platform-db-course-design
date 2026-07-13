<template>
  <section class="steam-store-page">
    <nav class="store-blue-nav" aria-label="商店导航">
      <RouterLink to="/">您的商店</RouterLink>
      <RouterLink :to="`/games/${gameId}`">游戏详情</RouterLink>
      <RouterLink :to="{ name: 'game-community', params: { gameId } }">社区评测</RouterLink>
      <RouterLink :to="`/library/${gameId}`">库内查看</RouterLink>
    </nav>

    <header class="store-title-row">
      <div>
        <p class="steam-kicker">商店页面</p>
        <h1>{{ game.title }}</h1>
      </div>
      <RouterLink class="subtle-link" :to="{ name: 'game-community', params: { gameId } }">查看社区中心</RouterLink>
    </header>

    <section class="store-hero">
      <div class="media-gallery">
        <div class="hero-shot" :class="`tone-${game.heroTone}`">
          <span>{{ game.capsuleLabel }}</span>
          <strong>{{ game.storeLine }}</strong>
        </div>
        <div class="thumb-strip">
          <button v-for="tag in game.tags" :key="tag" type="button">{{ tag }}</button>
        </div>
      </div>

      <aside class="store-summary">
        <div class="capsule-small">{{ game.shortName }}</div>
        <p>{{ game.storeLine }}</p>
        <dl>
          <div>
            <dt>最近评测</dt>
            <dd>{{ reviewSummary }}</dd>
          </div>
          <div>
            <dt>全部评测</dt>
            <dd>{{ reviews.length }} 篇课程演示评价</dd>
          </div>
          <div>
            <dt>发行日期</dt>
            <dd>课程设计演示数据</dd>
          </div>
          <div>
            <dt>标签</dt>
            <dd>{{ game.tags.join('、') }}</dd>
          </div>
        </dl>
      </aside>
    </section>

    <section class="store-content-grid">
      <main class="store-main-column">
        <section class="purchase-band">
          <div>
            <h2>购买 {{ game.shortName }}</h2>
            <p>购买或入库后，评价与成就模块会通过 PLAYER_LIBRARY 做资产确权。</p>
          </div>
          <button class="steam-green-button" type="button" @click="notice = '购买入口由 Group C 接口负责，当前页面保留商店展示位置。'">加入购物车</button>
        </section>

        <section class="store-panel">
          <h2>关于这款游戏</h2>
          <p>{{ game.subtitle }}</p>
          <p>本页面用于模拟 Steam 商店详情页：右栏展示成就和链接，主区域展示购买入口、评测摘要和社区入口。</p>
        </section>

        <section class="store-panel review-snapshot">
          <div class="panel-title-row">
            <h2>玩家评测</h2>
            <RouterLink :to="{ name: 'game-community', params: { gameId } }">查看全部评测</RouterLink>
          </div>
          <div v-if="loadingReviews" class="store-state">正在加载评测...</div>
          <div v-else-if="reviews.length === 0" class="store-state">暂无评测。</div>
          <article v-for="review in topReviews" v-else :key="review.reviewId" class="store-review-row">
            <strong>{{ review.isRecommend ? '推荐' : '不推荐' }}</strong>
            <div>
              <span>{{ review.nickname }}</span>
              <p>{{ review.content }}</p>
            </div>
          </article>
        </section>
      </main>

      <aside class="store-side-column">
        <section class="store-panel achievement-store-box">
          <div class="panel-title-row">
            <h2>项目成就（{{ achievements.length }} 项）</h2>
            <RouterLink :to="{ name: 'game-community', params: { gameId } }">查看全部</RouterLink>
          </div>
          <div class="achievement-tile-row">
                        <div v-for="achievement in achievementPreview" :key="achievement.achId" :class="['achievement-tile', { locked: !achievement.isUnlocked }]">
              <img :src="achievement.iconUrl" :alt="achievement.achName" />
            </div>
            <RouterLink v-if="achievements.length > achievementPreview.length" class="all-achievements" :to="{ name: 'game-community', params: { gameId } }">
              查看所有 {{ achievements.length }} 项
            </RouterLink>
          </div>
          <div class="store-progress"><i :style="{ width: achievementPercent + '%' }" /></div>
          <small>您已解锁 {{ unlockedCount }}/{{ achievements.length }} 项</small>
        </section>

        <section class="store-panel side-links">
          <h2>链接与信息</h2>
          <button v-for="link in game.links" :key="link" type="button" @click="notice = `${link} 需要对应模块接口或外部链接。`">{{ link }}</button>
        </section>
      </aside>
    </section>

    <div v-if="notice" class="floating-notice">{{ notice }}</div>
  </section>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { RouterLink, useRoute } from 'vue-router';
import { listGameAchievements, listGameReviews } from '../api/communityApi';
import { getApiError } from '../api/http';
import type { ReviewListItem } from '../api/types';
import { withAchievementIcons, type AchievementDisplayItem } from '../data/achievementCatalog';
import { getGameMeta } from '../data/gameCatalog';

const route = useRoute();
const gameId = computed(() => String(route.params.gameId || 'GAME_DST'));
const game = computed(() => getGameMeta(gameId.value));
const achievements = ref<AchievementDisplayItem[]>([]);
const reviews = ref<ReviewListItem[]>([]);
const loadingReviews = ref(false);
const notice = ref('');

const unlockedCount = computed(() => achievements.value.filter((achievement) => achievement.isUnlocked).length);
const achievementPercent = computed(() => achievements.value.length === 0 ? 0 : Math.round((unlockedCount.value / achievements.value.length) * 100));
const achievementPreview = computed(() => achievements.value.slice(0, 4));
const topReviews = computed(() => reviews.value.slice(0, 3));
const reviewSummary = computed(() => {
  if (reviews.value.length === 0) {
    return '暂无用户评测';
  }

  const recommendRate = Math.round((reviews.value.filter((review) => review.isRecommend).length / reviews.value.length) * 100);
  return `${recommendRate}% 推荐`;
});

watch(gameId, loadStore, { immediate: true });

async function loadStore() {
  notice.value = '';
  loadingReviews.value = true;
  try {
    const [achievementRows, reviewRows] = await Promise.all([
      listGameAchievements(gameId.value),
      listGameReviews(gameId.value, 12)
    ]);
    achievements.value = withAchievementIcons(achievementRows);
    reviews.value = reviewRows;
  } catch (error) {
    notice.value = getApiError(error);
  } finally {
    loadingReviews.value = false;
  }
}
</script>

<style scoped>
.steam-store-page {
  display: grid;
  gap: 16px;
}

.store-blue-nav,
.store-title-row,
.panel-title-row,
.store-content-grid,
.store-hero,
.purchase-band,
.store-review-row,
.achievement-tile-row {
  display: flex;
  align-items: center;
  gap: 12px;
}

.store-blue-nav {
  flex-wrap: wrap;
  min-height: 42px;
  padding: 0 10px;
  background: linear-gradient(90deg, #3b6e8f, #23537a 48%, #17415f);
}

.store-blue-nav a {
  padding: 9px 12px;
  color: #dbeaf7;
  font-weight: 800;
}

.store-blue-nav a.router-link-active,
.store-blue-nav a:hover {
  color: #ffffff;
  background: rgba(255, 255, 255, 0.12);
}

.store-title-row,
.panel-title-row,
.purchase-band {
  justify-content: space-between;
}

.store-title-row h1,
.store-title-row p,
.store-panel h2,
.purchase-band h2,
.purchase-band p,
.store-summary p,
.store-review-row p {
  margin: 0;
}

.store-title-row h1 {
  font-size: clamp(2rem, 4vw, 3.1rem);
}

.steam-kicker {
  margin: 0 0 4px;
  color: #66c0f4;
  font-size: 0.75rem;
  font-weight: 900;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.subtle-link,
.panel-title-row a {
  color: #66c0f4;
  font-weight: 800;
}

.store-hero {
  align-items: stretch;
  display: grid;
  grid-template-columns: minmax(0, 1fr) 340px;
}

.media-gallery,
.store-main-column,
.store-side-column {
  display: grid;
  gap: 12px;
}

.hero-shot {
  display: grid;
  align-content: end;
  min-height: 360px;
  padding: 26px;
  border: 1px solid rgba(102, 192, 244, 0.16);
  background:
    linear-gradient(145deg, rgba(31, 63, 85, 0.88), rgba(8, 13, 19, 0.96)),
    repeating-linear-gradient(45deg, rgba(255, 255, 255, 0.06) 0 14px, transparent 14px 28px);
}

.hero-shot.tone-tactical {
  background:
    linear-gradient(145deg, rgba(79, 70, 51, 0.88), rgba(8, 13, 19, 0.96)),
    repeating-linear-gradient(45deg, rgba(255, 255, 255, 0.06) 0 14px, transparent 14px 28px);
}

.hero-shot span {
  color: #ffffff;
  font-size: 5rem;
  font-weight: 900;
  line-height: 1;
}

.hero-shot strong {
  max-width: 680px;
  color: #dfe3e6;
  font-size: 1.25rem;
}

.thumb-strip {
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 8px;
}

.thumb-strip button {
  min-height: 58px;
  border: 0;
  color: #c7d5e0;
  background: rgba(42, 71, 94, 0.9);
  font-weight: 800;
}

.store-summary,
.store-panel,
.purchase-band {
  border: 1px solid rgba(102, 192, 244, 0.12);
  border-radius: 2px;
  background: rgba(11, 18, 26, 0.88);
}

.store-summary {
  display: grid;
  gap: 14px;
  align-content: start;
  padding: 14px;
}

.capsule-small {
  display: grid;
  place-items: center;
  min-height: 120px;
  color: #ffffff;
  background: linear-gradient(135deg, #1b4f73, #0b1017);
  font-size: 2.4rem;
  font-weight: 900;
}

.store-summary p,
.store-panel p,
.purchase-band p,
.store-panel small {
  color: #c7d5e0;
}

.store-summary dl {
  display: grid;
  gap: 8px;
  margin: 0;
}

.store-summary dl div {
  display: grid;
  grid-template-columns: 96px minmax(0, 1fr);
  gap: 10px;
}

.store-summary dt {
  color: #556772;
  font-weight: 900;
  text-transform: uppercase;
}

.store-summary dd {
  margin: 0;
  color: #66c0f4;
}

.store-content-grid {
  align-items: start;
  display: grid;
  grid-template-columns: minmax(0, 1fr) 360px;
}

.purchase-band,
.store-panel {
  padding: 16px;
}

.steam-green-button {
  min-height: 42px;
  border: 0;
  border-radius: 2px;
  padding: 0 18px;
  color: #08120a;
  background: linear-gradient(90deg, #75b022, #a4d007);
  cursor: pointer;
  font-weight: 900;
}

.store-review-row {
  align-items: start;
  margin-top: 10px;
  padding: 12px;
  background: rgba(255, 255, 255, 0.04);
}

.store-review-row strong {
  min-width: 80px;
  color: #66c0f4;
}

.store-review-row span {
  color: #8f98a0;
  font-weight: 800;
}

.achievement-tile-row {
  flex-wrap: wrap;
  align-items: stretch;
}

.achievement-tile,
.all-achievements {
  display: grid;
  place-items: center;
  width: 64px;
  height: 64px;
  color: #ffffff;
  background: linear-gradient(135deg, #366f91, #0f1a24);
  font-weight: 900;
}

.achievement-tile.locked {
  filter: grayscale(1);
  opacity: 0.5;
}

.all-achievements {
  width: 86px;
  padding: 0 10px;
  color: #66c0f4;
  text-align: center;
  background: #19364a;
}

.store-progress {
  height: 8px;
  margin-top: 12px;
  overflow: hidden;
  background: #05080c;
}

.store-progress i {
  display: block;
  height: 100%;
  background: linear-gradient(90deg, #66c0f4, #a4d007);
}

.side-links {
  display: grid;
  gap: 7px;
}

.side-links button {
  min-height: 36px;
  border: 0;
  color: #66c0f4;
  background: rgba(42, 71, 94, 0.72);
  text-align: left;
  cursor: pointer;
  font-weight: 800;
}

.store-state,
.floating-notice {
  padding: 12px;
  color: #c7d5e0;
  background: rgba(0, 0, 0, 0.22);
}

.floating-notice {
  border-left: 4px solid #66c0f4;
}

@media (max-width: 980px) {
  .store-hero,
  .store-content-grid {
    grid-template-columns: 1fr;
  }

  .thumb-strip {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}
</style>
