<template>
  <div class="app-shell">
    <header class="topbar">
      <RouterLink class="brand" to="/store">Steam Platform</RouterLink>
      <nav class="nav-links" aria-label="main navigation">
        <RouterLink to="/store">&#21830;&#24215;</RouterLink>
        <RouterLink to="/games/GAME_DST/community">&#31038;&#21306;</RouterLink>
        <RouterLink to="/market">&#24066;&#22330;</RouterLink>
        <RouterLink v-if="auth.isAuthenticated" to="/wallet">&#38065;&#21253;</RouterLink>
        <RouterLink v-if="auth.isAuthenticated" to="/orders">&#35746;&#21333;</RouterLink>
        <RouterLink v-if="auth.isAuthenticated" to="/library">&#28216;&#25103;&#24211;</RouterLink>
        <RouterLink v-if="auth.isAuthenticated" to="/refunds">&#36864;&#27454;</RouterLink>
        <RouterLink v-if="auth.isAuthenticated" to="/redeem">CDKey</RouterLink>
        <RouterLink to="/">&#20844;&#21578;</RouterLink>
        <RouterLink v-if="auth.isAuthenticated" to="/account">&#36134;&#25143;</RouterLink>
        <RouterLink v-if="auth.isDeveloper || auth.isAdmin" to="/developer/cdkeys">&#24320;&#21457;&#21830;</RouterLink>
        <RouterLink v-if="auth.isAdmin" to="/admin/notices">&#20844;&#21578;&#31649;&#29702;</RouterLink>
        <RouterLink v-if="auth.isAdmin" to="/admin/refunds">&#36864;&#27454;&#23457;&#26680;</RouterLink>
      </nav>
      <div class="session">
        <span v-if="auth.currentUser" class="session-user">{{ auth.currentUser.account }}</span>
        <button v-if="auth.isAuthenticated" class="ghost-button" type="button" @click="logout">&#36864;&#20986;</button>
        <RouterLink v-else class="button-link" to="/login">&#30331;&#24405;</RouterLink>
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