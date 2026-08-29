<template>
  <section class="not-found-view" aria-labelledby="not-found-title">
    <p class="not-found-code">404</p>
    <h1 id="not-found-title">找不到这个页面</h1>
    <p>链接可能已经失效，或者地址输入有误。</p>
    <div class="not-found-actions">
      <RouterLink class="primary-button" to="/store">返回商店</RouterLink>
      <button type="button" class="secondary-button" @click="router.back()">返回上一页</button>
    </div>
  </section>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';

const router = useRouter();
</script>

<style scoped>
.not-found-view {
  width: min(760px, calc(100% - 32px));
  min-height: 480px;
  margin: 48px auto;
  padding: 72px 32px;
  text-align: center;
  color: #d6d7d8;
  background: linear-gradient(180deg, #1b2838 0%, #101822 100%);
  border: 1px solid #2a475e;
  border-radius: 4px;
}

.not-found-code {
  margin: 0;
  color: #66c0f4;
  font-size: 88px;
  font-weight: 700;
  line-height: 1;
}

h1 {
  margin: 20px 0 12px;
  font-size: 32px;
}

.not-found-view > p:not(.not-found-code) {
  margin: 0;
  color: #8f98a0;
}

.not-found-actions {
  display: flex;
  justify-content: center;
  gap: 12px;
  margin-top: 32px;
}

.not-found-actions a,
.not-found-actions button {
  min-width: 132px;
  text-decoration: none;
}

@media (max-width: 560px) {
  .not-found-view {
    min-height: 420px;
    margin-top: 20px;
    padding: 56px 20px;
  }

  .not-found-code {
    font-size: 68px;
  }

  .not-found-actions {
    flex-direction: column;
  }
}
</style>
