<template>
  <section class="view-stack">
    <div class="section-heading">
      <p class="eyebrow">Admin</p>
      <h1>退款审核</h1>
    </div>

    <p v-if="message" class="message success">{{ message }}</p>
    <p v-if="error" class="message error">{{ error }}</p>

    <section class="data-panel">
      <header class="panel-header">
        <h2>退款单</h2>
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
              <th>游戏时长</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="refund in refunds" :key="refund.refundId">
              <td>{{ refund.refundId }}</td>
              <td>{{ refund.orderId }}</td>
              <td>{{ money(refund.refundAmount) }}</td>
              <td>{{ refund.status }}</td>
              <td>{{ refund.playTimeHours }} 小时</td>
              <td>
                <div class="row-actions">
                  <button class="secondary-button" type="button" :disabled="refund.status !== 'PENDING'" @click="approve(refund.refundId)">通过</button>
                  <button class="ghost-button" type="button" :disabled="refund.status !== 'PENDING'" @click="reject(refund.refundId)">拒绝</button>
                </div>
              </td>
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
import { approveRefund, getAdminRefunds, rejectRefund, type RefundSummary } from '../api/coreApi';
import { getApiError } from '../api/http';
import { money } from '../utils/format';

const refunds = ref<RefundSummary[]>([]);
const error = ref('');
const message = ref('');

async function load() {
  error.value = '';
  try {
    refunds.value = await getAdminRefunds();
  } catch (requestError) {
    error.value = getApiError(requestError);
  }
}

async function approve(refundId: string) {
  await audit(refundId, true);
}

async function reject(refundId: string) {
  await audit(refundId, false);
}

async function audit(refundId: string, approved: boolean) {
  error.value = '';
  message.value = '';
  try {
    const refund = approved
      ? await approveRefund(refundId, '课程演示审核通过。')
      : await rejectRefund(refundId, '课程演示审核拒绝。');
    message.value = `退款单 ${refund.refundId} 已更新为 ${refund.status}`;
    await load();
  } catch (requestError) {
    error.value = getApiError(requestError);
  }
}

onMounted(load);
</script>
