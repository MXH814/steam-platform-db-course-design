<template>
  <div class="app-shell" @keydown.esc="closeOverlays">
    <header class="client-header">
      <div class="window-menu">
        <RouterLink class="window-wordmark" to="/store"><Gamepad2 :size="15" /> Game Deck</RouterLink>
        <button type="button" @click="scrollToTop">查看</button>
        <button type="button" @click="openFriends">好友</button>
        <button type="button" @click="goToGames">游戏</button>
        <button type="button" @click="router.push('/refunds')">帮助</button>
        <span class="window-spacer" />
        <span class="cloud-state"><Cloud :size="13" /> 云端已连接</span>
      </div>

      <div class="client-menu">
        <RouterLink class="client-brand" to="/store" aria-label="Game Deck 商店">
          <span class="brand-symbol" aria-hidden="true"><Gamepad2 :size="22" /></span>
        </RouterLink>

        <nav class="supernav" aria-label="客户端主导航">
          <div class="supernav-entry" @mouseenter="openMenu('store')" @mouseleave="scheduleMenuClose">
            <RouterLink to="/store" @focus="openMenu('store')">商店</RouterLink>
            <div v-if="activeMenu === 'store'" class="supernav-menu" @mouseenter="cancelMenuClose">
              <RouterLink to="/store">主页</RouterLink>
              <RouterLink to="/store/recommend">探索队列</RouterLink>
              <RouterLink to="/store/categories">类别</RouterLink>
              <RouterLink to="/store/specials">特别优惠</RouterLink>
            </div>
          </div>
          <div v-if="auth.isAuthenticated" class="supernav-entry" @mouseenter="openMenu('library')" @mouseleave="scheduleMenuClose">
            <RouterLink to="/library" @focus="openMenu('library')">库</RouterLink>
            <div v-if="activeMenu === 'library'" class="supernav-menu" @mouseenter="cancelMenuClose">
              <RouterLink to="/library">主页</RouterLink>
              <RouterLink to="/library/GAME_CS2">Counter-Strike 2</RouterLink>
              <RouterLink to="/library/GAME_DST">饥荒联机版</RouterLink>
              <button type="button" @click="openDownloads">下载</button>
            </div>
          </div>
          <div class="supernav-entry" @mouseenter="openMenu('community')" @mouseleave="scheduleMenuClose">
            <RouterLink to="/games/GAME_DST/community" @focus="openMenu('community')">社区</RouterLink>
            <div v-if="activeMenu === 'community'" class="supernav-menu" @mouseenter="cancelMenuClose">
              <RouterLink to="/games/GAME_DST/community">社区主页</RouterLink>
              <RouterLink to="/games/GAME_CS2/community">讨论与评测</RouterLink>
              <RouterLink to="/inventory">库存</RouterLink>
              <RouterLink to="/market">市场</RouterLink>
            </div>
          </div>
          <div v-if="auth.isAuthenticated" class="supernav-entry" @mouseenter="openMenu('profile')" @mouseleave="scheduleMenuClose">
            <RouterLink to="/account" @focus="openMenu('profile')">{{ auth.currentUser?.account }}</RouterLink>
            <div v-if="activeMenu === 'profile'" class="supernav-menu" @mouseenter="cancelMenuClose">
              <RouterLink to="/account">个人资料</RouterLink>
              <button type="button" @click="openFriends">好友</button>
              <RouterLink to="/games/GAME_DST/community">内容</RouterLink>
              <RouterLink to="/inventory">库存</RouterLink>
              <RouterLink to="/wallet">钱包</RouterLink>
            </div>
          </div>
        </nav>

        <div class="client-actions">
          <button class="icon-link" type="button" title="通知" aria-label="通知" @click="toggleNotifications">
            <Bell :size="16" /><i v-if="notifications.length" />
          </button>
          <div v-if="notificationOpen" class="notification-popover">
            <header><strong>通知</strong><button type="button" aria-label="关闭通知" @click="notificationOpen = false"><X :size="15" /></button></header>
            <p v-if="notificationsLoading">正在同步平台公告...</p>
            <RouterLink v-for="notice in notifications.slice(0, 4)" :key="notice.noticeId" to="/" @click="notificationOpen = false">
              <strong>{{ notice.title }}</strong><span>{{ notice.content }}</span>
            </RouterLink>
            <p v-if="!notificationsLoading && !notifications.length">没有未读通知</p>
          </div>
          <button v-if="auth.isAuthenticated" class="account-link" type="button" :aria-expanded="activeMenu === 'account'" @click="toggleMenu('account')">
            <span class="account-avatar">{{ accountInitial }}</span><span>{{ auth.currentUser?.account }}</span><ChevronDown :size="14" />
          </button>
          <div v-if="activeMenu === 'account'" class="account-popover">
            <RouterLink to="/account">账户明细</RouterLink><RouterLink to="/wallet">钱包余额</RouterLink><RouterLink to="/wallet/history">购买历史</RouterLink>
            <button type="button" @click="logout">退出账户</button>
          </div>
          <RouterLink v-if="!auth.isAuthenticated" class="install-login" to="/login">登录</RouterLink>
          <button class="mobile-menu-button" type="button" :aria-expanded="mobileMenuOpen" aria-label="打开导航" @click="mobileMenuOpen = !mobileMenuOpen">
            <X v-if="mobileMenuOpen" :size="22" /><Menu v-else :size="22" />
          </button>
        </div>
      </div>

      <div class="store-nav" :class="{ open: mobileMenuOpen }">
        <nav aria-label="功能导航">
          <RouterLink to="/store">您的商店</RouterLink><RouterLink to="/store/recommend">新品与推荐</RouterLink><RouterLink to="/store/categories">类别</RouterLink>
          <RouterLink to="/inventory">库存</RouterLink><RouterLink to="/market">社区市场</RouterLink>
          <RouterLink v-if="auth.isAuthenticated" to="/wallet"><WalletCards :size="15" /> 钱包</RouterLink>
          <RouterLink v-if="auth.isAuthenticated" to="/refunds">客服</RouterLink>
          <RouterLink v-if="auth.isAuthenticated && !auth.isDeveloper" to="/redeem">激活产品</RouterLink>
        </nav>
        <details v-if="auth.isDeveloper || auth.isAdmin" class="manage-menu">
          <summary>管理 <ChevronDown :size="14" /></summary>
          <div><RouterLink v-if="auth.isDeveloper" to="/developer/games">游戏管理</RouterLink><RouterLink v-if="auth.isDeveloper || auth.isAdmin" to="/developer/cdkeys">CDKey 批次</RouterLink><RouterLink v-if="auth.isAdmin" to="/admin/games">游戏上下架</RouterLink><RouterLink v-if="auth.isAdmin" to="/admin/notices">公告管理</RouterLink><RouterLink v-if="auth.isAdmin" to="/admin/refunds">退款审核</RouterLink></div>
        </details>
      </div>
    </header>

    <main class="page" :class="{ 'page-wide': route.name === 'inventory', 'page-library': route.name === 'library' || route.name === 'game-library' }"><RouterView /></main>

    <Transition name="drawer">
      <aside v-if="activeDrawer === 'downloads'" class="client-drawer download-drawer" aria-label="下载">
        <header><div><Download :size="19" /><strong>下载</strong></div><button type="button" aria-label="关闭下载" @click="activeDrawer = ''"><X :size="17" /></button></header>
        <div v-if="libraryLoading" class="drawer-state">正在读取游戏库...</div>
        <article v-for="entry in libraryEntries" :key="entry.libId">
          <img :src="gameMeta(entry.gameId).coverImage" alt="" /><div><strong>{{ entry.gameName }}</strong><span><CheckCircle2 :size="13" /> 已安装 · 云状态已同步</span><i><b /></i></div>
          <button type="button" @click="launchLibrary(entry.gameId)"><Play :size="15" fill="currentColor" /></button>
        </article>
        <RouterLink v-if="!libraryEntries.length && !libraryLoading" to="/store" @click="activeDrawer = ''">前往商店添加游戏</RouterLink>
      </aside>
    </Transition>

    <Transition name="drawer">
      <aside v-if="activeDrawer === 'friends'" class="client-drawer friends-drawer" aria-label="好友与聊天">
        <header><div><Users :size="19" /><strong>好友与聊天</strong></div><button type="button" aria-label="关闭好友与聊天" @click="activeDrawer = ''"><X :size="17" /></button></header>
        <label class="friend-search"><Search :size="15" /><input v-model="friendSearch" type="search" placeholder="搜索好友" /></label>
        <div class="friend-list">
          <button v-for="friend in filteredFriends" :key="friend.name" type="button" :class="{ active: selectedFriend === friend.name }" @click="selectedFriend = friend.name">
            <span class="friend-avatar">{{ friend.name.slice(0, 1) }}</span><span><strong>{{ friend.name }}</strong><small>{{ friend.status }}</small></span><i :class="friend.state" />
          </button>
        </div>
        <section v-if="selectedFriend" class="chat-pane">
          <header>{{ selectedFriend }}</header><div class="chat-log"><p>现在可以开始聊天了。</p><p v-for="(chat, index) in chats[selectedFriend] || []" :key="index" class="mine">{{ chat }}</p></div>
          <form @submit.prevent="sendChat"><input v-model.trim="chatDraft" placeholder="发送消息" /><button type="submit" :disabled="!chatDraft" aria-label="发送消息"><Send :size="16" /></button></form>
        </section>
      </aside>
    </Transition>

    <div v-if="activeDrawer" class="drawer-scrim" @click="activeDrawer = ''" />
    <div v-if="toast" class="client-toast" role="status">{{ toast }}</div>
    <footer class="client-status">
      <RouterLink v-if="auth.isAuthenticated && !auth.isDeveloper" to="/redeem"><Plus :size="15" /> 添加游戏</RouterLink><span class="status-spacer" />
      <button v-if="auth.isAuthenticated" type="button" @click="openDownloads"><Download :size="15" /> 下载</button><button type="button" @click="openFriends"><Users :size="15" /> 好友与聊天</button>
    </footer>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { Bell, CheckCircle2, ChevronDown, Cloud, Download, Gamepad2, Menu, Play, Plus, Search, Send, Users, WalletCards, X } from '@lucide/vue';
import { useRoute, useRouter } from 'vue-router';
import { getLibrary, type LibraryEntry } from './api/coreApi';
import { http } from './api/http';
import type { SysNotice } from './api/types';
import { getGameMeta } from './data/gameCatalog';
import { useAuthStore } from './stores/auth';

type MenuKey = '' | 'store' | 'library' | 'community' | 'profile' | 'account';
type DrawerKey = '' | 'downloads' | 'friends';
const auth = useAuthStore();
const router = useRouter();
const route = useRoute();
const mobileMenuOpen = ref(false);
const activeMenu = ref<MenuKey>('');
const activeDrawer = ref<DrawerKey>('');
const notificationOpen = ref(false);
const notificationsLoading = ref(false);
const notifications = ref<SysNotice[]>([]);
const libraryLoading = ref(false);
const libraryEntries = ref<LibraryEntry[]>([]);
const toast = ref('');
const friendSearch = ref('');
const selectedFriend = ref('');
const chatDraft = ref('');
const chats = ref<Record<string, string[]>>({});
let menuTimer: number | undefined;
let toastTimer: number | undefined;
const friends = [
  { name: 'Alice', status: '正在玩 Counter-Strike 2', state: 'playing' },
  { name: 'Bob', status: '在线', state: 'online' },
  { name: 'Klei', status: '正在玩 饥荒联机版', state: 'playing' },
  { name: '课程设计小组', status: '离开', state: 'away' }
];
const filteredFriends = computed(() => { const keyword = friendSearch.value.trim().toLocaleLowerCase('zh-CN'); return friends.filter((friend) => !keyword || friend.name.toLocaleLowerCase('zh-CN').includes(keyword)); });
const accountInitial = computed(() => auth.currentUser?.account?.trim().charAt(0).toUpperCase() || '?');
const gameMeta = getGameMeta;

watch(() => route.fullPath, () => { activeMenu.value = ''; mobileMenuOpen.value = false; notificationOpen.value = false; });
function openMenu(menu: MenuKey) { cancelMenuClose(); activeMenu.value = menu; }
function toggleMenu(menu: MenuKey) { activeMenu.value = activeMenu.value === menu ? '' : menu; notificationOpen.value = false; }
function scheduleMenuClose() { cancelMenuClose(); menuTimer = window.setTimeout(() => { if (activeMenu.value !== 'account') activeMenu.value = ''; }, 160); }
function cancelMenuClose() { if (menuTimer) window.clearTimeout(menuTimer); }
async function toggleNotifications() {
  notificationOpen.value = !notificationOpen.value; activeMenu.value = '';
  if (!notificationOpen.value || notifications.value.length) return;
  notificationsLoading.value = true;
  try { const { data } = await http.get<SysNotice[]>('/api/notices'); notifications.value = data; } catch { notifications.value = []; } finally { notificationsLoading.value = false; }
}
async function openDownloads() {
  activeDrawer.value = 'downloads'; activeMenu.value = '';
  if (!auth.isAuthenticated) { await router.push({ name: 'login', query: { redirect: route.fullPath } }); activeDrawer.value = ''; return; }
  libraryLoading.value = true;
  try { libraryEntries.value = await getLibrary(); } catch { showToast('暂时无法读取下载列表，请稍后重试。'); } finally { libraryLoading.value = false; }
}
function openFriends() { activeDrawer.value = 'friends'; activeMenu.value = ''; }
function launchLibrary(gameId: string) { activeDrawer.value = ''; router.push({ name: 'game-library', params: { gameId } }); }
function sendChat() { if (!selectedFriend.value || !chatDraft.value) return; chats.value[selectedFriend.value] = [...(chats.value[selectedFriend.value] || []), chatDraft.value]; chatDraft.value = ''; }
function scrollToTop() { window.scrollTo({ top: 0, behavior: 'smooth' }); showToast('已返回页面顶部'); }
function goToGames() { router.push(auth.isAuthenticated ? '/library' : '/store'); }
function showToast(text: string) { toast.value = text; if (toastTimer) window.clearTimeout(toastTimer); toastTimer = window.setTimeout(() => { toast.value = ''; }, 2400); }
function closeOverlays() { activeMenu.value = ''; activeDrawer.value = ''; notificationOpen.value = false; }
function logout() { auth.logout(); closeOverlays(); router.push({ name: 'login' }); }
onBeforeUnmount(() => { cancelMenuClose(); if (toastTimer) window.clearTimeout(toastTimer); });
</script>
