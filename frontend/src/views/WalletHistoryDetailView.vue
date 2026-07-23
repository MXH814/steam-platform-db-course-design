<template>
  <section class="support-page">
    <h1>Steam 客服</h1>
    <nav class="breadcrumbs">
      <RouterLink to="/wallet/history">主页</RouterLink>
      <span>&gt;</span>
      <RouterLink to="/wallet/history">近期购买记录</RouterLink>
      <span>&gt;</span>
      <span>{{ entry?.itemName || '交易详情' }}</span>
    </nav>

    <p v-if="error" class="message error">{{ error }}</p>
    <PageState v-if="loading" kind="loading" title="正在加载交易详情" />

    <template v-else-if="entry">
      <article class="detail-panel">
        <p>交易号：{{ transactionNo }}</p>
        <p v-if="entry.walletTransactionId">钱包流水号：{{ entry.walletTransactionId }}</p>
        <p>{{ methodLabel }}：{{ paymentLabel(entry.paymentMethod) }}</p>
        <p>{{ dateLabel }}：{{ detailDate(entry.createTime) }}</p>
        <p v-if="entry.orderStatus || entry.paymentStatus">订单状态：{{ entry.orderStatus || '-' }} / {{ entry.paymentStatus || '-' }}</p>
        <p v-if="entry.refundStatus">退款状态：{{ entry.refundStatus }}</p>
        <p v-if="entry.walletChange !== null">钱包变更：{{ signedMoney(entry.walletChange) }}</p>
        <p v-if="entry.walletBalanceAfter !== null">变更后余额：{{ money(entry.walletBalanceAfter) }}</p>

        <p class="item-line">{{ entry.itemName }} - {{ money(entry.totalAmount) }}</p>

        <dl class="amount-lines">
          <div>
            <dt>小计</dt>
            <dd>{{ money(entry.originalPrice) }}</dd>
          </div>
          <div v-if="hasDiscount">
            <dt>折扣</dt>
            <dd>{{ money(-entry.discountAmount) }}（-{{ Math.round(entry.discountRate * 100) }}%）</dd>
          </div>
          <div>
            <dt>总计</dt>
            <dd>{{ money(entry.totalAmount) }}</dd>
          </div>
        </dl>
      </article>

      <section v-if="canRequestRefund" class="support-actions">
        <h2>您对这次购买有什么问题？</h2>
        <RouterLink class="refund-link" :to="{ name: 'wallet-history-refund', params: { historyId: entry.historyId } }">
          <span>我想要退款</span>
          <span aria-hidden="true">▶</span>
        </RouterLink>
      </section>
    </template>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { RouterLink, useRoute } from 'vue-router';
import { getWalletHistoryEntry, type WalletHistoryEntry } from '../api/coreApi';
import { getApiError } from '../api/http';
import PageState from '../components/PageState.vue';
import { money } from '../utils/format';

const route = useRoute();
const entry = ref<WalletHistoryEntry | null>(null);
const loading = ref(false);
const error = ref('');

const hasDiscount = computed(() => Boolean(entry.value && entry.value.discountAmount > 0 && entry.value.originalPrice > entry.value.totalAmount));
const transactionNo = computed(() => entry.value?.orderId || entry.value?.refundId || entry.value?.walletTransactionId || entry.value?.historyId || '-');
const dateLabel = computed(() => {
  if (entry.value?.sourceType === 'RECHARGE') return '充值日期';
  if (entry.value?.sourceType === 'REFUND') return '退款日期';
  if (entry.value?.sourceType === 'CDKEY_REDEEM') return '兑换日期';
  if (entry.value?.sourceType === 'FREE_CLAIM' || entry.value?.sourceType === 'GIFT' || entry.value?.sourceType === 'LIBRARY_IMPORT') return '入库日期';
  return '购买日期';
});
const methodLabel = computed(() => entry.value?.sourceType === 'CDKEY_REDEEM' || entry.value?.sourceType === 'FREE_CLAIM' || entry.value?.sourceType === 'GIFT' || entry.value?.sourceType === 'LIBRARY_IMPORT' ? '获取方式' : '支付方式');
const canRequestRefund = computed(() => Boolean(entry.value && entry.value.sourceType === 'BUY_GAME' && entry.value.orderId && entry.value.totalAmount > 0));

function paymentLabel(method: string) {
  const labels: Record<string, string> = {
    STEAM_WALLET: 'Steam 钱包',
    WECHAT_PAY: '微信支付',
    ALIPAY: '支付宝',
    VISA: 'Visa',
    MASTERCARD: '万事达卡',
    SIMULATED_EXTERNAL: '模拟外部支付',
    CDKEY_REDEEM: 'CDKey 兑换',
    FREE_CLAIM: '免费入库',
    GIFT: '礼物入库',
    LIBRARY_IMPORT: '游戏入库'
  };
  return labels[method] || method;
}

function signedMoney(value: number) {
  return `${value > 0 ? '+' : ''}${money(value)}`;
}

function detailDate(value: string) {
  return new Date(value).toLocaleString('zh-CN', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
}

async function load() {
  loading.value = true;
  error.value = '';
  try {
    entry.value = await getWalletHistoryEntry(String(route.params.historyId || ''));
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    loading.value = false;
  }
}

onMounted(load);
</script>

<style scoped>
.support-page {
  display: grid;
  gap: 24px;
  color: #c7d5e0;
}

.support-page h1 {
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

.detail-panel {
  padding: 26px 28px;
  background: rgba(25, 40, 56, 0.92);
  color: #8fa8ba;
  font-size: 1rem;
}

.detail-panel p {
  margin: 0 0 10px;
}

.item-line {
  margin-top: 24px !important;
  color: #66c0f4;
  font-size: 1.05rem;
}

.amount-lines {
  width: min(320px, 100%);
  margin: 22px 0 0;
  color: #66c0f4;
  font-size: 1.05rem;
}

.amount-lines div {
  display: grid;
  grid-template-columns: 80px 1fr;
}

.amount-lines dt,
.amount-lines dd {
  margin: 0;
}

.amount-lines div:last-child {
  margin-top: 6px;
  padding-top: 8px;
  border-top: 1px solid #8fa8ba;
}

.support-actions {
  display: grid;
  gap: 18px;
}

.support-actions h2 {
  margin: 0;
  color: #66c0f4;
  font-size: 1.35rem;
  font-weight: 400;
}

.refund-link {
  width: min(760px, 100%);
  min-height: 66px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
  color: #ffffff;
  background: #4d6177;
  font-size: 1.08rem;
}
</style>
