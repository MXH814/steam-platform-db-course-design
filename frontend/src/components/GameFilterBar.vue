<template>
  <section class="filter-bar" aria-label="商店筛选">
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
import type { GameQuery } from '../api/types';

const props = defineProps<{
  modelValue: GameQuery;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: GameQuery];
}>();

const draft = reactive<GameQuery>({ ...props.modelValue });
let timer: number | undefined;

watch(
  () => props.modelValue,
  (value) => Object.assign(draft, value),
  { deep: true }
);

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
  display: flex;
  gap: 0.8rem;
  align-items: center;
  justify-content: flex-end;
  padding: 0.3rem 0;
  background: transparent;
}

.search-box,
.filter-controls {
  display: flex;
  align-items: center;
}

.filter-controls {
  gap: 0.75rem;
  flex-wrap: wrap;
}

.search-box {
  width: min(420px, 100%);
  min-width: 0;
}

.search-box input {
  height: 34px;
  border-radius: 4px 0 0 4px;
  border-color: rgba(151, 170, 195, 0.24);
  background: #304050;
}

.search-box button {
  width: 40px;
  height: 34px;
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
  min-width: 120px;
  height: 34px;
  border: 1px solid rgba(151, 170, 195, 0.24);
  border-radius: 4px;
  color: var(--steam-text);
  background: #0d1118;
  font-weight: 800;
  padding: 0.25rem 0.55rem;
}

@media (prefers-reduced-motion: reduce) {
  .filter-links a {
    transition: none;
  }
}

@media (max-width: 1180px) {
  .filter-bar {
    align-items: stretch;
    flex-direction: column;
  }
}
</style>
