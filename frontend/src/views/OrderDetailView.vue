<template>
  <section class="view-stack">
    <div class="section-heading">
      <p class="eyebrow">Order</p>
      <h1>订单详情</h1>
    </div>

    <p v-if="error" class="message error">{{ error }}</p>

    <section v-if="order" class="details-panel">
      <dl>
        <div>
          <dt>订单号</dt>
          <dd>{{ order.orderId }}</dd>
        </div>
        <div>
          <dt>总金额</dt>
          <dd>{{ money(order.totalAmount) }}</dd>
        </div>
        <div>
          <dt>订单状态</dt>
          <dd>{{ order.orderStatus }}</dd>
        </div>
        <div>
          <dt>支付状态</dt>
          <dd>{{ order.paymentStatus }}</dd>
        </div>
        <div>
          <dt>创建时间</dt>
          <dd>{{ dateTime(order.createTime) }}</dd>
        </div>
        <div>
          <dt>幂等键</dt>
          <dd>{{ order.idempotencyKey || '-' }}</dd>
        </div>
      </dl>
    </section>

    <section v-if="order" class="data-panel">
      <header class="panel-header">
        <h2>订单明细</h2>
      </header>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>游戏</th>
              <th>原价</th>
              <th>折扣</th>
              <th>实付</th>
              <th>已退</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="detail in order.details" :key="detail.detailId">
              <td>{{ detail.gameName }}</td>
              <td>{{ money(detail.originalPrice) }}</td>
              <td>{{ money(detail.discountAmount) }}</td>
              <td>{{ money(detail.payableAmount) }}</td>
              <td>{{ money(detail.refundAmount) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { getOrder, type OrderSummary } from '../api/coreApi';
import { getApiError } from '../api/http';
import { dateTime, money } from '../utils/format';

const route = useRoute();
const order = ref<OrderSummary | null>(null);
const error = ref('');

onMounted(async () => {
  try {
    order.value = await getOrder(String(route.params.orderId));
  } catch (requestError) {
    error.value = getApiError(requestError);
  }
});
</script>
