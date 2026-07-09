<template>
  <section class="view-stack">
    <div class="section-heading">
      <p class="eyebrow">Orders</p>
      <h1>购买与订单</h1>
    </div>

    <section class="purchase-grid">
      <article class="game-purchase dst">
        <div>
          <span class="pill">买断制</span>
          <h2>Don't Starve Together / 饥荒联机版</h2>
          <p>使用钱包余额购买，生成订单、支付流水并进入游戏库。</p>
        </div>
        <button class="primary-button" type="button" :disabled="busy" @click="buyDst">购买 DST</button>
      </article>

      <article class="game-purchase cs2">
        <div>
          <span class="pill">免费入库</span>
          <h2>Counter-Strike 2</h2>
          <p>不扣钱包余额，走免费游戏入库链路。</p>
        </div>
        <button class="secondary-button" type="button" :disabled="busy" @click="claimCs2">免费领取 CS2</button>
      </article>
    </section>

    <p v-if="message" class="message success">{{ message }}</p>
    <p v-if="error" class="message error">{{ error }}</p>

    <section class="data-panel">
      <header class="panel-header">
        <h2>我的订单</h2>
        <button class="ghost-button" type="button" @click="load">刷新</button>
      </header>
      <div class="order-list">
        <RouterLink v-for="order in orders" :key="order.orderId" class="order-row" :to="`/orders/${order.orderId}`">
          <div>
            <strong>{{ order.details.map((detail) => detail.gameName).join('、') || order.orderId }}</strong>
            <span>{{ order.orderStatus }} / {{ order.paymentStatus }}</span>
          </div>
          <div class="align-right">
            <strong>{{ money(order.totalAmount) }}</strong>
            <span>{{ dateTime(order.createTime) }}</span>
          </div>
        </RouterLink>
        <p v-if="!orders.length" class="empty-text">暂无订单</p>
      </div>
    </section>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { buyGame, claimFreeGame, getOrders, type OrderSummary } from '../api/coreApi';
import { getApiError } from '../api/http';
import { dateTime, money } from '../utils/format';

const orders = ref<OrderSummary[]>([]);
const busy = ref(false);
const error = ref('');
const message = ref('');

async function load() {
  error.value = '';
  try {
    orders.value = await getOrders();
  } catch (requestError) {
    error.value = getApiError(requestError);
  }
}

async function buyDst() {
  await runOrderAction(() => buyGame('GAME_DST'), 'DST 购买流程已完成');
}

async function claimCs2() {
  await runOrderAction(() => claimFreeGame('GAME_CS2'), 'CS2 已免费入库');
}

async function runOrderAction(action: () => Promise<OrderSummary>, success: string) {
  busy.value = true;
  error.value = '';
  message.value = '';
  try {
    const order = await action();
    message.value = `${success}：${order.orderId}`;
    await load();
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    busy.value = false;
  }
}

onMounted(load);
</script>
