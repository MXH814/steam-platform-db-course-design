<template>
  <div class="game-detail">
    <GameFilterBar v-model="storeQuery" />

    <PageState v-if="loading" kind="loading" title="正在加载游戏详情" />
    <PageState v-else-if="error" kind="error" title="游戏详情加载失败" :message="error" action-label="重试" @action="loadDetail" />

    <template v-else-if="game">
      <p v-if="actionMessage" class="detail-feedback success">{{ actionMessage }}</p>
      <p v-if="actionError" class="detail-feedback error">{{ actionError }}</p>

      <nav class="breadcrumbs" aria-label="面包屑">
        <RouterLink to="/store">所有游戏</RouterLink>
        <span>/</span>
        <span>{{ game.shortName === 'CS2' ? '免费开玩游戏' : game.tags[0] || '商店' }}</span>
        <span>/</span>
        <strong>{{ game.gameName }}</strong>
      </nav>

      <header class="detail-title">
        <div>
          <h1>{{ game.gameName }}</h1>
          <p v-if="ownership.loading">正在确认游戏库状态...</p>
          <p v-else-if="ownership.error">暂未确认是否已入库，当前仍可浏览商店详情。</p>
          <p v-else-if="ownership.owned">已在你的游戏库中，商店页将保留商品信息和社区入口。</p>
          <p v-else>尚未入库，当前展示面向商店浏览和购买决策的信息。</p>
        </div>
        <RouterLink class="community-link" :to="{ name: 'game-community', params: { gameId: game.gameId } }">
          评价与成就
        </RouterLink>
      </header>

      <Cs2DetailSections
        v-if="game.shortName === 'CS2'"
        :game="game"
        :items="items.data"
        :items-state="items"
        :review="review.data"
        :review-state="review"
        :achievements="achievements.data"
        :achievements-state="achievements"
        :owned="ownership.owned"
        :ownership-loading="ownership.loading"
        :ownership-error="ownership.error"
        :action-loading="actionLoading"
        @primary-action="handlePrimaryAction"
      />
      <GenericGameDetailSections
        v-else
        :game="game"
        :packages="packages"
        :items="items"
        :review="review.data"
        :review-state="review"
        :achievements="achievements"
        :owned="ownership.owned"
        :ownership-loading="ownership.loading"
        :ownership-error="ownership.error"
        :action-loading="actionLoading"
        @primary-action="handlePrimaryAction"
      />
    </template>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { claimFreeGame } from '../api/coreApi';
import {
  getGameAchievementSummary,
  getGameContentPackages,
  getGameDetail,
  getGameItemSummary,
  getGameOwnership,
  getGameReviewSummary
} from '../api/games';
import { getApiError } from '../api/http';
import type { GameAchievementSummary, GameContentPackage, GameDetail, GameItemSummary, GameQuery, GameReviewSummary } from '../api/types';
import Cs2DetailSections from '../components/Cs2DetailSections.vue';
import GameFilterBar from '../components/GameFilterBar.vue';
import GenericGameDetailSections from '../components/GenericGameDetailSections.vue';
import PageState from '../components/PageState.vue';

type ModuleState<T> = {
  loading: boolean;
  error: string;
  data: T;
};

const route = useRoute();
const router = useRouter();
const storeQuery = ref<GameQuery>({ search: '', priceFilter: 'all', sort: 'default' });

const game = ref<GameDetail | null>(null);
const loading = ref(false);
const error = ref('');
const packages = reactive<ModuleState<GameContentPackage[]>>({ loading: false, error: '', data: [] });
const items = reactive<ModuleState<GameItemSummary | null>>({ loading: false, error: '', data: null });
const review = reactive<ModuleState<GameReviewSummary | null>>({ loading: false, error: '', data: null });
const achievements = reactive<ModuleState<GameAchievementSummary | null>>({ loading: false, error: '', data: null });
const ownership = reactive({ loading: false, error: '', owned: false });
const actionLoading = ref(false);
const actionError = ref('');
const actionMessage = ref('');

watch(
  () => storeQuery.value.search,
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

async function loadOwnership(gameId: string) {
  ownership.loading = true;
  ownership.error = '';
  try {
    const result = await getGameOwnership(gameId);
    ownership.owned = result.owned;
  } catch (err) {
    ownership.error = getApiError(err);
    ownership.owned = false;
  } finally {
    ownership.loading = false;
  }
}

async function loadDetail() {
  const gameId = String(route.params.gameId || '');
  loading.value = true;
  error.value = '';
  game.value = null;

  packages.data = [];
  items.data = null;
  review.data = null;
  achievements.data = null;
  ownership.owned = false;
  ownership.error = '';
  actionError.value = '';
  actionMessage.value = '';

  try {
    game.value = await getGameDetail(gameId);
    loadOwnership(gameId);
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

onMounted(loadDetail);

async function handlePrimaryAction() {
  if (!game.value || ownership.owned || actionLoading.value) return;

  actionLoading.value = true;
  actionError.value = '';
  actionMessage.value = '';
  try {
    if (game.value.finalPrice <= 0 || game.value.shortName === 'CS2') {
      await claimFreeGame(game.value.gameId);
      actionMessage.value = `${game.value.gameName} 已免费入库，可以开始游戏。`;
      await loadOwnership(game.value.gameId);
    } else {
      router.push({ name: 'game-checkout', params: { gameId: game.value.gameId } });
    }
  } catch (err) {
    actionError.value = getApiError(err);
  } finally {
    actionLoading.value = false;
  }
}
</script>

<style scoped>
.game-detail {
  display: grid;
  gap: 1rem;
}

.breadcrumbs,
.detail-title {
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

.detail-title h1,
.detail-title p {
  margin: 0;
}

.detail-title div {
  display: grid;
  gap: 0.35rem;
  min-width: 0;
}

.detail-title h1 {
  font-size: clamp(1.8rem, 4vw, 2.65rem);
  line-height: 1.08;
  text-wrap: balance;
}

.detail-title p {
  max-width: 72ch;
  color: var(--steam-muted);
  line-height: 1.55;
  text-wrap: pretty;
}

.community-link {
  border-radius: 4px;
  padding: 0.55rem 1rem;
  color: var(--steam-blue);
  background: rgba(42, 71, 94, 0.86);
  font-weight: 850;
  text-decoration: none;
}

.detail-feedback {
  margin: 0;
  border-radius: 4px;
  padding: 0.75rem 0.9rem;
  font-weight: 850;
}

.detail-feedback.success {
  border: 1px solid rgba(117, 197, 63, 0.32);
  color: #d7f7bd;
  background: rgba(117, 197, 63, 0.12);
}

.detail-feedback.error {
  border: 1px solid rgba(255, 120, 92, 0.34);
  color: #ffb9a8;
  background: rgba(255, 120, 92, 0.1);
}
</style>
