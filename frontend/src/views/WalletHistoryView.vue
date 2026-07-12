<template>
  <section class="history-page">
    <header class="history-hero">
      <h1>{{ accountName }} 的消费历史记录</h1>
    </header>

    <p class="help-line">交易出现问题？在下方选择出现问题的交易以获取详情。</p>

    <p v-if="error" class="message error">{{ error }}</p>
    <div class="history-table-wrap">
      <table class="history-table">
        <thead>
          <tr>
            <th>日期</th>
            <th>物品</th>
            <th>类型</th>
            <th>价格</th>
            <th>税额</th>
            <th>运费</th>
            <th>总计</th>
            <th>钱包<br />变更</th>
            <th>余额</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="item in rows"
            :key="item.historyId"
            class="history-row"
            tabindex="0"
            @click="openDetail(item.historyId)"
            @keyup.enter="openDetail(item.historyId)"
          >
            <td>{{ historyDate(item.createTime) }}</td>
            <td>
              <strong>{{ item.itemName }}</strong>
              <small v-if="item.orderId">订单 {{ item.orderId }}</small>
              <small v-if="statusLine(item)">{{ statusLine(item) }}</small>
            </td>
            <td>
              <strong>{{ typeLabel(item.sourceType) }}</strong>
              <small>{{ paymentLabel(item.paymentMethod) }}</small>
            </td>
            <td>
              <div v-if="hasDiscount(item)" class="discount-price">
                <span class="discount-badge">-{{ Math.round(item.discountRate * 100) }}%</span>
                <span class="price-stack">
                  <s>{{ money(item.originalPrice) }}</s>
                  <strong>{{ money(item.totalAmount) }}</strong>
                </span>
              </div>
              <span v-else>{{ money(item.totalAmount) }}</span>
            </td>
            <td>-</td>
            <td>-</td>
            <td>{{ money(item.totalAmount) }}</td>
            <td :class="{ credit: Number(item.walletChange || 0) > 0, debit: Number(item.walletChange || 0) < 0 }">
              {{ item.walletChange === null ? '-' : signedMoney(item.walletChange) }}
            </td>
            <td>{{ item.walletBalanceAfter === null ? '-' : money(item.walletBalanceAfter) }}</td>
          </tr>
          <tr v-if="!loading && rows.length === 0">
            <td colspan="9" class="empty">暂无消费历史记录</td>
          </tr>
          <tr v-if="loading">
            <td colspan="9" class="empty">正在加载消费历史...</td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="history-actions">
      <RouterLink class="ghost-button" to="/wallet">返回钱包</RouterLink>
      <button class="ghost-button" type="button" :disabled="loading" @click="load">刷新</button>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { RouterLink, useRouter } from 'vue-router';
import { getWalletHistory, type WalletHistoryEntry } from '../api/coreApi';
import { getApiError } from '../api/http';
import { useAuthStore } from '../stores/auth';
import { money } from '../utils/format';

const auth = useAuthStore();
const router = useRouter();
const rows = ref<WalletHistoryEntry[]>([]);
const loading = ref(false);
const error = ref('');
const accountName = computed(() => auth.currentUser?.account || '玩家');

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

function typeLabel(type: string) {
  if (type === 'BUY_GAME') return '购买';
  if (type === 'RECHARGE') return '充值';
  if (type === 'REFUND') return '退款';
  if (type === 'CDKEY_REDEEM') return '兑换';
  if (type === 'FREE_CLAIM') return '免费入库';
  if (type === 'GIFT') return '礼物';
  if (type === 'LIBRARY_IMPORT') return '入库';
  return type;
}

function statusLine(item: WalletHistoryEntry) {
  if (item.refundStatus) return `退款状态 ${item.refundStatus}`;
  if (item.orderStatus || item.paymentStatus) return `${item.orderStatus || '-'} / ${item.paymentStatus || '-'}`;
  return '';
}

function historyDate(value: string) {
  return new Date(value).toLocaleDateString('zh-CN', { year: 'numeric', month: 'long', day: 'numeric' });
}

function hasDiscount(item: WalletHistoryEntry) {
  return item.discountAmount > 0 && item.originalPrice > item.totalAmount;
}

function signedMoney(value: number) {
  return `${value > 0 ? '+' : ''}${money(value)}`;
}

function openDetail(historyId: string) {
  router.push({ name: 'wallet-history-detail', params: { historyId } });
}

async function load() {
  loading.value = true;
  error.value = '';
  try {
    const page = await getWalletHistory(1, 80);
    rows.value = page.items;
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    loading.value = false;
  }
}

onMounted(load);
</script>

<style scoped>
.history-page {
  display: grid;
  gap: 22px;
}

.history-hero {
  min-height: 72px;
  display: flex;
  align-items: flex-start;
}

.history-hero h1 {
  margin: 0;
  color: #ffffff;
  font-size: 2.25rem;
  line-height: 1.15;
}

.help-line {
  margin: 0;
  color: #66c0f4;
  font-size: 0.95rem;
  font-weight: 800;
}

.history-table-wrap {
  overflow-x: auto;
  border: 1px solid #2d4a65;
}

.history-table {
  min-width: 1220px;
  border-collapse: collapse;
  background: rgba(21, 34, 48, 0.96);
}

.history-table th {
  padding: 8px 14px;
  color: #d8f2ff;
  background: #109dcc;
  font-size: 0.95rem;
  font-weight: 900;
  text-align: left;
}

.history-table th:nth-last-child(-n + 2),
.history-table td:nth-last-child(-n + 2) {
  border-left: 1px solid #2d4a65;
}

.history-table td {
  min-height: 76px;
  padding: 14px;
  border-bottom: 1px solid rgba(82, 117, 150, 0.2);
  color: #c7d5e0;
  font-size: 0.92rem;
  vertical-align: top;
}

.history-row {
  cursor: pointer;
}

.history-row:hover,
.history-row:focus {
  background: rgba(102, 192, 244, 0.08);
  outline: none;
}

.history-table strong {
  display: block;
  color: #c7d5e0;
  font-size: 0.96rem;
}

.history-table small {
  display: block;
  margin-top: 3px;
  color: #7699ad;
}

.discount-price {
  display: flex;
  align-items: stretch;
  gap: 0;
}

.discount-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 58px;
  padding: 0 8px;
  color: #b8f36f;
  background: #4c6b22;
  font-size: 0.95rem;
  font-weight: 900;
}

.price-stack {
  display: grid;
  min-width: 86px;
  padding-left: 8px;
}

.price-stack s {
  color: #8f98a0;
}

.price-stack strong {
  color: #b8f36f;
}

.credit {
  color: #b8f36f !important;
}

.debit {
  color: #c7d5e0 !important;
}

.empty {
  text-align: center;
}

.history-actions {
  display: flex;
  gap: 10px;
}
</style>
