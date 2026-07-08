import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from './stores/auth';
import AccountView from './views/AccountView.vue';
import AdminNoticesView from './views/AdminNoticesView.vue';
import HomeView from './views/HomeView.vue';
import GameCommunityView from './views/GameCommunityView.vue';
import LoginView from './views/LoginView.vue';
import RegisterView from './views/RegisterView.vue';

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'home', component: HomeView },
    { path: '/games/:gameId/community', name: 'game-community', component: GameCommunityView },
    { path: '/login', name: 'login', component: LoginView },
    { path: '/register', name: 'register', component: RegisterView },
    { path: '/account', name: 'account', component: AccountView, meta: { requiresAuth: true } },
    { path: '/admin/notices', name: 'admin-notices', component: AdminNoticesView, meta: { requiresAuth: true, requiresAdmin: true } }
  ]
});

router.beforeEach(async (to) => {
  const auth = useAuthStore();
  if (!auth.initialized) {
    await auth.bootstrap();
  }

  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } };
  }

  if (to.meta.requiresAdmin && !auth.isAdmin) {
    return { name: 'account' };
  }

  return true;
});
