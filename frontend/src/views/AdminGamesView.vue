<template>
  <section class="admin-games view-stack">
    <div class="section-heading">
      <p class="eyebrow">Admin Console</p>
      <h1>游戏上下架</h1>
    </div>

    <section class="admin-game-toolbar">
      <div>
        <span class="pill">Group B</span>
        <h2>控制公开商店中的游戏状态</h2>
        <p>开发商提交的新游戏默认下架，管理员上架后玩家才能在商店访问。</p>
      </div>
      <div class="status-tabs" role="tablist" aria-label="游戏状态筛选">
        <button type="button" :class="{ active: filter === 'all' }" @click="filter = 'all'">全部</button>
        <button type="button" :class="{ active: filter === 'ONLINE' }" @click="filter = 'ONLINE'">已上架</button>
        <button type="button" :class="{ active: filter === 'OFFLINE' }" @click="filter = 'OFFLINE'">已下架</button>
      </div>
    </section>

    <div v-if="message" class="state-panel success">{{ message }}</div>
    <div v-if="error" class="state-panel error">{{ error }}</div>

    <section class="data-panel">
      <header class="panel-header">
        <div>
          <h2>游戏列表</h2>
          <span>{{ visibleGames.length }} / {{ games.length }} 款</span>
        </div>
        <button class="ghost-button" type="button" :disabled="loading" @click="loadGames">刷新</button>
      </header>

      <PageState v-if="loading" kind="loading" title="正在加载游戏上下架列表" />
      <PageState v-else-if="!visibleGames.length" kind="empty" title="当前筛选没有游戏" message="切换状态筛选或刷新后再试。" />
      <div v-else class="admin-game-list">
        <article v-for="game in visibleGames" :key="game.gameId" class="admin-game-row">
          <div class="game-main">
            <strong>{{ game.gameName }}</strong>
            <span>{{ game.gameId }} · {{ game.developerName }} · {{ formatDate(game.releaseDate) }}</span>
          </div>
          <GamePriceBlock :base-price="game.basePrice" :final-price="game.finalPrice" :discount-rate="game.discountRate" compact />
          <StatusBadge :status="game.status" />
          <div class="row-actions">
            <button
              v-if="game.status === 'OFFLINE'"
              class="primary-button"
              type="button"
              :disabled="busyGameId === game.gameId"
              @click="changeStatus(game, 'ONLINE')"
            >
              上架
            </button>
            <button
              v-else
              class="ghost-button"
              type="button"
              :disabled="busyGameId === game.gameId"
              @click="changeStatus(game, 'OFFLINE')"
            >
              下架
            </button>
            <RouterLink class="ghost-button" :to="`/games/${encodeURIComponent(game.gameId)}`">详情</RouterLink>
          </div>
        </article>
      </div>
    </section>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { getGames, setGameOffline, setGameOnline } from '../api/games';
import { getApiError } from '../api/http';
import type { GameListItem } from '../api/types';
import GamePriceBlock from '../components/GamePriceBlock.vue';
import PageState from '../components/PageState.vue';
import StatusBadge from '../components/StatusBadge.vue';

type StatusFilter = 'all' | 'ONLINE' | 'OFFLINE';

const games = ref<GameListItem[]>([]);
const filter = ref<StatusFilter>('all');
const loading = ref(false);
const busyGameId = ref('');
const message = ref('');
const error = ref('');

const visibleGames = computed(() => {
  if (filter.value === 'all') {
    return games.value;
  }

  return games.value.filter((game) => game.status === filter.value);
});

onMounted(loadGames);

async function loadGames() {
  loading.value = true;
  error.value = '';
  try {
    const [online, offline] = await Promise.all([
      getGames({ status: 'ONLINE', pageSize: 100 }),
      getGames({ status: 'OFFLINE', pageSize: 100 })
    ]);
    games.value = mergeGames([...online.items, ...offline.items]);
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    loading.value = false;
  }
}

async function changeStatus(game: GameListItem, status: 'ONLINE' | 'OFFLINE') {
  message.value = '';
  error.value = '';
  busyGameId.value = game.gameId;

  try {
    const updated = status === 'ONLINE' ? await setGameOnline(game.gameId) : await setGameOffline(game.gameId);
    message.value = `${updated.gameName} 已${status === 'ONLINE' ? '上架' : '下架'}`;
    await loadGames();
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    busyGameId.value = '';
  }
}

function mergeGames(items: GameListItem[]) {
  return Array.from(new Map(items.map((item) => [item.gameId, item])).values()).sort((a, b) => {
    if (a.status !== b.status) {
      return a.status === 'OFFLINE' ? -1 : 1;
    }

    return a.gameName.localeCompare(b.gameName, 'zh-CN');
  });
}

function formatDate(value: string) {
  return value ? new Date(value).toLocaleDateString('zh-CN') : '-';
}
</script>

<style scoped>
.admin-games {
  gap: 1.1rem;
}

.admin-game-toolbar,
.admin-game-row,
.status-tabs {
  display: grid;
  gap: 1rem;
}

.admin-game-toolbar {
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: end;
  border: 1px solid rgba(102, 192, 244, 0.2);
  border-radius: 8px;
  padding: 1.2rem;
  background:
    linear-gradient(135deg, rgba(117, 197, 63, 0.11), transparent 44%),
    rgba(20, 28, 39, 0.92);
}

.admin-game-toolbar h2,
.admin-game-toolbar p {
  margin: 0;
}

.admin-game-toolbar h2 {
  margin-top: 0.7rem;
  font-size: 1.45rem;
}

.admin-game-toolbar p,
.panel-header span,
.game-main span {
  color: var(--steam-muted);
}

.status-tabs {
  grid-template-columns: repeat(3, auto);
  width: fit-content;
  border: 1px solid rgba(151, 170, 195, 0.18);
  border-radius: 8px;
  padding: 0.25rem;
  background: rgba(8, 13, 19, 0.42);
}

.status-tabs button {
  min-height: 34px;
  border: 0;
  border-radius: 6px;
  padding: 0.35rem 0.75rem;
  color: #c9d3e3;
  background: transparent;
  cursor: pointer;
  font-weight: 800;
}

.status-tabs button.active,
.status-tabs button:hover {
  color: #06111b;
  background: var(--steam-blue);
}

.panel-header > div {
  display: grid;
  gap: 0.2rem;
}

.data-panel :deep(.page-state) {
  margin: 1rem;
}

.admin-game-list {
  display: grid;
}

.admin-game-row {
  grid-template-columns: minmax(0, 1fr) auto auto auto;
  align-items: center;
  padding: 1rem 1.1rem;
  border-bottom: 1px solid rgba(151, 170, 195, 0.12);
}

.admin-game-row:hover {
  background: rgba(127, 198, 255, 0.06);
}

.game-main {
  display: grid;
  gap: 0.25rem;
  min-width: 0;
}

.game-main strong {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

@media (max-width: 900px) {
  .admin-game-toolbar,
  .admin-game-row {
    grid-template-columns: 1fr;
  }

  .status-tabs {
    width: 100%;
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .row-actions {
    width: 100%;
  }

  .row-actions > * {
    flex: 1;
  }
}
</style>
