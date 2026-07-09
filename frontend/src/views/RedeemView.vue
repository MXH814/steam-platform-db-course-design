<template>
  <section class="view-stack">
    <div class="section-heading">
      <p class="eyebrow">CDKey</p>
      <h1>兑换游戏激活码</h1>
    </div>

    <section class="redeem-hero">
      <div>
        <span class="pill">DST 演示链路</span>
        <h2>输入开发商生成的 CDKey，直接写入玩家游戏库。</h2>
        <p>兑换成功后可以在“我的游戏库”看到对应游戏，后续退款、游玩时长和订单演示都能继续串起来。</p>
      </div>
      <form class="redeem-form" @submit.prevent="redeem">
        <label>
          <span>CDKey</span>
          <input v-model.trim="cdkey" placeholder="例如 DST-20260709-XXXX" autocomplete="off" required />
        </label>
        <button class="primary-button" type="submit" :disabled="submitting">
          {{ submitting ? '兑换中...' : '立即兑换' }}
        </button>
      </form>
    </section>

    <p v-if="message" class="message success">{{ message }}</p>
    <p v-if="error" class="message error">{{ error }}</p>

    <section v-if="result" class="details-panel">
      <dl>
        <div>
          <dt>结果</dt>
          <dd>{{ result.result }}</dd>
        </div>
        <div>
          <dt>游戏</dt>
          <dd>{{ result.gameId || '-' }}</dd>
        </div>
        <div>
          <dt>库记录</dt>
          <dd>{{ result.libraryId || '-' }}</dd>
        </div>
        <div>
          <dt>说明</dt>
          <dd>{{ result.message }}</dd>
        </div>
      </dl>
    </section>
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { redeemCdkey, type CdkeyRedeemResult } from '../api/coreApi';
import { getApiError } from '../api/http';

const cdkey = ref('');
const submitting = ref(false);
const result = ref<CdkeyRedeemResult | null>(null);
const error = ref('');
const message = ref('');

async function redeem() {
  submitting.value = true;
  result.value = null;
  error.value = '';
  message.value = '';

  try {
    result.value = await redeemCdkey(cdkey.value);
    message.value = result.value.message || 'CDKey 兑换成功';
    cdkey.value = '';
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    submitting.value = false;
  }
}
</script>
