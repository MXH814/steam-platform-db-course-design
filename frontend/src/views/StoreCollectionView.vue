<template>
  <div class="collection-view">
    <GameFilterBar v-model="query" />

    <section class="collection-header">
      <div>
        <p>Store Browse</p>
        <h1>{{ pageTitle }}</h1>
        <span>{{ pageDescription }}</span>
      </div>
      <RouterLink to="/store">返回商店首页</RouterLink>
    </section>

    <section v-if="availableCollections.length && !activeCollection" class="collection-cards" aria-label="可浏览栏目">
      <RouterLink v-for="item in availableCollections" :key="item.slug" class="collection-card" :to="item.to">
        <strong>{{ item.title }}</strong>
        <span>{{ item.summary }}</span>
      </RouterLink>
    </section>

    <PageState v-if="loading" kind="loading" title="正在加载列表" message="正在获取当前栏目下的游戏。" />
    <PageState v-else-if="error" kind="error" title="列表加载失败" :message="error" action-label="重试" @action="loadGames" />

    <template v-else>
      <section v-if="isFallback" class="fallback-notice">
        <strong>正在使用本地演示数据</strong>
        <span>{{ fallbackMessage }}</span>
      </section>

      <section class="store-section">
        <header class="store-heading">
          <h2>{{ activeCollection?.title || '全部可展示游戏' }}</h2>
          <span>{{ games.length }} 款游戏</span>
        </header>

        <div v-if="games.length" class="game-grid">
          <GameCard v-for="game in games" :key="game.gameId" :game="game" />
        </div>
        <PageState v-else kind="empty" title="当前栏目暂无游戏" message="特别栏目只保留免费和折扣区分，后续接入更多 GAME 数据后自动填充。" />
      </section>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { getApiError } from '../api/http';
import { getGames } from '../api/games';
import type { GameListItem, GameQuery } from '../api/types';
import GameCard from '../components/GameCard.vue';
import GameFilterBar from '../components/GameFilterBar.vue';
import PageState from '../components/PageState.vue';

type CollectionLink = {
  slug: string;
  title: string;
  summary: string;
  to: string;
  queryPatch?: Partial<GameQuery>;
};

const route = useRoute();
const query = ref<GameQuery>({ search: '', priceFilter: 'all', sort: 'default', page: 1, pageSize: 50 });
const games = ref<GameListItem[]>([]);
const loading = ref(false);
const error = ref('');
const isFallback = ref(false);
const fallbackMessage = ref('');
let retryTimer: number | undefined;

const specialLinks: CollectionLink[] = [
  { slug: 'discount', title: '折扣中', summary: '只显示买断制折扣游戏。', to: '/store/specials/discount', queryPatch: { priceFilter: 'discount' } },
  { slug: 'free', title: '免费入库', summary: '只显示免费游戏。', to: '/store/specials/free', queryPatch: { priceFilter: 'free' } }
];

const section = computed(() => String(route.params.section || 'specials'));
const collectionId = computed(() => String(route.params.collectionId || ''));
const availableCollections = computed(() => specialLinks);
const activeCollection = computed(() => availableCollections.value.find((item) => item.slug === collectionId.value));
const pageTitle = computed(() => '特别栏目');
const pageDescription = computed(() => '这里不再维护复杂分类，只保留折扣中和免费入库两个验收需要的入口。');

async function loadGames(options: { silent?: boolean } = {}) {
  if (!options.silent) loading.value = true;
  error.value = '';
  try {
    const page = await getGames({ ...query.value, ...activeCollection.value?.queryPatch });
    games.value = page.items;
    isFallback.value = page.source === 'fallback';
    fallbackMessage.value = page.warning || '';
  } catch (err) {
    error.value = getApiError(err);
  } finally {
    if (!options.silent) loading.value = false;
  }
}

watch([query, activeCollection], () => loadGames(), { deep: true });

onMounted(() => {
  loadGames();
  retryTimer = window.setInterval(() => {
    if (isFallback.value) loadGames({ silent: true });
  }, 8000);
});

onBeforeUnmount(() => {
  window.clearInterval(retryTimer);
});
</script>

<style scoped>
.collection-view {
  display: grid;
  gap: 1.1rem;
}

.collection-header {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  align-items: end;
  border: 1px solid rgba(102, 192, 244, 0.12);
  border-radius: 6px;
  padding: clamp(1rem, 2vw, 1.35rem);
  background: linear-gradient(90deg, rgba(27, 40, 56, 0.96), rgba(18, 35, 52, 0.96));
}

.collection-header p,
.collection-header h1,
.collection-header span,
.store-heading h2 {
  margin: 0;
}

.collection-header p {
  color: var(--steam-blue);
  font-weight: 900;
}

.collection-header h1 {
  margin-top: 0.25rem;
  font-size: clamp(1.6rem, 3vw, 2.35rem);
  line-height: 1.1;
  text-wrap: balance;
}

.collection-header span {
  display: block;
  margin-top: 0.4rem;
  color: var(--steam-muted);
  text-wrap: pretty;
}

.collection-header a {
  flex: 0 0 auto;
  border-radius: 4px;
  padding: 0.6rem 0.9rem;
  color: #06111b;
  background: var(--steam-blue);
  font-weight: 900;
}

.collection-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 0.8rem;
}

.collection-card {
  display: grid;
  gap: 0.35rem;
  min-height: 118px;
  align-content: end;
  border: 1px solid var(--steam-border);
  border-radius: 6px;
  padding: 0.9rem;
  background: rgba(22, 32, 45, 0.92);
  transition: border-color 160ms ease-out, background-color 160ms ease-out;
}

.collection-card:hover {
  border-color: rgba(102, 192, 244, 0.55);
  background: rgba(27, 40, 56, 0.98);
}

.collection-card strong {
  color: var(--steam-text);
  font-size: 1.05rem;
}

.collection-card span {
  color: var(--steam-muted);
  font-size: 0.9rem;
  text-wrap: pretty;
}

.fallback-notice {
  display: flex;
  gap: 0.65rem;
  align-items: center;
  flex-wrap: wrap;
  border: 1px solid rgba(102, 192, 244, 0.28);
  border-radius: 6px;
  padding: 0.75rem 0.9rem;
  color: #d9ebf8;
  background: rgba(26, 159, 255, 0.1);
}

.fallback-notice span {
  color: var(--steam-muted);
}

.store-section {
  display: grid;
  gap: 0.9rem;
}

.store-heading {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  align-items: end;
}

.store-heading h2 {
  font-size: 1.25rem;
}

.store-heading span {
  color: var(--steam-muted);
  font-variant-numeric: tabular-nums;
}

.game-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 0.9rem;
}

@media (prefers-reduced-motion: reduce) {
  .collection-card {
    transition: none;
  }
}

@media (max-width: 1080px) {
  .game-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 680px) {
  .collection-header,
  .store-heading {
    align-items: start;
    flex-direction: column;
  }

  .game-grid {
    grid-template-columns: 1fr;
  }
}
</style>
