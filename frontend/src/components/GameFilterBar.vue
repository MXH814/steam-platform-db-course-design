<template>
  <section class="filter-bar" aria-label="商店筛选">
    <nav class="filter-links" aria-label="商店栏目">
      <RouterLink v-for="item in navItems" :key="item.label" :to="item.to" :class="{ active: isActive(item) }">
        {{ item.label }}
        <span v-if="item.hasChildren">⌄</span>
      </RouterLink>
    </nav>

    <form class="search-box" @submit.prevent="emitChange">
      <input v-model="draft.search" type="search" placeholder="搜索商店" aria-label="搜索商店" @input="queueChange" />
      <button type="submit" aria-label="搜索">⌕</button>
    </form>

    <div class="filter-controls" aria-label="游戏筛选条件">
      <label>
        <span>类型</span>
        <select v-model="draft.priceFilter" @change="emitChange">
          <option value="all">全部</option>
          <option value="free">免费</option>
          <option value="paid">买断制</option>
          <option value="discount">折扣中</option>
          <option value="market">支持市场</option>
          <option value="packages">有内容包</option>
        </select>
      </label>

      <label>
        <span>排序</span>
        <select v-model="draft.sort" @change="emitChange">
          <option value="default">推荐</option>
          <option value="price">价格</option>
          <option value="releaseDate">发行时间</option>
          <option value="reputation">口碑</option>
        </select>
      </label>
    </div>
  </section>
</template>

<script setup lang="ts">
import { reactive, watch } from 'vue';
import { useRoute } from 'vue-router';
import type { RouteLocationRaw } from 'vue-router';
import type { GameQuery } from '../api/types';

type NavItem = {
  label: string;
  section: string;
  to: RouteLocationRaw;
  hasChildren?: boolean;
};

const props = defineProps<{
  modelValue: GameQuery;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: GameQuery];
}>();

const route = useRoute();
const navItems: NavItem[] = [
  { label: '浏览', section: 'store', to: { name: 'store' }, hasChildren: true },
  { label: '推荐', section: 'recommend', to: { name: 'store-section', params: { section: 'recommend' } }, hasChildren: true },
  { label: '类别', section: 'categories', to: { name: 'store-section', params: { section: 'categories' } }, hasChildren: true },
  { label: '畅玩方式', section: 'playstyles', to: { name: 'store-section', params: { section: 'playstyles' } }, hasChildren: true },
  { label: '特别栏目', section: 'specials', to: { name: 'store-section', params: { section: 'specials' } }, hasChildren: true }
];
const draft = reactive<GameQuery>({ ...props.modelValue });
let timer: number | undefined;

watch(
  () => props.modelValue,
  (value) => Object.assign(draft, value),
  { deep: true }
);

function isActive(item: NavItem): boolean {
  if (item.section === 'store') {
    return route.name === 'store';
  }

  return route.params.section === item.section;
}

function emitChange() {
  window.clearTimeout(timer);
  emit('update:modelValue', { ...draft, page: 1 });
}

function queueChange() {
  window.clearTimeout(timer);
  timer = window.setTimeout(emitChange, 220);
}
</script>

<style scoped>
.filter-bar {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(260px, 640px) auto;
  gap: 0.8rem;
  align-items: center;
  border: 1px solid rgba(102, 192, 244, 0.08);
  padding: 0.85rem 1rem;
  background: linear-gradient(90deg, rgba(27, 40, 56, 0.95), rgba(16, 34, 50, 0.95));
}

.filter-links,
.search-box,
.filter-controls {
  display: flex;
  align-items: center;
}

.filter-links,
.filter-controls {
  gap: 0.75rem;
  flex-wrap: wrap;
}

.filter-links a {
  display: inline-flex;
  min-height: 42px;
  align-items: center;
  gap: 0.2rem;
  border: 1px solid var(--steam-border);
  border-radius: 4px;
  padding: 0.52rem 0.88rem;
  color: var(--steam-text);
  background: rgba(13, 17, 24, 0.54);
  font-weight: 850;
  transition: border-color 160ms ease-out, background-color 160ms ease-out;
}

.filter-links a:hover {
  border-color: rgba(102, 192, 244, 0.52);
  background: rgba(22, 32, 45, 0.98);
}

.filter-links a.active {
  border-color: transparent;
  color: #07111c;
  background: var(--steam-blue);
}

.filter-links span {
  font-size: 0.78rem;
  line-height: 1;
}

.search-box {
  min-width: 0;
}

.search-box input {
  height: 42px;
  border-radius: 4px 0 0 4px;
  border-color: rgba(151, 170, 195, 0.24);
  background: #304050;
}

.search-box button {
  width: 48px;
  height: 42px;
  border: 0;
  border-radius: 0 4px 4px 0;
  color: #ffffff;
  background: var(--steam-blue-strong);
  cursor: pointer;
  font-size: 1.35rem;
  font-weight: 900;
}

.filter-controls label {
  display: grid;
  gap: 0.24rem;
}

.filter-controls span {
  color: var(--steam-muted);
  font-size: 0.72rem;
}

.filter-controls select {
  min-width: 136px;
  height: 42px;
  border: 1px solid rgba(151, 170, 195, 0.24);
  border-radius: 4px;
  color: var(--steam-text);
  background: #0d1118;
  font-weight: 800;
}

@media (prefers-reduced-motion: reduce) {
  .filter-links a {
    transition: none;
  }
}

@media (max-width: 1180px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}
</style>
