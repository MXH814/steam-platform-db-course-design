<template>
  <SteamGameDetailTemplate
    :game="game"
    :review="review"
    variant="cs2"
    summary="Counter-Strike 2 是本项目的免费入库和饰品经济样板。商店详情页只负责商品介绍、入库状态、市场概览和社区入口；个人库存与交易明细放在游戏库和 Group D 页面。"
  >
    <template #banner>
      <div class="cs2-banner">
        <span>COUNTER-STRIKE 2</span>
        <strong>免费开玩 · 饰品经济 · 市场入口</strong>
      </div>
    </template>

    <template #media>
      <div class="media-preview">
        <div>
          <span>CS2</span>
          <strong>TACTICAL MARKET DEMO</strong>
        </div>
      </div>
      <div class="thumb-strip" aria-label="媒体缩略图">
        <span v-for="item in 5" :key="item" :class="{ active: item === 1 }">预览 {{ item }}</span>
      </div>
    </template>

    <template #summary-art>
      <div class="cs2-capsule">Counter-Strike 2</div>
    </template>

    <template #summary-extra>
      <div class="summary-tags">
        <span>免费开玩</span>
        <span>多人竞技</span>
        <span>饰品市场</span>
        <span>创意工坊</span>
      </div>
    </template>

    <template #primary-action>
      <section class="action-panel">
        <div>
          <h2>{{ owned ? '已在你的游戏库中' : '玩 Counter-Strike 2' }}</h2>
          <p v-if="ownershipLoading">正在确认你的游戏库状态...</p>
          <p v-else-if="owned">你已经拥有 CS2。商店页保留介绍、口碑和市场概览，库存与个人成就在库内详情查看。</p>
          <p v-else-if="ownershipError">暂未确认是否已入库。你仍可浏览商店详情，免费入库由 Group C 接口负责。</p>
          <p v-else>CS2 是免费游戏。这里展示免费入库入口，真实入库流程由 Group C 的免费入库接口承接。</p>
        </div>
        <div class="action-buttons">
          <RouterLink v-if="owned" class="green-button" :to="{ name: 'game-library', params: { gameId: game.gameId } }">开始游戏</RouterLink>
          <button v-else type="button" class="green-button" :disabled="actionLoading || ownershipLoading" @click="emit('primary-action')">
            {{ actionLoading ? '正在入库...' : '免费入库' }}
          </button>
          <RouterLink class="blue-button" :to="{ name: 'market', query: { gameId: game.gameId } }">饰品市场</RouterLink>
          <RouterLink class="ghost-button" :to="communityRoute">评价与成就</RouterLink>
          <RouterLink class="ghost-button" :to="workshopRoute">创意工坊</RouterLink>
        </div>
      </section>
    </template>

    <template #main>
      <section class="module-grid">
        <GameSummarySection title="饰品市场概览" :loading="itemsState.loading" :error="itemsState.error" :empty="!items">
          <dl v-if="items" class="stat-grid">
            <div><dt>饰品模板</dt><dd>{{ items.templateCount }}</dd></div>
            <div><dt>市场买单</dt><dd>{{ items.activeBuyOrderCount }}</dd></div>
            <div><dt>市场卖单</dt><dd>{{ items.activeSellOrderCount }}</dd></div>
            <div><dt>累计成交</dt><dd>{{ items.tradeCount }}</dd></div>
            <div><dt>最高买价</dt><dd>{{ money(items.highestBuyPrice) }}</dd></div>
            <div><dt>最近成交</dt><dd>{{ money(items.lastTradePrice) }}</dd></div>
          </dl>
          <RouterLink class="section-link" :to="{ name: 'market', query: { gameId: game.gameId } }">进入饰品市场</RouterLink>
        </GameSummarySection>

        <GameSummarySection title="评价概览" :loading="reviewState.loading" :error="reviewState.error" :empty="!review">
          <dl v-if="review" class="stat-grid">
            <div><dt>口碑</dt><dd class="blue">{{ review.ratingText }}</dd></div>
            <div><dt>评价数</dt><dd>{{ review.reviewCount }}</dd></div>
            <div><dt>推荐率</dt><dd>{{ review.recommendRate }}%</dd></div>
            <div><dt>推荐数</dt><dd>{{ review.recommendCount }}</dd></div>
          </dl>
          <p v-if="review?.latestReviewContent" class="module-note">{{ review.latestReviewContent }}</p>
          <RouterLink class="section-link" :to="communityRoute">进入评价区</RouterLink>
        </GameSummarySection>

        <GameSummarySection title="成就概览" :loading="achievementsState.loading" :error="achievementsState.error" :empty="!achievements">
          <dl v-if="achievements" class="stat-grid">
            <div><dt>成就数</dt><dd>{{ achievements.achievementCount }}</dd></div>
            <div><dt>平均达成率</dt><dd>{{ achievements.averageGlobalRate ?? 0 }}%</dd></div>
          </dl>
          <div v-if="achievements?.achievements.length" class="mini-list">
            <article v-for="item in achievements.achievements.slice(0, 3)" :key="item.achievementId">
              <strong>{{ item.achievementName }}</strong>
              <span>{{ item.globalRate ?? 0 }}%</span>
            </article>
          </div>
          <RouterLink class="section-link" :to="communityRoute">查看全部成就</RouterLink>
        </GameSummarySection>

        <section class="about-panel">
          <h2>页面职责</h2>
          <p>商店详情页不展示“我的库存实例”和市场撮合明细。用户已入库后，从游戏库进入 CS2 库内详情查看个人游玩时长、库存入口和个人成就进度。</p>
        </section>
      </section>
    </template>

    <template #side>
      <SteamInfoPanel
        title="商店页保留入口"
        :rows="['免费入库状态', '饰品市场入口', '评价与成就入口', '创意工坊入口']"
      />
      <SteamInfoPanel
        title="已入库后查看"
        :rows="['/library/GAME_CS2', '个人游玩时长', '库存入口', '个人成就进度']"
      />
      <SteamInfoPanel
        title="本页摘要接口"
        :rows="['GET /api/games/{gameId}', 'GET /api/games/{gameId}/items/summary', 'GET /api/games/{gameId}/reviews/summary', 'GET /api/games/{gameId}/achievements/summary']"
      />
    </template>
  </SteamGameDetailTemplate>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { GameAchievementSummary, GameDetail, GameItemSummary, GameReviewSummary } from '../api/types';
import GameSummarySection from './GameSummarySection.vue';
import SteamGameDetailTemplate from './SteamGameDetailTemplate.vue';
import SteamInfoPanel from './SteamInfoPanel.vue';

type ModuleState<T> = {
  loading: boolean;
  error: string;
  data: T;
};

const props = defineProps<{
  game: GameDetail;
  items: GameItemSummary | null;
  itemsState: ModuleState<GameItemSummary | null>;
  review: GameReviewSummary | null;
  reviewState: ModuleState<GameReviewSummary | null>;
  achievements: GameAchievementSummary | null;
  achievementsState: ModuleState<GameAchievementSummary | null>;
  owned: boolean;
  ownershipLoading?: boolean;
  ownershipError?: string;
  actionLoading?: boolean;
}>();

const emit = defineEmits<{
  (event: 'primary-action'): void;
}>();

const communityRoute = computed(() => ({ name: 'game-community', params: { gameId: props.game.gameId } }));
const workshopRoute = computed(() => ({ name: 'game-community', params: { gameId: props.game.gameId }, query: { section: 'workshop' } }));

function money(value: number | null) {
  return typeof value === 'number' ? `¥ ${value.toFixed(2)}` : '暂无';
}
</script>

<style scoped>
.cs2-banner {
  display: grid;
  min-height: 138px;
  align-content: center;
  border: 12px solid rgba(7, 12, 18, 0.8);
  padding: 1rem 2rem;
  background:
    radial-gradient(circle at 74% 26%, rgba(26, 159, 255, 0.24), transparent 12rem),
    linear-gradient(100deg, #111923, #175a7a 56%, #161d27);
}

.cs2-banner span {
  color: var(--steam-blue);
  font-weight: 950;
}

.cs2-banner strong {
  color: #ffffff;
  font-size: clamp(1.8rem, 4vw, 3.1rem);
  line-height: 1.08;
  text-wrap: balance;
}

.media-preview {
  display: grid;
  min-height: 420px;
  place-items: center;
  overflow: hidden;
  background:
    radial-gradient(circle at 64% 40%, rgba(238, 137, 32, 0.26), transparent 10rem),
    linear-gradient(135deg, #d8d7d0, #4d6374 62%, #080d13);
}

.media-preview div {
  display: grid;
  justify-items: center;
  gap: 0.25rem;
  color: #10151d;
}

.media-preview span {
  font-size: clamp(4rem, 14vw, 8rem);
  font-weight: 950;
  line-height: 0.95;
}

.media-preview strong {
  font-weight: 950;
}

.thumb-strip {
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 0.35rem;
  padding-top: 0.45rem;
}

.thumb-strip span {
  display: grid;
  min-height: 42px;
  place-items: center;
  border: 2px solid transparent;
  border-radius: 4px;
  color: #c7d5e0;
  background: #213142;
  font-size: 0.8rem;
}

.thumb-strip .active {
  border-color: var(--steam-blue);
}

.cs2-capsule {
  display: grid;
  min-height: 178px;
  place-items: center;
  padding: 1rem;
  color: #ffffff;
  background: linear-gradient(135deg, #f08a18, #111923 48%, #1d5d84);
  font-size: clamp(1.6rem, 4vw, 2.6rem);
  font-weight: 950;
  text-align: center;
}

.summary-tags,
.action-buttons {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.summary-tags {
  padding: 0 1rem 1rem;
}

.summary-tags span {
  border-radius: 3px;
  padding: 0.18rem 0.4rem;
  color: #d7e4f0;
  background: rgba(151, 170, 195, 0.14);
  font-size: 0.78rem;
}

.action-panel {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 1rem;
  align-items: center;
  border-radius: 4px;
  padding: 1rem;
  background: linear-gradient(90deg, rgba(17, 25, 35, 0.96), rgba(27, 40, 56, 0.96));
}

.action-panel h2,
.action-panel p,
.about-panel h2,
.about-panel p,
.module-note {
  margin: 0;
}

.action-panel h2,
.about-panel h2 {
  font-size: 1.14rem;
}

.action-panel p,
.about-panel p,
.module-note {
  color: var(--steam-muted);
  line-height: 1.65;
  text-wrap: pretty;
}

.action-buttons {
  justify-content: flex-end;
}

.green-button,
.blue-button,
.ghost-button,
.section-link {
  border-radius: 4px;
  padding: 0.62rem 1rem;
  font-weight: 900;
  text-decoration: none;
  white-space: nowrap;
}

.green-button {
  border: 0;
  color: #102009;
  background: var(--steam-green);
  cursor: pointer;
}

.blue-button,
.ghost-button,
.section-link {
  border: 1px solid var(--steam-border);
  color: var(--steam-blue);
  background: rgba(42, 71, 94, 0.86);
}

.module-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1rem;
}

.stat-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.65rem;
}

.stat-grid div {
  border-radius: 4px;
  padding: 0.72rem;
  background: rgba(9, 14, 22, 0.42);
}

.stat-grid dt {
  color: var(--steam-muted);
  font-size: 0.78rem;
}

.stat-grid dd {
  margin: 0.2rem 0 0;
  color: var(--steam-text);
  font-size: 1.25rem;
  font-weight: 950;
  font-variant-numeric: tabular-nums;
}

.blue {
  color: var(--steam-blue) !important;
}

.mini-list {
  display: grid;
  gap: 0.55rem;
  margin-top: 0.8rem;
}

.mini-list article {
  display: flex;
  justify-content: space-between;
  gap: 0.75rem;
  border-bottom: 1px solid rgba(151, 170, 195, 0.12);
  padding-bottom: 0.45rem;
}

.mini-list span {
  color: var(--steam-blue);
  font-variant-numeric: tabular-nums;
}

.about-panel {
  border: 1px solid var(--steam-border);
  border-radius: 6px;
  padding: 1rem;
  background: rgba(22, 32, 45, 0.84);
}

@media (max-width: 940px) {
  .action-panel,
  .module-grid {
    grid-template-columns: 1fr;
  }

  .action-buttons {
    justify-content: flex-start;
  }
}

@media (max-width: 620px) {
  .cs2-banner,
  .media-preview {
    min-height: 260px;
  }

  .thumb-strip,
  .stat-grid {
    grid-template-columns: 1fr;
  }
}
</style>
