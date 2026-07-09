<template>
  <div class="game-detail">
    <GameFilterBar v-model="storeQuery" />

    <PageState v-if="loading" kind="loading" title="正在加载游戏详情" />
    <PageState v-else-if="error" kind="error" title="游戏详情加载失败" :message="error" action-label="重试" @action="loadDetail" />
    <template v-else-if="game">
      <nav class="breadcrumbs" aria-label="面包屑">
        <RouterLink to="/store">所有游戏</RouterLink>
        <span>›</span>
        <span>{{ game.tags[0] || '商店' }}</span>
        <span>›</span>
        <strong>{{ game.gameName }}</strong>
      </nav>

      <header class="detail-title">
        <h1>{{ game.gameName }}</h1>
        <StatusBadge :status="game.status" />
      </header>

      <section class="detail-hero">
        <div class="media-panel" :class="`tone-${game.coverTone}`">
          <span>{{ game.shortName }}</span>
        </div>
        <aside class="info-panel">
          <div class="capsule" :class="`tone-${game.coverTone}`">{{ game.shortName }}</div>
          <p>{{ game.description }}</p>
          <dl>
            <div>
              <dt>最近评测</dt>
              <dd class="blue">{{ review.data?.ratingText || game.reputation }}</dd>
            </div>
            <div>
              <dt>发行日期</dt>
              <dd>{{ formatDate(game.releaseDate) }}</dd>
            </div>
            <div>
              <dt>开发商</dt>
              <dd class="blue">{{ game.developerName }}</dd>
            </div>
            <div>
              <dt>标签</dt>
              <dd class="tag-list">
                <span v-for="tag in game.tags" :key="tag">{{ tag }}</span>
              </dd>
            </div>
          </dl>
        </aside>
      </section>

      <section class="purchase-panel">
        <div>
          <h2>{{ game.shortName === 'CS2' ? '免费加入库' : '购买完整游戏' }}</h2>
          <p>{{ game.shortName === 'CS2' ? '进入库存与饰品市场演示主链路。' : '买断制购买，进入订单与钱包扣款演示主链路。' }}</p>
        </div>
        <GamePriceBlock :base-price="game.basePrice" :final-price="game.finalPrice" :discount-rate="game.discountRate" />
        <button type="button" class="primary-button">{{ game.shortName === 'CS2' ? '免费入库' : '购买游戏' }}</button>
      </section>

      <div v-if="game.shortName === 'CS2'" class="quick-links">
        <RouterLink class="button-link" to="/market">进入饰品市场</RouterLink>
        <RouterLink class="ghost-button" to="/account">查看库存</RouterLink>
      </div>

      <section class="summary-grid">
        <GameSummarySection
          title="内容包 / 礼包"
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

        <GameSummarySection title="物品摘要" :loading="items.loading" :error="items.error" :empty="!items.data">
          <dl v-if="items.data" class="stat-grid">
            <div><dt>模板数</dt><dd>{{ items.data.templateCount }}</dd></div>
            <div><dt>库存实例</dt><dd>{{ items.data.inventoryItemCount }}</dd></div>
            <div><dt>买单</dt><dd>{{ items.data.activeBuyOrderCount }}</dd></div>
            <div><dt>卖单</dt><dd>{{ items.data.activeSellOrderCount }}</dd></div>
            <div><dt>成交数</dt><dd>{{ items.data.tradeCount }}</dd></div>
            <div><dt>最近成交</dt><dd>{{ money(items.data.lastTradePrice) }}</dd></div>
          </dl>
        </GameSummarySection>

        <GameSummarySection title="评价概览" :loading="review.loading" :error="review.error" :empty="!review.data">
          <dl v-if="review.data" class="stat-grid">
            <div><dt>口碑</dt><dd class="blue">{{ review.data.ratingText }}</dd></div>
            <div><dt>评价数</dt><dd>{{ review.data.reviewCount }}</dd></div>
            <div><dt>推荐率</dt><dd>{{ review.data.recommendRate }}%</dd></div>
            <div><dt>推荐数</dt><dd>{{ review.data.recommendCount }}</dd></div>
          </dl>
          <p v-if="review.data?.latestReviewContent" class="summary-note">{{ review.data.latestReviewContent }}</p>
        </GameSummarySection>

        <GameSummarySection title="成就概览" :loading="achievements.loading" :error="achievements.error" :empty="!achievements.data">
          <dl v-if="achievements.data" class="stat-grid">
            <div><dt>成就数</dt><dd>{{ achievements.data.achievementCount }}</dd></div>
            <div><dt>平均达成率</dt><dd>{{ achievements.data.averageGlobalRate ?? 0 }}%</dd></div>
          </dl>
          <div v-if="achievements.data?.achievements.length" class="achievement-list">
            <article v-for="item in achievements.data.achievements.slice(0, 3)" :key="item.achievementId">
              <strong>{{ item.achievementName }}</strong>
              <span>{{ item.globalRate ?? 0 }}%</span>
            </article>
          </div>
        </GameSummarySection>
      </section>
    </template>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { getApiError } from '../api/http';
import {
  getGameAchievementSummary,
  getGameContentPackages,
  getGameDetail,
  getGameItemSummary,
  getGameReviewSummary
} from '../api/games';
import type { GameAchievementSummary, GameContentPackage, GameDetail, GameItemSummary, GameQuery, GameReviewSummary } from '../api/types';
import GameFilterBar from '../components/GameFilterBar.vue';
import GamePriceBlock from '../components/GamePriceBlock.vue';
import GameSummarySection from '../components/GameSummarySection.vue';
import PageState from '../components/PageState.vue';
import StatusBadge from '../components/StatusBadge.vue';

type ModuleState<T> = {
  loading: boolean;
  error: string;
  data: T;
};

const route = useRoute();
const router = useRouter();
let storeQuery = reactive<GameQuery>({ search: '', priceFilter: 'all', sort: 'default' });

const game = ref<GameDetail | null>(null);
const loading = ref(false);
const error = ref('');
const packages = reactive<ModuleState<GameContentPackage[]>>({ loading: false, error: '', data: [] });
const items = reactive<ModuleState<GameItemSummary | null>>({ loading: false, error: '', data: null });
const review = reactive<ModuleState<GameReviewSummary | null>>({ loading: false, error: '', data: null });
const achievements = reactive<ModuleState<GameAchievementSummary | null>>({ loading: false, error: '', data: null });

watch(
  () => storeQuery.search,
  (value) => {
    if (value) {
      router.push({ name: 'store', query: { search: value } });
    }
  }
);

watch(
  () => route.params.gameId,
  () => loadDetail()
);

async function loadModule<T>(state: ModuleState<T>, loader: () => Promise<T>) {
  state.loading = true;
  state.error = '';
  try {
    state.data = await loader();
  } catch (err) {
    state.error = getApiError(err);
  } finally {
    state.loading = false;
  }
}

async function loadDetail() {
  const gameId = String(route.params.gameId || '');
  loading.value = true;
  error.value = '';
  game.value = null;
  try {
    game.value = await getGameDetail(gameId);
    await Promise.all([
      loadModule(packages, () => getGameContentPackages(gameId)),
      loadModule(items, () => getGameItemSummary(gameId)),
      loadModule(review, () => getGameReviewSummary(gameId)),
      loadModule(achievements, () => getGameAchievementSummary(gameId))
    ]);
  } catch (err) {
    error.value = getApiError(err);
  } finally {
    loading.value = false;
  }
}

function formatDate(value: string) {
  return value ? new Date(value).toLocaleDateString('zh-CN') : '待定';
}

function money(value: number | null) {
  return typeof value === 'number' ? `¥${value.toFixed(2)}` : '-';
}

onMounted(loadDetail);
</script>

<style scoped>
.game-detail {
  display: grid;
  gap: 1rem;
}

.breadcrumbs,
.detail-title,
.quick-links {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  flex-wrap: wrap;
}

.breadcrumbs {
  color: var(--steam-muted);
  font-size: 0.9rem;
}

.breadcrumbs a {
  color: var(--steam-blue);
}

.detail-title {
  justify-content: space-between;
}

.detail-title h1 {
  margin: 0;
  font-size: clamp(1.8rem, 4vw, 2.65rem);
  line-height: 1.08;
  text-wrap: balance;
}

.detail-hero {
  display: grid;
  grid-template-columns: minmax(0, 1.55fr) minmax(300px, 0.75fr);
  gap: 1rem;
}

.media-panel,
.capsule {
  display: grid;
  place-items: center;
  color: #ffffff;
  font-weight: 950;
  background: linear-gradient(135deg, #17283a, #0d1118);
}

.media-panel {
  min-height: 430px;
  border: 12px solid rgba(4, 8, 12, 0.86);
  font-size: clamp(4rem, 12vw, 8rem);
}

.capsule {
  min-height: 142px;
  border-radius: 4px;
  font-size: 3rem;
}

.tone-cs2 {
  background: linear-gradient(135deg, #111924, #1b5b74 60%, #10151d);
}

.tone-dst {
  background: linear-gradient(135deg, #112015, #315739 62%, #0d1118);
}

.info-panel {
  display: grid;
  gap: 0.8rem;
  align-content: start;
  border-radius: 4px;
  padding: 0.9rem;
  background: linear-gradient(180deg, rgba(27, 40, 56, 0.98), rgba(16, 24, 34, 0.96));
}

.info-panel p,
.summary-note {
  margin: 0;
  color: #c7d5e0;
  text-wrap: pretty;
}

.info-panel dl,
.stat-grid {
  display: grid;
  gap: 0.65rem;
  margin: 0;
}

.info-panel dl div,
.stat-grid div {
  display: grid;
  grid-template-columns: 92px minmax(0, 1fr);
  gap: 0.8rem;
}

.blue {
  color: var(--steam-blue);
}

.tag-list {
  display: flex;
  gap: 0.35rem;
  flex-wrap: wrap;
}

.tag-list span {
  border-radius: 3px;
  padding: 0.14rem 0.35rem;
  color: #d8e4ef;
  background: rgba(151, 170, 195, 0.16);
  font-size: 0.76rem;
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
.purchase-panel p {
  margin: 0;
}

.purchase-panel h2 {
  font-size: 1.1rem;
}

.purchase-panel p {
  color: var(--steam-muted);
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1rem;
}

.package-list,
.achievement-list {
  display: grid;
  gap: 0.65rem;
}

.package-list article,
.achievement-list article {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 0.75rem;
  align-items: center;
  border-radius: 4px;
  padding: 0.65rem;
  background: rgba(8, 13, 19, 0.36);
}

.package-list strong,
.package-list span,
.achievement-list strong,
.achievement-list span {
  display: block;
}

.package-list span,
.achievement-list span {
  color: var(--steam-muted);
  font-size: 0.82rem;
}

@media (max-width: 900px) {
  .detail-hero,
  .summary-grid,
  .purchase-panel {
    grid-template-columns: 1fr;
  }

  .media-panel {
    min-height: 300px;
  }
}
</style>
