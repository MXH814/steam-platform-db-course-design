import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from './stores/auth';
import AccountView from './views/AccountView.vue';
import AdminNoticesView from './views/AdminNoticesView.vue';
import GameDetailView from './views/GameDetailView.vue';
import HomeView from './views/HomeView.vue';
import GameCommunityView from './views/GameCommunityView.vue';
import LoginView from './views/LoginView.vue';
import MarketView from './views/MarketView.vue';
import RegisterView from './views/RegisterView.vue';
import StoreCollectionView from './views/StoreCollectionView.vue';
import StoreView from './views/StoreView.vue';

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'home', component: HomeView },
    { path: '/store', name: 'store', component: StoreView },
    { path: '/store/:section(recommend|categories|playstyles|specials)', name: 'store-collection', component: StoreCollectionView },
    { path: '/store/:section(categories|playstyles|specials)/:collectionId', name: 'store-collection-detail', component: StoreCollectionView },
    { path: '/games/:gameId', name: 'game-detail', component: GameDetailView },
    { path: '/games/:gameId/community', name: 'game-community', component: GameCommunityView },
    { path: '/market', name: 'market', component: MarketView, meta: { tab: 'market' } },
    { path: '/market/orders', name: 'market-orders', component: MarketView, meta: { tab: 'orders' } },
    { path: '/market/trades', name: 'market-trades', component: MarketView, meta: { tab: 'trades' } },
    { path: '/market/transfers', name: 'market-transfers', component: MarketView, meta: { tab: 'transfers' } },
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
