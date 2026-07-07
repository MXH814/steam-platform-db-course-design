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
  </section>
</template>

<script setup lang="ts">
import { useAuthStore } from '../stores/auth';

const auth = useAuthStore();
</script>
