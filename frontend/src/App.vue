<template>
  <div class="app-shell">
    <header class="topbar">
      <RouterLink class="brand" to="/">Steam Platform</RouterLink>
      <nav class="nav-links" aria-label="主导航">
        <RouterLink to="/">公告</RouterLink>
        <RouterLink v-if="auth.isAuthenticated" to="/account">账户</RouterLink>
        <RouterLink v-if="auth.isAdmin" to="/admin/notices">公告管理</RouterLink>
      </nav>
      <div class="session">
        <span v-if="auth.currentUser" class="session-user">{{ auth.currentUser.account }}</span>
        <button v-if="auth.isAuthenticated" class="ghost-button" type="button" @click="logout">退出</button>
        <RouterLink v-else class="button-link" to="/login">登录</RouterLink>
      </div>
    </header>

    <main class="page">
      <RouterView />
    </main>
  </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';
import { useAuthStore } from './stores/auth';

const auth = useAuthStore();
const router = useRouter();

function logout() {
  auth.logout();
  router.push({ name: 'login' });
}
</script>
