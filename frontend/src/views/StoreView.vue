<template>
  <div class="store-view">
    <GameFilterBar v-model="query" />

    <PageState v-if="loading" kind="loading" title="正在加载商店" message="正在获取游戏列表和精选推荐。" />
    <PageState v-else-if="error" kind="error" title="商店加载失败" :message="error" action-label="重试" @action="loadGames" />
    <template v-else>
      <section v-if="isFallback" class="fallback-notice">
        <strong>正在使用本地演示数据</strong>
        <span>{{ fallbackMessage }}</span>
      </section>
      <GameHeroPanel v-if="games.length" :games="games" />
      <PageState v-else kind="empty" title="没有找到游戏" message="换一个搜索词或筛选条件试试。" />

      <section class="store-section">
        <header class="store-heading">
          <div>
            <p>Group B / Store</p>
            <h1>游戏商店</h1>
          </div>
          <span>{{ games.length }} 款可展示游戏</span>
        </header>

        <div v-if="games.length" class="game-grid">
          <GameCard v-for="game in games" :key="game.gameId" :game="game" />
        </div>
      </section>
    </template>
  </div>
</template>

<script setup lang="ts">
import { onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue';
import { getApiError } from '../api/http';
import { getGames } from '../api/games';
import type { GameListItem, GameQuery } from '../api/types';
import GameCard from '../components/GameCard.vue';
import GameFilterBar from '../components/GameFilterBar.vue';
import GameHeroPanel from '../components/GameHeroPanel.vue';
import PageState from '../components/PageState.vue';

let query = reactive<GameQuery>({
  search: '',
  priceFilter: 'all',
  sort: 'default',
  page: 1,
  pageSize: 50
});

const games = ref<GameListItem[]>([]);
const loading = ref(false);
const error = ref('');
const isFallback = ref(false);
const fallbackMessage = ref('');
let retryTimer: number | undefined;

async function loadGames(options: { silent?: boolean } = {}) {
  if (!options.silent) {
    loading.value = true;
  }
  error.value = '';
  try {
    const page = await getGames(query);
    games.value = page.items;
    isFallback.value = page.source === 'fallback';
    fallbackMessage.value = page.warning || '';
  } catch (err) {
    error.value = getApiError(err);
  } finally {
    if (!options.silent) {
      loading.value = false;
    }
  }
}

watch(query, () => loadGames(), { deep: true });

onMounted(() => {
  loadGames();
  retryTimer = window.setInterval(() => {
    if (isFallback.value) {
      loadGames({ silent: true });
    }
  }, 8000);
});

onBeforeUnmount(() => {
  window.clearInterval(retryTimer);
});
</script>

<style scoped>
.store-view {
  display: grid;
  gap: 1.1rem;
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
  align-items: end;
  justify-content: space-between;
  gap: 1rem;
}

.store-heading p,
.store-heading h1 {
  margin: 0;
}

.store-heading p {
  color: var(--steam-blue);
  font-size: 0.78rem;
  font-weight: 900;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.store-heading h1 {
  font-size: clamp(1.55rem, 3vw, 2.2rem);
  line-height: 1.1;
  text-wrap: balance;
}

.store-heading span {
  color: var(--steam-muted);
  font-size: 0.9rem;
}

.game-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 0.9rem;
}

@media (max-width: 1080px) {
  .game-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 680px) {
  .store-heading {
    align-items: start;
    flex-direction: column;
  }

  .game-grid {
    grid-template-columns: 1fr;
  }
}
</style>
