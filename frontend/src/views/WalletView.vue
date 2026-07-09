<template>
  <section class="view-stack">
    <div class="section-heading">
      <p class="eyebrow">Wallet</p>
      <h1>钱包与资金流水</h1>
    </div>

    <div class="metric-grid">
      <article class="metric-card">
        <span>可用余额</span>
        <strong>{{ money(wallet?.availableBalance) }}</strong>
      </article>
      <article class="metric-card">
        <span>冻结余额</span>
        <strong>{{ money(wallet?.frozenBalance) }}</strong>
      </article>
      <article class="metric-card">
        <span>总余额</span>
        <strong>{{ money(wallet?.totalBalance) }}</strong>
      </article>
    </div>

    <form class="tool-panel" @submit.prevent="recharge">
      <label>
        <span>充值金额</span>
        <input v-model.number="amount" type="number" min="0.01" max="99999.99" step="0.01" />
      </label>
      <button class="primary-button" type="submit" :disabled="loading">充值</button>
    </form>

    <p v-if="message" class="message success">{{ message }}</p>
    <p v-if="error" class="message error">{{ error }}</p>

    <section class="data-panel">
      <header class="panel-header">
        <h2>最近流水</h2>
        <button class="ghost-button" type="button" @click="load">刷新</button>
      </header>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>类型</th>
              <th>方向</th>
              <th>金额</th>
              <th>前余额</th>
              <th>后余额</th>
              <th>时间</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="txn in transactions" :key="txn.txnId">
              <td>{{ txn.bizType }}</td>
              <td>{{ txn.fundsDirection }}</td>
              <td>{{ money(txn.amount) }}</td>
              <td>{{ money(txn.availBalBefore) }}</td>
              <td>{{ money(txn.availBalAfter) }}</td>
              <td>{{ dateTime(txn.createTime) }}</td>
            </tr>
            <tr v-if="!transactions.length">
              <td colspan="6">暂无流水</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { getWallet, getWalletTransactions, rechargeWallet, type WalletSummary, type WalletTransactionEntry } from '../api/coreApi';
import { getApiError } from '../api/http';
import { dateTime, money } from '../utils/format';

const wallet = ref<WalletSummary | null>(null);
const transactions = ref<WalletTransactionEntry[]>([]);
const amount = ref(50);
const loading = ref(false);
const error = ref('');
const message = ref('');

async function load() {
  error.value = '';
  try {
    const [walletResult, transactionPage] = await Promise.all([
      getWallet(),
      getWalletTransactions(1, 20)
    ]);
    wallet.value = walletResult;
    transactions.value = transactionPage.items;
  } catch (requestError) {
    error.value = getApiError(requestError);
  }
}

async function recharge() {
  loading.value = true;
  message.value = '';
  error.value = '';
  try {
    const result = await rechargeWallet(amount.value);
    message.value = `充值成功，当前可用余额 ${money(result.availableBalance)}`;
    await load();
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    loading.value = false;
  }
}

onMounted(load);
</script>
