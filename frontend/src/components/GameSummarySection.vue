<template>
  <section class="summary-section">
    <header>
      <h2>{{ title }}</h2>
      <span v-if="meta">{{ meta }}</span>
    </header>
    <PageState v-if="loading" kind="loading" title="正在加载" />
    <PageState v-else-if="error" kind="error" title="模块加载失败" :message="error" />
    <PageState v-else-if="empty" kind="empty" title="暂无数据" />
    <div v-else class="summary-body">
      <slot />
    </div>
  </section>
</template>

<script setup lang="ts">
import PageState from './PageState.vue';

withDefaults(
  defineProps<{
    title: string;
    meta?: string;
    loading?: boolean;
    error?: string;
    empty?: boolean;
  }>(),
  {
    meta: '',
    loading: false,
    error: '',
    empty: false
  }
);
</script>

<style scoped>
.summary-section {
  display: grid;
  gap: 0.8rem;
  border: 1px solid var(--steam-border);
  border-radius: 6px;
  padding: 1rem;
  background: rgba(22, 32, 45, 0.84);
}

.summary-section header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.8rem;
  border-bottom: 1px solid rgba(151, 170, 195, 0.12);
  padding-bottom: 0.6rem;
}

.summary-section h2 {
  margin: 0;
  font-size: 1.02rem;
  text-wrap: balance;
}

.summary-section header span {
  color: var(--steam-blue);
  font-size: 0.82rem;
  font-weight: 800;
}

.summary-body {
  min-width: 0;
}
</style>
