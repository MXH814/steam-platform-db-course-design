<template>
  <SteamGameDetailTemplate
    :game="game"
    :review="review"
    :variant="game.shortName === 'DST' ? 'dst' : 'default'"
    :summary="detailSummary"
  >
    <template #media>
      <div class="game-media"><img :src="meta.heroImage" :alt="`${game.gameName} 游戏画面`" /></div>
    </template>

    <template #summary-art>
      <img class="game-capsule" :src="meta.headerImage" :alt="game.gameName" />
    </template>

    <template #summary-extra>
      <div class="summary-tags">
        <span v-for="tag in game.tags.slice(0, 6)" :key="tag">{{ tag }}</span>
      </div>
    </template>

    <template #primary-action>
      <section class="purchase-panel">
        <div>
          <h2>{{ owned ? '已在你的游戏库中' : game.finalPrice > 0 ? `购买 ${game.shortName}` : `获取 ${game.shortName}` }}</h2>
          <p v-if="ownershipLoading">正在确认你的游戏库状态...</p>
          <p v-else-if="owned">你已经拥有这款游戏。商店页保留商品介绍、DLC/礼包、公告和社区入口，个人游玩数据请进入游戏库查看。</p>
          <p v-else-if="ownershipError">暂时无法确认游戏库状态，你仍可继续浏览商品详情。</p>
          <p v-else>{{ purchaseCopy }}</p>
        </div>
        <GamePriceBlock :base-price="game.basePrice" :final-price="game.finalPrice" :discount-rate="game.discountRate" />
        <RouterLink v-if="owned" class="primary-link" :to="{ name: 'game-library', params: { gameId: game.gameId } }">开始游戏</RouterLink>
        <button v-else type="button" class="primary-button" :disabled="actionLoading || ownershipLoading" @click="emit('primary-action')">
          {{ actionLoading ? '正在处理...' : game.finalPrice > 0 ? '购买游戏' : '免费入库' }}
        </button>
        <div v-if="isDst" class="action-links">
          <RouterLink :to="communityRoute">社区入口</RouterLink>
          <RouterLink :to="workshopRoute">创意工坊</RouterLink>
        </div>
      </section>
    </template>

    <template #main>
      <section class="module-grid">
        <GameSummarySection
          title="DLC / 皮肤箱 / 礼包"
          :loading="packages.loading"
          :error="packages.error"
          :empty="!packages.data.length"
        >
          <div class="package-list">
            <article v-for="item in packages.data" :key="item.packageId">
              <div>
                <strong>{{ item.packageName }}</strong>
                <span>{{ item.packageType }}</span>
              </div>
              <GamePriceBlock :base-price="item.basePrice" :final-price="item.finalPrice" :discount-rate="item.discountRate" compact />
            </article>
          </div>
        </GameSummarySection>

        <GameSummarySection title="更新公告" :loading="false" error="" :empty="!isDst">
          <div class="notice-list">
            <article v-for="notice in dstNotices" :key="notice.title">
              <strong>{{ notice.title }}</strong>
              <span>{{ notice.date }}</span>
              <p>{{ notice.summary }}</p>
            </article>
          </div>
          <RouterLink v-if="isDst" class="section-link" :to="{ name: 'store' }">查看商店公告</RouterLink>
        </GameSummarySection>

        <GameSummarySection title="评价概览" :loading="reviewState.loading" :error="reviewState.error" :empty="!review">
          <dl v-if="review" class="stat-grid">
            <div><dt>口碑</dt><dd class="blue">{{ review.ratingText }}</dd></div>
            <div><dt>评价数</dt><dd>{{ review.reviewCount }}</dd></div>
            <div><dt>推荐率</dt><dd>{{ review.recommendRate }}%</dd></div>
            <div><dt>推荐数</dt><dd>{{ review.recommendCount }}</dd></div>
          </dl>
          <p v-if="review?.latestReviewContent" class="module-note">{{ review.latestReviewContent }}</p>
          <RouterLink class="section-link" :to="communityRoute">进入社区评价</RouterLink>
        </GameSummarySection>

        <GameSummarySection title="项目成就概览" :loading="achievements.loading" :error="achievements.error" :empty="!achievements.data">
          <dl v-if="achievements.data" class="stat-grid">
            <div><dt>成就数</dt><dd>{{ achievements.data.achievementCount }}</dd></div>
            <div><dt>平均达成率</dt><dd>{{ formatRate(achievements.data.averageGlobalRate) }}</dd></div>
          </dl>
          <div v-if="achievements.data?.achievements.length" class="mini-list">
                        <article v-for="item in achievements.data.achievements.slice(0, 3)" :key="item.achievementId">
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
          <p>{{ detailSummary }}</p>
        </section>
      </section>
    </template>

    <template #side>
      <SteamInfoPanel
        title="功能"
        :rows="isDst ? ['多人在线合作', 'Steam 创意工坊', '可用附加内容', '社区中心'] : ['社区中心']"
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
import type { GameAchievementSummary, GameAchievementSummaryItem, GameContentPackage, GameDetail, GameItemSummary, GameReviewSummary } from '../api/types';
import { getAchievementIcon } from '../data/achievementCatalog';
import { getGameMeta } from '../data/gameCatalog';
import GamePriceBlock from './GamePriceBlock.vue';
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
  packages: ModuleState<GameContentPackage[]>;
  items: ModuleState<GameItemSummary | null>;
  review: GameReviewSummary | null;
  reviewState: ModuleState<GameReviewSummary | null>;
  achievements: ModuleState<GameAchievementSummary | null>;
  owned: boolean;
  ownershipLoading?: boolean;
  ownershipError?: string;
  actionLoading?: boolean;
}>();

const emit = defineEmits<{
  (event: 'primary-action'): void;
}>();

const isDst = computed(() => props.game.shortName === 'DST');
const meta = computed(() => getGameMeta(props.game.gameId));
const communityRoute = computed(() => ({ name: 'game-community', params: { gameId: props.game.gameId } }));
const workshopRoute = computed(() => ({ name: 'game-community', params: { gameId: props.game.gameId }, query: { section: 'workshop' } }));

const dstNotices = [
  {
    title: '联机生存内容已更新',
    date: '2026-07-09',
    summary: '新的内容包、社区评价与成就信息现已上线。'
  },
  {
    title: '社区与创意工坊入口保留',
    date: '2026-07-08',
    summary: '详情页只提供入口，社区互动和成就解锁由对应页面继续承接。'
  }
];

const detailSummary = computed(() => {
  if (isDst.value) {
    return '与好友一起进入一片奇异而危险的世界。收集资源、制作物品、建立营地，并在不断变化的荒野中共同生存。';
  }
  return props.game.description || props.game.summary || '该游戏使用统一详情模板展示基础信息，等待后端继续补充字段。';
});

const purchaseCopy = computed(() =>
  isDst.value
    ? '购买后游戏将立即添加到你的游戏库。'
    : '购买后即可在游戏库中安装并游玩。'
);

function formatRate(value: number | null) {
  return typeof value === 'number' ? `${value}%` : '暂无';
}

function summaryAchievementIcon(item: GameAchievementSummaryItem) {
  return getAchievementIcon({ achId: item.achievementId });
}
</script>

<style scoped>
.game-media {
  height: 420px;
  overflow: hidden;
  background: #080d13;
}

.game-media img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.game-capsule {
  display: block;
  width: 100%;
  aspect-ratio: 460 / 215;
  object-fit: cover;
}

.summary-tags {
  display: flex;
  gap: 0.35rem;
  flex-wrap: wrap;
  padding: 0 1rem 1rem;
}

.summary-tags span {
  border-radius: 3px;
  padding: 0.18rem 0.4rem;
  color: #d7e4f0;
  background: rgba(151, 170, 195, 0.14);
  font-size: 0.78rem;
}

.purchase-panel {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto auto;
  gap: 0.85rem;
  align-items: center;
  border-radius: 4px;
  padding: 1rem;
  background: linear-gradient(90deg, rgba(17, 25, 35, 0.96), rgba(27, 40, 56, 0.96));
}

.purchase-panel h2,
.purchase-panel p,
.about-panel h2,
.about-panel p,
.module-note,
.notice-list p {
  margin: 0;
}

.purchase-panel h2,
.about-panel h2 {
  font-size: 1.14rem;
}

.purchase-panel p,
.about-panel p,
.module-note,
.notice-list p {
  color: var(--steam-muted);
  line-height: 1.65;
  text-wrap: pretty;
}

.primary-button,
.primary-link,
.action-links a,
.section-link {
  border-radius: 4px;
  padding: 0.62rem 1rem;
  font-weight: 900;
  white-space: nowrap;
}

.primary-button,
.primary-link {
  border: 0;
  color: #102009;
  background: var(--steam-green);
  cursor: pointer;
  text-decoration: none;
}

.section-link {
  display: inline-block;
  width: fit-content;
  margin-top: 0.75rem;
  border: 1px solid var(--steam-border);
  color: var(--steam-blue);
  background: rgba(42, 71, 94, 0.86);
  text-decoration: none;
}

.action-links {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.action-links a {
  border: 1px solid var(--steam-border);
  color: var(--steam-blue);
  background: rgba(42, 71, 94, 0.86);
  text-decoration: none;
}

.module-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1rem;
}

.package-list,
.mini-list,
.notice-list {
  display: grid;
  gap: 0.65rem;
}

.package-list article,
.mini-list article,
.notice-list article {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 0.75rem;
  align-items: center;
  border-bottom: 1px solid rgba(151, 170, 195, 0.12);
  padding-bottom: 0.55rem;
}

.mini-list article {
  grid-template-columns: 44px minmax(0, 1fr);
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

.package-list strong,
.notice-list strong,
.mini-list strong {
  color: var(--steam-text);
}

.package-list span,
.notice-list span,
.mini-list span {
  color: var(--steam-blue);
  font-size: 0.85rem;
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

.about-panel {
  border: 1px solid var(--steam-border);
  border-radius: 6px;
  padding: 1rem;
  background: rgba(22, 32, 45, 0.84);
}

@media (max-width: 1080px) {
  .purchase-panel,
  .module-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 620px) {
  .game-banner,
  .game-media {
    min-height: 260px;
  }

  .stat-grid,
  .package-list article,
  .mini-list article,
  .notice-list article {
    grid-template-columns: 1fr;
  }
}
</style>
