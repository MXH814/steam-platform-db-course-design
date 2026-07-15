import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from './stores/auth';
import AccountView from './views/AccountView.vue';
import AdminGamesView from './views/AdminGamesView.vue';
import AdminNoticesView from './views/AdminNoticesView.vue';
import AdminRefundsView from './views/AdminRefundsView.vue';
import CdkeyBatchView from './views/CdkeyBatchView.vue';
import DeveloperGamesView from './views/DeveloperGamesView.vue';
import GameCommunityView from './views/GameCommunityView.vue';
import GameCheckoutView from './views/GameCheckoutView.vue';
import GameDetailView from './views/GameDetailView.vue';
import GameLibraryView from './views/GameLibraryView.vue';
import HomeView from './views/HomeView.vue';
import InventoryView from './views/InventoryView.vue';
import LibraryView from './views/LibraryView.vue';
import LoginView from './views/LoginView.vue';
import MarketView from './views/MarketView.vue';
import OrderDetailView from './views/OrderDetailView.vue';
import RedeemView from './views/RedeemView.vue';
import RegisterView from './views/RegisterView.vue';
import RefundsView from './views/RefundsView.vue';
import StoreCollectionView from './views/StoreCollectionView.vue';
import StoreView from './views/StoreView.vue';
import WalletView from './views/WalletView.vue';
import WalletHistoryView from './views/WalletHistoryView.vue';
import WalletHistoryDetailView from './views/WalletHistoryDetailView.vue';
import WalletRefundPlaceholderView from './views/WalletRefundPlaceholderView.vue';
import WalletRechargeCheckoutView from './views/WalletRechargeCheckoutView.vue';

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'home', component: HomeView },
    { path: '/store', name: 'store', component: StoreView },
    { path: '/store/:section(recommend|categories|playstyles|specials)', name: 'store-section', component: StoreCollectionView },
    { path: '/store/:section(recommend|categories|playstyles|specials)/:collectionId', name: 'store-collection-detail', component: StoreCollectionView },
    { path: '/games/:gameId', name: 'game-detail', component: GameDetailView },
    { path: '/games/:gameId/store', name: 'game-store', redirect: (to) => ({ name: 'game-detail', params: { gameId: to.params.gameId } }) },
    { path: '/games/:gameId/community', name: 'game-community', component: GameCommunityView },
    { path: '/checkout/game/:gameId', name: 'game-checkout', component: GameCheckoutView, meta: { requiresAuth: true } },
    { path: '/inventory', name: 'inventory', component: InventoryView, meta: { requiresAuth: true } },
    { path: '/library', name: 'library', component: LibraryView, meta: { requiresAuth: true } },
    { path: '/library/:gameId', name: 'game-library', component: GameLibraryView, meta: { requiresAuth: true } },
    { path: '/market', name: 'market', component: MarketView, meta: { tab: 'market' } },
    { path: '/market/orders', name: 'market-orders', component: MarketView, meta: { tab: 'orders' } },
    { path: '/market/trades', name: 'market-trades', component: MarketView, meta: { tab: 'trades' } },
    { path: '/market/transfers', name: 'market-transfers', component: MarketView, meta: { tab: 'transfers' } },
    { path: '/login', name: 'login', component: LoginView },
    { path: '/register', name: 'register', component: RegisterView },
    { path: '/account', name: 'account', component: AccountView, meta: { requiresAuth: true } },
    { path: '/wallet', name: 'wallet', component: WalletView, meta: { requiresAuth: true } },
    { path: '/wallet/recharge/checkout', name: 'wallet-recharge-checkout', component: WalletRechargeCheckoutView, meta: { requiresAuth: true } },
    { path: '/wallet/history', name: 'wallet-history', component: WalletHistoryView, meta: { requiresAuth: true } },
    { path: '/wallet/history/:historyId', name: 'wallet-history-detail', component: WalletHistoryDetailView, meta: { requiresAuth: true } },
    { path: '/wallet/history/:historyId/refund', name: 'wallet-history-refund', component: WalletRefundPlaceholderView, meta: { requiresAuth: true } },
    { path: '/orders', name: 'orders', redirect: { name: 'wallet-history' }, meta: { requiresAuth: true } },
    { path: '/orders/:orderId', name: 'order-detail', component: OrderDetailView, meta: { requiresAuth: true } },
    { path: '/refunds', name: 'refunds', component: RefundsView, meta: { requiresAuth: true } },
    { path: '/redeem', name: 'redeem', component: RedeemView, meta: { requiresAuth: true } },
    { path: '/developer/games', name: 'developer-games', component: DeveloperGamesView, meta: { requiresAuth: true, requiresDeveloper: true } },
    { path: '/developer/cdkeys', name: 'developer-cdkeys', component: CdkeyBatchView, meta: { requiresAuth: true, requiresDeveloper: true } },
    { path: '/admin/games', name: 'admin-games', component: AdminGamesView, meta: { requiresAuth: true, requiresAdmin: true } },
    { path: '/admin/notices', name: 'admin-notices', component: AdminNoticesView, meta: { requiresAuth: true, requiresAdmin: true } },
    { path: '/admin/refunds', name: 'admin-refunds', component: AdminRefundsView, meta: { requiresAuth: true, requiresAdmin: true } }
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

  if (to.meta.requiresDeveloper && !auth.isDeveloper && !auth.isAdmin) {
    return { name: 'account' };
  }

  return true;
});
