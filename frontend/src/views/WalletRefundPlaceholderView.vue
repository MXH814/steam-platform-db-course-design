<template>
  <section class="refund-placeholder">
    <h1>退款申请</h1>
    <nav class="breadcrumbs">
      <RouterLink to="/wallet/history">近期购买记录</RouterLink>
      <span>&gt;</span>
      <RouterLink :to="{ name: 'wallet-history-detail', params: { historyId } }">交易详情</RouterLink>
      <span>&gt;</span>
      <span>退款</span>
    </nav>

    <article class="placeholder-panel">
      <h2>退款功能暂未实现</h2>
      <p>这里是退款流程占位页面，当前不会提交退款申请，也不会调用后端退款接口。</p>
      <RouterLink class="back-link" :to="{ name: 'wallet-history-detail', params: { historyId } }">返回交易详情</RouterLink>
    </article>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { RouterLink, useRoute } from 'vue-router';

const route = useRoute();
const historyId = computed(() => String(route.params.historyId || ''));
</script>

<style scoped>
.refund-placeholder {
  display: grid;
  gap: 24px;
  color: #c7d5e0;
}

.refund-placeholder h1 {
  margin: 0;
  color: #ffffff;
  font-size: 2.2rem;
  font-weight: 500;
}

.breadcrumbs {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  color: #7f98ad;
  font-size: 0.98rem;
}

.breadcrumbs a {
  color: #66c0f4;
}

.placeholder-panel {
  width: min(760px, 100%);
  padding: 28px;
  background: rgba(25, 40, 56, 0.92);
}

.placeholder-panel h2 {
  margin: 0 0 12px;
  color: #66c0f4;
  font-size: 1.35rem;
}

.placeholder-panel p {
  margin: 0 0 22px;
}

.back-link {
  display: inline-flex;
  min-height: 40px;
  align-items: center;
  padding: 0 18px;
  color: #dcefb2;
  background: linear-gradient(180deg, #8fbc16, #5f8408);
  font-weight: 700;
}
</style>
