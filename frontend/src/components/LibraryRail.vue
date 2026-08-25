<template>
  <aside class="library-rail" :class="{ compact: compactView }">
    <div class="rail-title-row">
      <RouterLink class="rail-home" to="/library">
        <Home :size="16" />
        主页
      </RouterLink>
      <button type="button" title="切换紧凑视图" aria-label="切换紧凑视图" :aria-pressed="compactView" @click="compactView = !compactView">
        <Grid2X2 :size="16" />
      </button>
    </div>

    <button class="rail-filter" type="button" :aria-expanded="filterOpen" @click="filterOpen = !filterOpen">
      {{ filterMode === 'all' ? '游戏和软件' : '最近玩过' }}
      <ChevronDown :size="15" />
    </button>
    <div v-if="filterOpen" class="rail-filter-menu">
      <button type="button" :class="{ active: filterMode === 'all' }" @click="setFilter('all')">全部游戏</button>
      <button type="button" :class="{ active: filterMode === 'recent' }" @click="setFilter('recent')">最近玩过</button>
    </div>

    <label class="rail-search">
      <Search :size="15" />
      <input v-model="search" type="search" placeholder="搜索游戏库" aria-label="搜索游戏库" />
      <SlidersHorizontal :size="15" />
    </label>

    <div class="rail-collection-title">
      <span>收藏夹（{{ filteredEntries.length }}）</span>
      <PlayCircle :size="14" />
    </div>

    <nav class="rail-game-list" aria-label="游戏库列表">
      <RouterLink
        v-for="entry in filteredEntries"
        :key="entry.gameId"
        :to="{ name: 'game-library', params: { gameId: entry.gameId } }"
        :class="{ active: entry.gameId === activeGameId }"
      >
        <img :src="gameMeta(entry.gameId).coverImage" alt="" />
        <span>{{ entry.gameName }}</span>
      </RouterLink>
      <p v-if="!filteredEntries.length" class="rail-empty">没有匹配的游戏</p>
    </nav>

    <RouterLink class="rail-add" to="/store">
      <Plus :size="15" />
      添加游戏
    </RouterLink>
  </aside>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { ChevronDown, Grid2X2, Home, PlayCircle, Plus, Search, SlidersHorizontal } from '@lucide/vue';
import type { LibraryEntry } from '../api/coreApi';
import { getGameMeta } from '../data/gameCatalog';

const props = defineProps<{
  entries: LibraryEntry[];
  activeGameId?: string;
}>();

const search = ref('');
const compactView = ref(false);
const filterOpen = ref(false);
const filterMode = ref<'all' | 'recent'>('all');
const filteredEntries = computed(() => {
  const keyword = search.value.trim().toLocaleLowerCase('zh-CN');
  return props.entries.filter((entry) => {
    const matchesKeyword = !keyword || entry.gameName.toLocaleLowerCase('zh-CN').includes(keyword);
    const matchesFilter = filterMode.value === 'all' || entry.playMinutes > 0;
    return matchesKeyword && matchesFilter;
  });
});

const gameMeta = getGameMeta;

function setFilter(mode: 'all' | 'recent') {
  filterMode.value = mode;
  filterOpen.value = false;
}
</script>

<style scoped>
.library-rail {
  position: sticky;
  top: 114px;
  display: grid;
  grid-template-rows: auto auto auto auto minmax(0, 1fr) auto;
  width: 268px;
  height: calc(100vh - 114px);
  min-height: 560px;
  border-right: 1px solid #080c11;
  background: #171f2a;
  box-shadow: 2px 0 12px rgba(0, 0, 0, 0.24);
}

.rail-filter-menu {
  display: grid;
  margin: -4px 8px 6px;
  padding: 4px;
  background: #303b49;
  box-shadow: 0 8px 18px rgba(0, 0, 0, 0.36);
}

.rail-filter-menu button {
  min-height: 30px;
  border: 0;
  padding: 0 9px;
  color: #b4bec9;
  background: transparent;
  cursor: pointer;
  text-align: left;
}

.rail-filter-menu button:hover,
.rail-filter-menu button.active {
  color: #ffffff;
  background: #3f5369;
}

.rail-title-row,
.rail-filter,
.rail-search,
.rail-collection-title,
.rail-add {
  display: flex;
  align-items: center;
}

.rail-title-row {
  gap: 6px;
  padding: 8px 8px 6px;
}

.rail-home,
.rail-title-row button,
.rail-filter {
  min-height: 34px;
  border: 0;
  border-radius: 2px;
  color: #d6d7d8;
  background: #242f3d;
}

.rail-home {
  flex: 1;
  gap: 8px;
  padding: 0 10px;
  font-weight: 700;
}

.rail-title-row button {
  display: grid;
  width: 34px;
  place-items: center;
  cursor: pointer;
}

.rail-filter {
  justify-content: space-between;
  margin: 0 8px 6px;
  padding: 0 10px;
  cursor: pointer;
  font-size: 0.86rem;
}

.rail-search {
  gap: 7px;
  min-height: 34px;
  margin: 0 8px 8px;
  padding: 0 8px;
  color: #7e8a98;
  background: #0f151e;
}

.rail-search input {
  min-width: 0;
  height: 30px;
  border: 0;
  border-radius: 0;
  padding: 0;
  background: transparent;
  font-size: 0.82rem;
}

.rail-collection-title {
  justify-content: space-between;
  padding: 6px 12px;
  color: #8b96a5;
  font-size: 0.73rem;
  font-weight: 700;
  text-transform: uppercase;
}

.rail-game-list {
  min-height: 0;
  overflow-y: auto;
  padding: 0 6px;
}

.rail-game-list a {
  display: grid;
  grid-template-columns: 28px minmax(0, 1fr);
  gap: 8px;
  align-items: center;
  min-height: 36px;
  padding: 3px 7px;
  color: #aeb6c1;
  font-size: 0.82rem;
}

.rail-game-list a:hover,
.rail-game-list a.active {
  color: #ffffff;
  background: #3a506a;
}

.rail-game-list img {
  width: 28px;
  height: 28px;
  object-fit: cover;
}

.library-rail.compact .rail-game-list a {
  min-height: 29px;
}

.library-rail.compact .rail-game-list img {
  width: 22px;
  height: 22px;
}

.rail-game-list span {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.rail-empty {
  margin: 8px;
  color: #7d8997;
  font-size: 0.82rem;
}

.rail-add {
  gap: 8px;
  min-height: 42px;
  padding: 0 12px;
  border-top: 1px solid rgba(255, 255, 255, 0.05);
  color: #9da7b4;
  background: #111821;
  font-size: 0.82rem;
}

@media (max-width: 920px) {
  .library-rail {
    display: none;
  }
}
</style>
