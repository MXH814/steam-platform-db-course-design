<template>
  <div class="app-shell">
    <header class="client-header">
      <div class="client-menu">
        <RouterLink class="client-brand" to="/store">
          <span class="brand-symbol" aria-hidden="true"><Gamepad2 :size="23" /></span>
          <span>Game Deck</span>
        </RouterLink>
        <nav aria-label="客户端菜单">
          <RouterLink to="/store">商店</RouterLink>
          <RouterLink v-if="auth.isAuthenticated" to="/library">游戏库</RouterLink>
          <RouterLink to="/games/GAME_DST/community">社区</RouterLink>
        </nav>
        <div class="client-actions">
          <RouterLink class="icon-link" to="/" title="通知" aria-label="通知"><Bell :size="16" /></RouterLink>
          <RouterLink v-if="auth.isAuthenticated" class="account-link" to="/account">
            <span class="account-avatar">{{ accountInitial }}</span>
            <span>{{ auth.currentUser?.account }}</span>
            <ChevronDown :size="14" />
          </RouterLink>
          <RouterLink v-else class="install-login" to="/login">登录</RouterLink>
          <button class="mobile-menu-button" type="button" :aria-expanded="mobileMenuOpen" aria-label="打开导航" @click="mobileMenuOpen = !mobileMenuOpen">
            <X v-if="mobileMenuOpen" :size="22" />
            <Menu v-else :size="22" />
          </button>
        </div>
      </div>

      <div class="store-nav" :class="{ open: mobileMenuOpen }">
        <nav aria-label="功能导航">
          <RouterLink to="/store">您的商店</RouterLink>
          <RouterLink to="/store/recommend">新品与推荐</RouterLink>
          <RouterLink to="/store/categories">类别</RouterLink>
          <RouterLink to="/inventory">库存</RouterLink>
          <RouterLink to="/market">社区市场</RouterLink>
          <RouterLink v-if="auth.isAuthenticated" to="/wallet"><WalletCards :size="15" /> 钱包</RouterLink>
          <RouterLink v-if="auth.isAuthenticated" to="/refunds">客服</RouterLink>
          <RouterLink v-if="auth.isAuthenticated && !auth.isDeveloper" to="/redeem">激活产品</RouterLink>
        </nav>
        <details v-if="auth.isDeveloper || auth.isAdmin" class="manage-menu">
          <summary>管理 <ChevronDown :size="14" /></summary>
          <div>
            <RouterLink v-if="auth.isDeveloper" to="/developer/games">游戏管理</RouterLink>
            <RouterLink v-if="auth.isDeveloper || auth.isAdmin" to="/developer/cdkeys">CDKey 批次</RouterLink>
            <RouterLink v-if="auth.isAdmin" to="/admin/games">游戏上下架</RouterLink>
            <RouterLink v-if="auth.isAdmin" to="/admin/notices">公告管理</RouterLink>
            <RouterLink v-if="auth.isAdmin" to="/admin/refunds">退款审核</RouterLink>
          </div>
        </details>
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

    <footer class="client-status">
      <RouterLink v-if="auth.isAuthenticated && !auth.isDeveloper" to="/redeem"><Plus :size="15" /> 添加游戏</RouterLink>
      <span class="status-spacer"></span>
      <RouterLink v-if="auth.isAuthenticated" to="/wallet/history"><Download :size="15" /> 下载</RouterLink>
      <RouterLink to="/games/GAME_DST/community"><Users :size="15" /> 好友与聊天</RouterLink>
      <button v-if="auth.isAuthenticated" type="button" @click="logout">退出</button>
    </footer>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { Bell, ChevronDown, Download, Gamepad2, Menu, Plus, Users, WalletCards, X } from '@lucide/vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from './stores/auth';

const auth = useAuthStore();
const router = useRouter();
const route = useRoute();
const mobileMenuOpen = ref(false);
const accountInitial = computed(() => auth.currentUser?.account?.trim().charAt(0).toUpperCase() || '?');

function logout() {
  auth.logout();
  router.push({ name: 'login' });
}
</script>
