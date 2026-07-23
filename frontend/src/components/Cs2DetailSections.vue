<template>
  <SteamGameDetailTemplate
    :game="game"
    :review="review"
    variant="cs2"
    summary="Counter-Strike 2 延续经典的团队竞技玩法，并带来全新的视觉效果、动态烟雾和社区饰品经济。"
  >
    <template #media>
      <div class="media-preview"><img :src="meta.heroImage" alt="Counter-Strike 2 游戏画面" /></div>
      <div class="thumb-strip" aria-label="媒体缩略图">
        <img v-for="item in 5" :key="item" :class="{ active: item === 1 }" :src="item % 2 ? meta.headerImage : meta.heroImage" alt="" />
      </div>
    </template>

    <template #summary-art>
      <img class="cs2-capsule" :src="meta.headerImage" alt="Counter-Strike 2" />
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
          <p v-else-if="ownershipError">暂时无法确认游戏库状态，你仍可继续浏览商品详情。</p>
          <p v-else>免费开玩。添加到游戏库后即可安装并进入比赛。</p>
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

        <GameSummarySection title="项目成就概览" :loading="achievementsState.loading" :error="achievementsState.error" :empty="!achievements">
          <dl v-if="achievements" class="stat-grid">
            <div><dt>成就数</dt><dd>{{ achievements.achievementCount }}</dd></div>
            <div><dt>平均达成率</dt><dd>{{ formatRate(achievements.averageGlobalRate) }}</dd></div>
          </dl>
          <div v-if="achievements?.achievements.length" class="mini-list">
                        <article v-for="item in achievements.achievements.slice(0, 3)" :key="item.achievementId">
              <img :src="summaryAchievementIcon(item)" :alt="item.achievementName" />
              <div>
                <strong>{{ item.achievementName }}</strong>
                <span>{{ formatRate(item.globalRate) }}</span>
              </div>
            </article>
          </div>
          <RouterLink class="section-link" :to="communityRoute">查看全部成就</RouterLink>
        </GameSummarySection>

        <section class="about-panel">
          <h2>关于此游戏</h2>
          <p>在经典的目标攻防与团队对抗中磨练技巧。游戏库提供游玩记录，库存与社区市场则用于查看和交易已拥有的饰品。</p>
        </section>
      </section>
    </template>

    <template #side>
      <SteamInfoPanel
        title="功能"
        :rows="['多人在线竞技', '社区市场', 'Steam 创意工坊', '成就']"
      />
      <SteamInfoPanel
        title="语言与内容"
        :rows="['支持简体中文界面', '在线互动', '游戏内购买']"
      />
    </template>
  </SteamGameDetailTemplate>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { GameAchievementSummary, GameAchievementSummaryItem, GameDetail, GameItemSummary, GameReviewSummary } from '../api/types';
import { getAchievementIcon } from '../data/achievementCatalog';
import { getGameMeta } from '../data/gameCatalog';
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
const meta = computed(() => getGameMeta(props.game.gameId));
const workshopRoute = computed(() => ({ name: 'game-community', params: { gameId: props.game.gameId }, query: { section: 'workshop' } }));

function money(value: number | null) {
  return typeof value === 'number' ? `¥ ${value.toFixed(2)}` : '暂无';
}

function formatRate(value: number | null) {
  return typeof value === 'number' ? `${value}%` : '暂无';
}

function summaryAchievementIcon(item: GameAchievementSummaryItem) {
  return getAchievementIcon({ achId: item.achievementId });
}
</script>

<style scoped>
.media-preview {
  height: 420px;
  overflow: hidden;
  background: #080d13;
}

.media-preview img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.cs2-capsule {
  display: block;
  width: 100%;
  aspect-ratio: 460 / 215;
  object-fit: cover;
}

.thumb-strip img {
  width: 100%;
  height: 58px;
  object-fit: cover;
  opacity: 0.62;
}

.thumb-strip img.active {
  outline: 2px solid #ffffff;
  opacity: 1;
}

.thumb-strip {
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 0.35rem;
  padding-top: 0.45rem;
}

.thumb-strip .active {
  border-color: var(--steam-blue);
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
  display: grid;
  grid-template-columns: 44px minmax(0, 1fr);
  gap: 0.75rem;
  align-items: center;
  border-bottom: 1px solid rgba(151, 170, 195, 0.12);
  padding-bottom: 0.45rem;
}

.mini-list img {
  width: 44px;
  height: 44px;
  object-fit: cover;
  border-radius: 2px;
  background: #05080c;
}

.mini-list article div {
  display: grid;
  gap: 0.18rem;
  min-width: 0;
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
