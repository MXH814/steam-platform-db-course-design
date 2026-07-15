<template>
  <section class="game-checkout">
    <h1>复核并购买</h1>

    <p v-if="error" class="message error">{{ error }}</p>
    <div v-if="game" class="checkout-grid">
      <main class="review-panel">
        <header>
          <h2>{{ game.gameName }}</h2>
          <span>{{ game.developerName }}</span>
        </header>

        <dl class="review-lines">
          <div>
            <dt>游戏单价</dt>
            <dd>{{ money(game.basePrice) }}</dd>
          </div>
          <div v-if="discountAmount > 0">
            <dt>折扣</dt>
            <dd class="discount">-{{ Math.round((discountAmount / game.basePrice) * 100) }}% / -{{ money(discountAmount) }}</dd>
          </div>
          <div>
            <dt>合计</dt>
            <dd>{{ money(game.finalPrice) }}</dd>
          </div>
          <div>
            <dt>购入账户</dt>
            <dd>{{ accountName }}</dd>
          </div>
        </dl>

        <label class="payment-select">
          <span>支付方式</span>
          <select v-model="paymentMethod">
            <option value="STEAM_WALLET">Steam 钱包</option>
            <option value="WECHAT_PAY">微信支付</option>
            <option value="ALIPAY">支付宝</option>
            <option value="VISA">Visa</option>
            <option value="MASTERCARD">万事达卡</option>
          </select>
        </label>

        <div class="selected-method">
          <strong>{{ paymentLabel(paymentMethod) }}</strong>
          <span>{{ paymentMethod === 'STEAM_WALLET' ? '将从您的 Steam 钱包余额扣除。' : '课程演示模拟支付，点击支付后直接完成。' }}</span>
        </div>

        <p v-if="ownsGame" class="owned-note">您已经拥有这款游戏，无需重复购买。</p>

        <button class="pay-button" type="button" :disabled="busy || ownsGame" @click="pay">
          {{ busy ? '处理中...' : '支付' }}
        </button>
      </main>

      <aside class="account-card">
        <h2>购买摘要</h2>
        <p>{{ game.gameName }}</p>
        <strong>{{ money(game.finalPrice) }}</strong>
        <span>{{ paymentLabel(paymentMethod) }}</span>
      </aside>
    </div>
    <PageState v-else-if="loading" kind="loading" title="正在加载购买信息" />
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { buyGame, getLibrary, type PaymentMethod } from '../api/coreApi';
import { getGameDetail } from '../api/games';
import { getApiError } from '../api/http';
import type { GameDetail } from '../api/types';
import PageState from '../components/PageState.vue';
import { useAuthStore } from '../stores/auth';
import { money } from '../utils/format';

const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const game = ref<GameDetail | null>(null);
const paymentMethod = ref<PaymentMethod>('STEAM_WALLET');
const busy = ref(false);
const loading = ref(false);
const error = ref('');
const ownedGameIds = ref<Set<string>>(new Set());
const accountName = computed(() => auth.currentUser?.account || '玩家');
const gameId = computed(() => String(route.params.gameId || 'GAME_DST'));
const discountAmount = computed(() => Math.max(0, (game.value?.basePrice || 0) - (game.value?.finalPrice || 0)));
const ownsGame = computed(() => ownedGameIds.value.has(gameId.value));

function paymentLabel(method: PaymentMethod) {
  const labels: Record<PaymentMethod, string> = {
    STEAM_WALLET: 'Steam 钱包',
    WECHAT_PAY: '微信支付',
    ALIPAY: '支付宝',
    VISA: 'Visa',
    MASTERCARD: '万事达卡'
  };
  return labels[method];
}

async function load() {
  loading.value = true;
  error.value = '';
  try {
    const [detail, library] = await Promise.all([
      getGameDetail(gameId.value),
      getLibrary().catch(() => [])
    ]);
    game.value = detail;
    ownedGameIds.value = new Set(library.map(item => item.gameId));
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    loading.value = false;
  }
}

async function pay() {
  if (!game.value || ownsGame.value) return;
  busy.value = true;
  error.value = '';
  try {
    await buyGame(game.value.gameId, paymentMethod.value);
    router.push('/wallet/history');
  } catch (requestError) {
    error.value = purchaseErrorMessage(requestError);
  } finally {
    busy.value = false;
  }
}

function purchaseErrorMessage(requestError: unknown) {
  const message = getApiError(requestError);
  if (message.includes('GAME_ALREADY_OWNED') || message.includes('ORA-1')) {
    return '您已经拥有这款游戏，无需重复购买。';
  }

  return message;
}

onMounted(load);
</script>

<style scoped>
.game-checkout {
  display: grid;
  gap: 24px;
}

.game-checkout h1 {
  margin: 0;
  font-size: 2.25rem;
  line-height: 1.15;
}

.checkout-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 360px;
  gap: 28px;
}

.review-panel,
.account-card {
  background: rgba(12, 20, 29, 0.96);
}

.review-panel {
  display: grid;
  gap: 22px;
  padding: 28px;
}

.review-panel header h2,
.review-panel header span,
.account-card h2,
.account-card p {
  margin: 0;
}

.review-panel header span,
.selected-method span,
.account-card span {
  color: #8fa4bd;
}

.review-lines {
  display: grid;
  gap: 12px;
  margin: 0;
}

.review-lines div {
  display: grid;
  grid-template-columns: 160px minmax(0, 1fr);
  padding: 12px 14px;
  background: #182636;
}

.review-lines dd {
  margin: 0;
  font-size: 1rem;
  text-align: right;
}

.discount {
  color: #b8f36f;
}

.payment-select {
  max-width: 420px;
}

.payment-select select {
  height: 50px;
  color: #ffffff;
  background: #67bce8;
  font-size: 1rem;
  font-weight: 800;
}

.selected-method {
  display: grid;
  gap: 4px;
  padding: 16px;
  background: #4d7185;
}

.pay-button {
  justify-self: end;
  min-width: 110px;
  min-height: 48px;
  border: 0;
  border-radius: 3px;
  color: #dcefb2;
  background: linear-gradient(180deg, #8fbc16, #5f8408);
  font-size: 1rem;
  font-weight: 900;
  cursor: pointer;
}

.pay-button:disabled {
  cursor: not-allowed;
  filter: grayscale(0.35);
  opacity: 0.65;
}

.account-card {
  display: grid;
  gap: 12px;
  align-content: start;
  padding: 24px;
}

.account-card strong {
  color: #ffffff;
  font-size: 1.5rem;
}

.owned-note {
  margin: 0;
  color: #ffb3b3;
}

@media (max-width: 900px) {
  .checkout-grid,
  .review-lines div {
    grid-template-columns: 1fr;
  }

  .review-lines dd {
    text-align: left;
  }
}
</style>
