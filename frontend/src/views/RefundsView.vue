<template>
  <section class="view-stack">
    <div class="section-heading">
      <p class="eyebrow">Refunds</p>
      <h1>退款申请</h1>
    </div>

    <form class="tool-panel" @submit.prevent="submitRefund">
      <label>
        <span>选择订单</span>
        <select v-model="selectedOrderId" required>
          <option value="" disabled>请选择可退款订单</option>
          <option v-for="order in orders" :key="order.orderId" :value="order.orderId">
            {{ order.orderId }} · {{ order.details.map((detail) => detail.gameName).join('、') }} · {{ money(order.totalAmount) }}
          </option>
        </select>
      </label>
      <label>
        <span>退款原因</span>
        <textarea v-model.trim="reason" rows="3" required />
      </label>
      <button class="primary-button" type="submit">提交退款</button>
    </form>

    <p v-if="message" class="message success">{{ message }}</p>
    <p v-if="error" class="message error">{{ error }}</p>

    <section class="data-panel">
      <header class="panel-header">
        <h2>我的退款单</h2>
        <button class="ghost-button" type="button" @click="load">刷新</button>
      </header>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>退款号</th>
              <th>订单号</th>
              <th>金额</th>
              <th>状态</th>
              <th>原因</th>
              <th>申请时间</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="refund in refunds" :key="refund.refundId">
              <td>{{ refund.refundId }}</td>
              <td>{{ refund.orderId }}</td>
              <td>{{ money(refund.refundAmount) }}</td>
              <td>{{ refund.status }}</td>
              <td>{{ refund.reason }}</td>
              <td>{{ dateTime(refund.applyTime) }}</td>
            </tr>
            <tr v-if="!refunds.length">
              <td colspan="6">暂无退款单</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { createRefund, getOrders, getRefunds, type OrderSummary, type RefundSummary } from '../api/coreApi';
import { getApiError } from '../api/http';
import { dateTime, money } from '../utils/format';

const orders = ref<OrderSummary[]>([]);
const refunds = ref<RefundSummary[]>([]);
const selectedOrderId = ref('');
const reason = ref('体验不符合预期，申请课程演示退款。');
const error = ref('');
const message = ref('');

async function load() {
  error.value = '';
  try {
    const [orderList, refundList] = await Promise.all([getOrders(), getRefunds()]);
    orders.value = orderList.filter((order) => order.paymentStatus === 'PAID' || order.paymentStatus === 'PARTIAL_REFUNDED');
    refunds.value = refundList;
  } catch (requestError) {
    error.value = getApiError(requestError);
  }
}

async function submitRefund() {
  error.value = '';
  message.value = '';
  try {
    const refund = await createRefund(selectedOrderId.value, reason.value);
    message.value = `退款申请已提交：${refund.refundId}`;
    selectedOrderId.value = '';
    await load();
  } catch (requestError) {
    error.value = getApiError(requestError);
  }
}

onMounted(load);
</script>
