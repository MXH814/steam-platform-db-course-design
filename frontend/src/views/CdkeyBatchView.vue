<template>
  <section class="view-stack">
    <div class="section-heading">
      <p class="eyebrow">Developer</p>
      <h1>CDKey 批次</h1>
    </div>

    <form class="form-panel wide" @submit.prevent="createBatch">
      <label>
        <span>游戏</span>
        <select v-model="gameId">
          <option value="GAME_DST">Don't Starve Together / 饥荒联机版</option>
        </select>
      </label>
      <label>
        <span>批次号</span>
        <input v-model.trim="batchNo" required />
      </label>
      <label>
        <span>生效时间</span>
        <input v-model="validFrom" type="datetime-local" required />
      </label>
      <label>
        <span>过期时间</span>
        <input v-model="expireTime" type="datetime-local" required />
      </label>
      <label>
        <span>数量</span>
        <input v-model.number="quantity" type="number" min="1" max="50" required />
      </label>
      <button class="primary-button" type="submit">生成 CDKey</button>
    </form>

    <p v-if="error" class="message error">{{ error }}</p>

    <section v-if="batch" class="data-panel">
      <header class="panel-header">
        <h2>本次生成结果</h2>
        <span class="pill">明文只展示一次</span>
      </header>
      <div class="key-grid">
        <code v-for="key in batch.plaintextKeys" :key="key">{{ key }}</code>
      </div>
    </section>
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { createCdkeyBatch, type CdkeyBatchSummary } from '../api/coreApi';
import { getApiError } from '../api/http';

const gameId = ref('GAME_DST');
const batchNo = ref(`DST-${new Date().toISOString().slice(0, 10).replace(/-/g, '')}`);
const now = new Date();
const nextMonth = new Date(now.getTime() + 30 * 24 * 60 * 60 * 1000);
const validFrom = ref(toLocalInput(now));
const expireTime = ref(toLocalInput(nextMonth));
const quantity = ref(3);
const batch = ref<CdkeyBatchSummary | null>(null);
const error = ref('');

function toLocalInput(date: Date): string {
  return new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 16);
}

async function createBatch() {
  error.value = '';
  batch.value = null;
  try {
    batch.value = await createCdkeyBatch(
      gameId.value,
      batchNo.value,
      new Date(validFrom.value).toISOString(),
      new Date(expireTime.value).toISOString(),
      quantity.value
    );
  } catch (requestError) {
    error.value = getApiError(requestError);
  }
}
</script>
