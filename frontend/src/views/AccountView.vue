<template>
  <section class="view-stack">
    <div class="section-heading">
      <p class="eyebrow">当前用户</p>
      <h1>账户中心</h1>
    </div>

    <div v-if="auth.currentUser" class="details-panel">
      <dl>
        <div>
          <dt>账号</dt>
          <dd>{{ auth.currentUser.account }}</dd>
        </div>
        <div>
          <dt>角色</dt>
          <dd>{{ auth.currentUser.role }}</dd>
        </div>
        <div>
          <dt>主体 ID</dt>
          <dd>{{ auth.currentUser.principalId }}</dd>
        </div>
        <div>
          <dt>登录有效期</dt>
          <dd>{{ new Date(auth.currentUser.expiresAt).toLocaleString() }}</dd>
        </div>
      </dl>
    </div>

    <section class="data-panel">
      <header class="panel-header">
        <h2>常用入口</h2>
      </header>
      <div class="quick-grid">
        <RouterLink class="quick-link" to="/wallet">钱包余额与流水</RouterLink>
        <RouterLink class="quick-link" to="/orders">购买与订单</RouterLink>
        <RouterLink class="quick-link" to="/library">我的游戏库</RouterLink>
        <RouterLink class="quick-link" to="/refunds">退款申请</RouterLink>
        <RouterLink class="quick-link" to="/redeem">CDKey 兑换</RouterLink>
        <RouterLink v-if="auth.isDeveloper" class="quick-link" to="/developer/games">游戏管理</RouterLink>
        <RouterLink v-if="auth.isDeveloper || auth.isAdmin" class="quick-link" to="/developer/cdkeys">CDKey 批次</RouterLink>
        <RouterLink v-if="auth.isAdmin" class="quick-link" to="/admin/games">游戏上下架</RouterLink>
        <RouterLink v-if="auth.isAdmin" class="quick-link" to="/admin/refunds">退款审核</RouterLink>
      </div>
    </section>
  </section>
</template>

<script setup lang="ts">
import { useAuthStore } from '../stores/auth';

const auth = useAuthStore();
</script>
