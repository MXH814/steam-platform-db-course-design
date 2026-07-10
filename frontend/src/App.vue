<template>
  <div class="app-shell">
    <header class="topbar">
      <RouterLink class="brand" to="/store">Steam Platform</RouterLink>
      <nav class="nav-links" aria-label="主导航">
        <RouterLink to="/store">商店</RouterLink>
        <RouterLink to="/games/GAME_DST/community">社区</RouterLink>
        <RouterLink to="/inventory">库存</RouterLink>
        <RouterLink to="/market">市场</RouterLink>
        <RouterLink v-if="auth.isAuthenticated" to="/wallet">钱包</RouterLink>
        <RouterLink v-if="auth.isAuthenticated" to="/orders">订单</RouterLink>
        <RouterLink v-if="auth.isAuthenticated" to="/library">游戏库</RouterLink>
        <RouterLink v-if="auth.isAuthenticated" to="/refunds">退款</RouterLink>
        <RouterLink v-if="auth.isAuthenticated" to="/redeem">CDKey</RouterLink>
        <RouterLink to="/">公告</RouterLink>
        <RouterLink v-if="auth.isAuthenticated" to="/account">账户</RouterLink>
        <RouterLink v-if="auth.isDeveloper" to="/developer/games">游戏管理</RouterLink>
        <RouterLink v-if="auth.isDeveloper || auth.isAdmin" to="/developer/cdkeys">CDKey 批次</RouterLink>
        <RouterLink v-if="auth.isAdmin" to="/admin/games">游戏上下架</RouterLink>
        <RouterLink v-if="auth.isAdmin" to="/admin/notices">公告管理</RouterLink>
        <RouterLink v-if="auth.isAdmin" to="/admin/refunds">退款审核</RouterLink>
      </nav>
      <div class="session">
        <span v-if="auth.currentUser" class="session-user">{{ auth.currentUser.account }}</span>
        <button v-if="auth.isAuthenticated" class="ghost-button" type="button" @click="logout">退出</button>
        <RouterLink v-else class="button-link" to="/login">登录</RouterLink>
      </div>
    </header>

    <main
      class="page"
      :class="{
        'page-wide': route.name === 'inventory',
        'page-library': route.name === 'library' || route.name === 'game-library'
      }"
    >
      <RouterView />
    </main>
  </div>
</template>

<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from './stores/auth';

const auth = useAuthStore();
const router = useRouter();
const route = useRoute();

function logout() {
  auth.logout();
  router.push({ name: 'login' });
}
</script>
