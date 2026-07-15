<template>
  <section class="payment-page">
    <h1>支付方式</h1>

    <div class="payment-layout">
      <main class="payment-panel">
        <label class="payment-select">
          <span>请选择一种支付方式。</span>
          <select v-model="paymentMethod">
            <option value="VISA">Visa</option>
            <option value="MASTERCARD">万事达卡</option>
            <option value="ALIPAY">支付宝</option>
            <option value="WECHAT_PAY">微信支付</option>
          </select>
        </label>

        <div class="payment-notice">
          <strong>{{ methodLabel(paymentMethod) }}</strong>
          <span>{{ methodHelp(paymentMethod) }}</span>
        </div>

        <dl class="checkout-summary">
          <div>
            <dt>充值金额</dt>
            <dd>{{ amountError ? '-' : money(amount) }}</dd>
          </div>
          <div>
            <dt>支付方式</dt>
            <dd>{{ methodLabel(paymentMethod) }}</dd>
          </div>
        </dl>

        <p>下订单之前，您有机会检查您的订单。</p>
        <p v-if="amountError" class="message error">{{ amountError }}</p>
        <p v-if="message" class="message success">{{ message }}</p>
        <p v-if="error" class="message error">{{ error }}</p>

        <button class="continue-button" type="button" :disabled="busy || Boolean(amountError)" @click="submit">
          {{ busy ? '处理中...' : '继续' }}
        </button>
      </main>

      <aside class="accepted-card">
        <h2>支付方式</h2>
        <p>我们接受以下安全支付方式：</p>
        <div class="payment-logos">
          <span>VISA</span>
          <span>Mastercard</span>
          <span>Alipay</span>
          <span>WeChat</span>
        </div>
      </aside>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { rechargeWallet, type PaymentMethod } from '../api/coreApi';
import { getApiError } from '../api/http';
import { money } from '../utils/format';

type ExternalPaymentMethod = Exclude<PaymentMethod, 'STEAM_WALLET'>;

const route = useRoute();
const router = useRouter();
const rawAmount = computed(() => {
  const value = route.query.amount;
  return String(Array.isArray(value) ? value[0] : value ?? '30').trim();
});
const amount = computed(() => Number(rawAmount.value));
const amountError = computed(() => {
  if (!/^\d+(\.\d{1,2})?$/.test(rawAmount.value)) {
    return '充值金额必须是最多两位小数的正数。';
  }

  if (amount.value < 0.01 || amount.value > 99999.99) {
    return '充值金额必须在 ¥ 0.01 到 ¥ 99999.99 之间。';
  }

  return '';
});
const paymentMethod = ref<ExternalPaymentMethod>('WECHAT_PAY');
const busy = ref(false);
const error = ref('');
const message = ref('');

function methodLabel(method: PaymentMethod) {
  const labels: Record<PaymentMethod, string> = {
    STEAM_WALLET: 'Steam 钱包',
    WECHAT_PAY: '微信支付',
    ALIPAY: '支付宝',
    VISA: 'Visa',
    MASTERCARD: '万事达卡'
  };
  return labels[method];
}

function methodHelp(method: PaymentMethod) {
  if (method === 'WECHAT_PAY') return '该微信账户必须与由中国的银行发行的有效银行卡绑定。';
  if (method === 'ALIPAY') return '将模拟跳转到支付宝并立即完成课程演示支付。';
  return '银行卡支付为课程演示模拟支付，不会发起真实扣款。';
}

async function submit() {
  if (amountError.value) {
    error.value = amountError.value;
    return;
  }

  busy.value = true;
  error.value = '';
  message.value = '';
  try {
    const result = await rechargeWallet(amount.value, paymentMethod.value);
    message.value = `充值成功，当前钱包余额 ${money(result.availableBalance)}`;
    setTimeout(() => router.push('/wallet/history'), 500);
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    busy.value = false;
  }
}
</script>

<style scoped>
.payment-page {
  width: min(1500px, 100%);
  margin: 0 auto;
  display: grid;
  gap: 20px;
}

.payment-page h1 {
  margin: 0;
  font-size: 2.25rem;
  line-height: 1.15;
}

.payment-layout {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 430px;
  gap: 40px;
  align-items: start;
}

.payment-panel,
.accepted-card {
  background: rgba(12, 20, 29, 0.96);
}

.payment-panel {
  position: relative;
  min-height: 430px;
  padding: 34px 32px 38px;
}

.payment-select {
  max-width: 420px;
  color: #c7d5e0;
  font-size: 1rem;
}

.payment-select select {
  height: 54px;
  border: 3px solid #111820;
  border-radius: 5px;
  color: #ffffff;
  background: #67bce8;
  font-size: 1rem;
  font-weight: 700;
}

.payment-notice {
  display: flex;
  gap: 12px;
  align-items: center;
  margin: 34px 0 22px;
  padding: 20px 24px;
  border-radius: 4px;
  color: #e8f3fb;
  background: #4d7185;
  font-size: 0.98rem;
  font-weight: 700;
}

.checkout-summary {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
  max-width: 620px;
  margin: 0 0 28px;
}

.checkout-summary div {
  padding: 12px 14px;
  background: #182636;
}

.checkout-summary dt {
  color: #8aa8bd;
}

.checkout-summary dd {
  margin: 0;
  font-size: 1rem;
}

.payment-panel p {
  color: #c7d5e0;
  font-size: 1rem;
}

.continue-button {
  position: absolute;
  right: 32px;
  bottom: 38px;
  min-width: 104px;
  min-height: 50px;
  border: 0;
  border-radius: 3px;
  color: #dcefb2;
  background: linear-gradient(180deg, #8fbc16, #5f8408);
  font-size: 1rem;
  font-weight: 800;
  cursor: pointer;
}

.accepted-card {
  padding: 24px;
}

.accepted-card h2 {
  margin: 0 0 28px;
  font-size: 1.35rem;
}

.accepted-card p {
  color: #c7d5e0;
  font-weight: 700;
}

.payment-logos {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

.payment-logos span {
  min-width: 76px;
  padding: 8px 10px;
  border-radius: 4px;
  color: #102033;
  background: #f4f7fb;
  font-weight: 900;
  text-align: center;
}

@media (max-width: 980px) {
  .payment-layout,
  .checkout-summary {
    grid-template-columns: 1fr;
  }

  .continue-button {
    position: static;
    margin-top: 20px;
  }
}
</style>
