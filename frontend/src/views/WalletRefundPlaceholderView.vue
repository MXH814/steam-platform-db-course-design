<template>
  <section class="refund-flow">
    <h1>Game Deck 客服</h1>
    <nav class="breadcrumbs"><RouterLink to="/wallet/history">近期购买记录</RouterLink><span>&gt;</span><RouterLink :to="{ name: 'wallet-history-detail', params: { historyId } }">交易详情</RouterLink><span>&gt;</span><span>申请退款</span></nav>

    <p v-if="error" class="flow-message error">{{ error }}</p>
    <article v-if="loading" class="refund-panel">正在核对交易与退款资格...</article>
    <article v-else-if="entry" class="refund-panel">
      <header><span>退款申请</span><h2>{{ entry.itemName }}</h2></header>
      <dl><div><dt>订单号</dt><dd>{{ entry.orderId }}</dd></div><div><dt>可申请金额</dt><dd>{{ money(entry.totalAmount) }}</dd></div><div><dt>支付方式</dt><dd>{{ entry.paymentMethod }}</dd></div></dl>
      <form @submit.prevent="submit">
        <label><span>退款原因</span><select v-model="reasonPreset"><option>游戏体验与预期不符</option><option>误购了此产品</option><option>技术问题导致无法游玩</option><option>其他原因</option></select></label>
        <label><span>补充说明</span><textarea v-model.trim="details" rows="5" maxlength="500" placeholder="请说明申请退款的原因" /></label>
        <label class="confirm-row"><input v-model="confirmed" type="checkbox" /> 我确认提交后该申请将进入管理员审核流程</label>
        <div class="refund-actions"><RouterLink :to="{ name: 'wallet-history-detail', params: { historyId } }">取消</RouterLink><button type="submit" :disabled="submitting || !confirmed || !entry.orderId">{{ submitting ? '正在提交...' : '提交退款申请' }}</button></div>
      </form>
    </article>
    <article v-if="createdRefundId" class="refund-success"><strong>退款申请已提交</strong><span>申请编号：{{ createdRefundId }}</span><RouterLink to="/refunds">查看我的退款申请</RouterLink></article>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { RouterLink, useRoute } from 'vue-router';
import { createRefund, getWalletHistoryEntry, type WalletHistoryEntry } from '../api/coreApi';
import { getApiError } from '../api/http';
import { money } from '../utils/format';

const route = useRoute();
const historyId = computed(() => String(route.params.historyId || ''));
const entry = ref<WalletHistoryEntry | null>(null);
const loading = ref(false);
const submitting = ref(false);
const confirmed = ref(false);
const reasonPreset = ref('游戏体验与预期不符');
const details = ref('');
const error = ref('');
const createdRefundId = ref('');

async function load() {
  loading.value = true;
  error.value = '';
  try {
    entry.value = await getWalletHistoryEntry(historyId.value);
    if (!entry.value.orderId || entry.value.sourceType !== 'BUY_GAME') error.value = '这笔交易不属于可退款的付费游戏订单。';
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    loading.value = false;
  }
}

async function submit() {
  if (!entry.value?.orderId || !confirmed.value) return;
  submitting.value = true;
  error.value = '';
  try {
    const reason = details.value ? `${reasonPreset.value}：${details.value}` : reasonPreset.value;
    const refund = await createRefund(entry.value.orderId, reason, entry.value.orderDetailId, undefined);
    createdRefundId.value = refund.refundId;
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    submitting.value = false;
  }
}

onMounted(load);
</script>

<style scoped>
.refund-flow { display: grid; gap: 20px; color: #c7d5e0; }
.refund-flow h1 { margin: 0; color: #fff; font-size: 2rem; font-weight: 500; }
.breadcrumbs { display: flex; flex-wrap: wrap; gap: 8px; color: #7f98ad; }
.breadcrumbs a { color: #66c0f4; }
.refund-panel { width: min(760px, 100%); padding: 24px; background: #1b2a39; }
.refund-panel header { margin-bottom: 18px; }
.refund-panel header span { color: #66c0f4; font-size: 0.78rem; text-transform: uppercase; }
.refund-panel h2 { margin: 3px 0 0; }
.refund-panel dl { display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; margin: 0 0 20px; padding: 14px; background: #12202d; }
.refund-panel form { display: grid; gap: 14px; }
.confirm-row { display: flex; grid-template-columns: none; align-items: center; gap: 8px; font-weight: 400; }
.confirm-row input { width: 17px; height: 17px; }
.refund-actions { display: flex; justify-content: flex-end; gap: 8px; }
.refund-actions a, .refund-actions button, .refund-success a { display: inline-flex; min-height: 40px; align-items: center; border: 0; padding: 0 16px; color: #fff; background: #3c556b; cursor: pointer; }
.refund-actions button, .refund-success a { background: linear-gradient(#8fbc16, #5f8408); }
.flow-message, .refund-success { width: min(760px, 100%); padding: 14px; background: #17232e; }
.flow-message.error { border-left: 3px solid #e66; color: #ffb4b4; }
.refund-success { display: flex; align-items: center; gap: 14px; border-left: 3px solid #8fbc16; }
.refund-success span { color: #91a3b2; }
.refund-success a { margin-left: auto; }
@media (max-width: 640px) { .refund-panel dl { grid-template-columns: 1fr; } .refund-actions, .refund-success { align-items: stretch; flex-direction: column; } .refund-success a { margin-left: 0; } }
</style>
