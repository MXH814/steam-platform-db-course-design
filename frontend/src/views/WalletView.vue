<template>
  <section class="steam-wallet-page">
    <header class="wallet-hero">
      <h1>为您的 Steam 钱包充值</h1>
    </header>

    <div class="wallet-intro">
      <div>
        <h2>为 {{ accountName }} 的钱包充值</h2>
        <p>您的 Steam 钱包资金可用于购买 Steam 上的游戏，也可在支持 Steam 交易的游戏内用来进行购买。</p>
        <p>下订单之前，您有机会检查您的订单。</p>
      </div>
    </div>

    <div class="wallet-layout">
      <main class="recharge-list" aria-label="推荐充值金额">
        <article v-for="option in rechargeOptions" :key="option" class="recharge-row">
          <div>
            <h3>充值 {{ money(option) }}</h3>
            <p v-if="option === 30">最低资金级别</p>
          </div>
          <div class="recharge-action">
            <span>{{ money(option) }}</span>
            <RouterLink class="steam-recharge-button" :to="{ name: 'wallet-recharge-checkout', query: { amount: option } }">充值</RouterLink>
          </div>
        </article>
      </main>

      <aside class="account-card">
        <header>您的 STEAM 帐户</header>
        <div class="account-balance">
          <span>当前钱包余额</span>
          <strong>{{ money(wallet?.availableBalance) }}</strong>
        </div>
        <dl class="account-balance-details">
          <div>
            <dt>冻结余额</dt>
            <dd>{{ money(wallet?.frozenBalance) }}</dd>
          </div>
          <div>
            <dt>总余额</dt>
            <dd>{{ money(wallet?.totalBalance) }}</dd>
          </div>
        </dl>
        <RouterLink class="account-link" to="/wallet/history">查看我的账户明细</RouterLink>
      </aside>
    </div>

    <p v-if="error" class="message error">{{ error }}</p>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { RouterLink } from 'vue-router';
import { getWallet, type WalletSummary } from '../api/coreApi';
import { getApiError } from '../api/http';
import { useAuthStore } from '../stores/auth';
import { money } from '../utils/format';

const auth = useAuthStore();
const wallet = ref<WalletSummary | null>(null);
const error = ref('');
const rechargeOptions = [30, 60, 150, 300, 600];
const accountName = computed(() => auth.currentUser?.account || '玩家');

async function loadWallet() {
  error.value = '';
  try {
    wallet.value = await getWallet();
  } catch (requestError) {
    error.value = getApiError(requestError);
  }
}

onMounted(loadWallet);
</script>

<style scoped>
.steam-wallet-page {
  display: grid;
  gap: 24px;
}

.wallet-hero {
  min-height: 120px;
  display: flex;
  align-items: flex-start;
  padding-top: 8px;
}

.wallet-hero h1 {
  margin: 0;
  color: #ffffff;
  font-size: 2.4rem;
  line-height: 1.15;
  text-shadow: 0 2px 18px rgba(0, 0, 0, 0.35);
}

.wallet-intro {
  color: #c7d5e0;
  font-size: 1rem;
}

.wallet-intro h2 {
  margin: 0 0 14px;
  color: #ffffff;
  font-size: 1.2rem;
}

.wallet-intro p {
  max-width: 980px;
  margin: 0 0 12px;
  font-weight: 650;
}

.wallet-layout {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 480px;
  gap: 28px;
  align-items: start;
}

.recharge-list {
  display: grid;
  gap: 20px;
}

.recharge-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: center;
  min-height: 92px;
  padding: 20px 18px 20px 26px;
  border-radius: 4px;
  background: linear-gradient(90deg, rgba(70, 92, 111, 0.98), rgba(87, 105, 122, 0.98));
}

.recharge-row h3,
.recharge-row p {
  margin: 0;
}

.recharge-row h3 {
  color: #ffffff;
  font-size: 1.35rem;
  font-weight: 800;
}

.recharge-row p {
  color: #c7d5e0;
  font-size: 0.98rem;
  font-weight: 700;
}

.recharge-action {
  display: grid;
  grid-template-columns: 130px 92px;
  align-items: center;
  overflow: hidden;
  border: 2px solid #000;
  border-radius: 3px;
  background: #05080c;
}

.recharge-action span {
  padding: 10px 16px;
  color: #d7e2ef;
  font-size: 1rem;
  font-weight: 800;
  text-align: right;
}

.steam-recharge-button {
  min-height: 44px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #dcefb2;
  background: linear-gradient(180deg, #8fbc16, #5f8408);
  font-size: 1rem;
  font-weight: 800;
}

.account-card {
  background: #0c141d;
}

.account-card header {
  padding: 10px 18px;
  color: #dfe6ef;
  background: #343f4b;
  font-size: 1rem;
  font-weight: 800;
  letter-spacing: 0.18em;
}

.account-balance {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 16px;
  align-items: end;
  padding: 42px 20px 12px;
}

.account-balance span {
  color: #66c0f4;
  font-weight: 800;
}

.account-balance strong {
  color: #c7d5e0;
  font-size: 1.8rem;
  font-weight: 400;
}

.account-balance-details {
  display: grid;
  gap: 8px;
  margin: 0 20px 18px;
  color: #8fa4b8;
  font-size: 0.88rem;
}

.account-balance-details div {
  display: flex;
  justify-content: space-between;
  gap: 12px;
}

.account-balance-details dt,
.account-balance-details dd {
  margin: 0;
}

.account-balance-details dd {
  color: #c7d5e0;
}

.account-link {
  display: block;
  margin: 0 20px 20px;
  padding: 10px 14px;
  color: #66c0f4;
  background: #1b3346;
  font-size: 0.95rem;
  font-weight: 800;
}

@media (max-width: 980px) {
  .wallet-layout,
  .recharge-row {
    grid-template-columns: 1fr;
  }

  .account-card {
    order: -1;
  }

  .recharge-action {
    width: min(100%, 260px);
    margin-top: 14px;
    justify-self: end;
  }
}
</style>
