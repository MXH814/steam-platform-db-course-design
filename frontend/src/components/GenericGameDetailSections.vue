<template>
  <SteamGameDetailTemplate
    :game="game"
    :review="review"
    :variant="game.shortName === 'DST' ? 'dst' : 'default'"
    :summary="detailSummary"
  >
    <template #banner>
      <div class="game-banner" :class="{ dst: isDst }">
        <span>{{ game.shortName }}</span>
        <strong>{{ game.gameName }}</strong>
      </div>
    </template>

    <template #media>
      <div class="game-media" :class="{ dst: isDst }">
        <div>
          <span>{{ game.shortName }}</span>
          <strong>{{ isDst ? 'SURVIVAL PACKAGE DEMO' : 'STORE DETAIL DEMO' }}</strong>
        </div>
      </div>
    </template>

    <template #summary-art>
      <div class="game-capsule" :class="{ dst: isDst }">{{ game.gameName }}</div>
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
          <p v-else-if="ownershipError">暂未确认是否已入库。你仍可浏览商店详情，购买和入库由 Group C 接口负责。</p>
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

        <GameSummarySection title="成就概览" :loading="achievements.loading" :error="achievements.error" :empty="!achievements.data">
          <dl v-if="achievements.data" class="stat-grid">
            <div><dt>成就数</dt><dd>{{ achievements.data.achievementCount }}</dd></div>
            <div><dt>平均达成率</dt><dd>{{ achievements.data.averageGlobalRate ?? 0 }}%</dd></div>
          </dl>
          <div v-if="achievements.data?.achievements.length" class="mini-list">
            <article v-for="item in achievements.data.achievements.slice(0, 3)" :key="item.achievementId">
              <strong>{{ item.achievementName }}</strong>
              <span>{{ item.globalRate ?? 0 }}%</span>
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
        title="商店页保留入口"
        :rows="isDst ? ['购买区域展示', 'DLC / 皮肤箱 / 礼包', '更新公告', '社区入口', '创意工坊入口'] : ['统一详情模板', '社区入口']"
      />
      <SteamInfoPanel
        title="已入库后查看"
        :rows="isDst ? ['/library/GAME_DST', '个人游玩时长', '个人成就进度', '社区评价入口'] : [`/library/${game.gameId}`]"
      />
      <SteamInfoPanel
        title="Group B 边界"
        :rows="['不处理钱包扣款', '不生成订单事务', '不处理退款', '不直接修改数据库']"
      />
    </template>
  </SteamGameDetailTemplate>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { GameAchievementSummary, GameContentPackage, GameDetail, GameItemSummary, GameReviewSummary } from '../api/types';
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
const communityRoute = computed(() => ({ name: 'game-community', params: { gameId: props.game.gameId } }));
const workshopRoute = computed(() => ({ name: 'game-community', params: { gameId: props.game.gameId }, query: { section: 'workshop' } }));

const dstNotices = [
  {
    title: '联机生存演示数据已更新',
    date: '2026-07-09',
    summary: '用于答辩演示的 DLC、礼包、评价和成就摘要已经准备好。'
  },
  {
    title: '社区与创意工坊入口保留',
    date: '2026-07-08',
    summary: '详情页只提供入口，社区互动和成就解锁由对应页面继续承接。'
  }
];

const detailSummary = computed(() => {
  if (isDst.value) {
    return 'DST 在本项目中承担买断制购买、内容包、评价和成就样板。详情页保留这些可与后端接口联动的模块，并把社区入口接到已实现的评论与成就页面。';
  }
  return props.game.description || props.game.summary || '该游戏使用统一详情模板展示基础信息，等待后端继续补充字段。';
});

const purchaseCopy = computed(() =>
  isDst.value
    ? '买断制购买区域只展示价格、折扣和入口，钱包扣款、订单生成、退款事务由 Group C 承接。'
    : '通用游戏详情展示，等待后端补充业务入口。'
);
</script>

<style scoped>
.game-banner {
  display: grid;
  min-height: 138px;
  align-content: center;
  border: 12px solid rgba(7, 12, 18, 0.8);
  padding: 1rem 2rem;
  background:
    radial-gradient(circle at 74% 26%, rgba(102, 192, 244, 0.22), transparent 12rem),
    linear-gradient(100deg, #132235, #24445d 56%, #111923);
}

.game-banner.dst {
  background:
    radial-gradient(circle at 78% 28%, rgba(117, 197, 63, 0.22), transparent 12rem),
    linear-gradient(100deg, #132117, #34512c 56%, #101822);
}

.game-banner span {
  color: var(--steam-blue);
  font-weight: 950;
}

.game-banner strong {
  color: #ffffff;
  font-size: clamp(1.8rem, 4vw, 3.1rem);
  line-height: 1.08;
  text-wrap: balance;
}

.game-media {
  display: grid;
  min-height: 420px;
  place-items: center;
  background:
    radial-gradient(circle at 62% 42%, rgba(102, 192, 244, 0.18), transparent 10rem),
    linear-gradient(135deg, #080d13, #31465c);
}

.game-media.dst {
  background:
    radial-gradient(circle at 62% 42%, rgba(117, 197, 63, 0.18), transparent 10rem),
    linear-gradient(135deg, #10130d, #2f4528);
}

.game-media div {
  display: grid;
  justify-items: center;
  gap: 0.25rem;
  color: rgba(255, 255, 255, 0.86);
}

.game-media span {
  font-size: clamp(3.5rem, 12vw, 7rem);
  font-weight: 950;
  line-height: 0.95;
}

.game-capsule {
  display: grid;
  min-height: 178px;
  place-items: center;
  padding: 1rem;
  color: #ffffff;
  background: linear-gradient(135deg, #31465c, #111923);
  font-size: clamp(1.4rem, 3vw, 2.2rem);
  font-weight: 950;
  text-align: center;
  text-wrap: balance;
}

.game-capsule.dst {
  background: linear-gradient(135deg, #38512f, #111923);
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
