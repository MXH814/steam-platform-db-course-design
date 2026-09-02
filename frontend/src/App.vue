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
            <RouterLink to="/community" @focus="openMenu('community')">社区</RouterLink>
            <div v-if="activeMenu === 'community'" class="supernav-menu" @mouseenter="cancelMenuClose">
              <RouterLink to="/community">社区主页</RouterLink>
              <RouterLink to="/community?tab=discussions">讨论区</RouterLink>
              <RouterLink to="/games/GAME_CS2/community">游戏中心</RouterLink>
              <RouterLink to="/inventory">库存</RouterLink>
              <RouterLink to="/market">市场</RouterLink>
            </div>
          </div>
          <div v-if="auth.isAuthenticated" class="supernav-entry" @mouseenter="openMenu('profile')" @mouseleave="scheduleMenuClose">
            <RouterLink to="/profile" @focus="openMenu('profile')">{{ auth.currentUser?.account }}</RouterLink>
            <div v-if="activeMenu === 'profile'" class="supernav-menu" @mouseenter="cancelMenuClose">
              <RouterLink to="/profile">个人资料</RouterLink>
              <button type="button" @click="openFriends">好友</button>
              <RouterLink to="/community">内容</RouterLink>
              <RouterLink to="/inventory">库存</RouterLink>
              <RouterLink to="/trade-offers">交易报价</RouterLink>
              <RouterLink to="/wallet">钱包</RouterLink>
            </div>
          </div>
        </nav>

        <div class="client-actions">
          <button class="icon-link" type="button" title="通知" aria-label="通知" @click="toggleNotifications">
            <Bell :size="16" /><i v-if="unreadNotificationCount" />
          </button>
          <div v-if="notificationOpen" class="notification-popover">
            <header><strong>通知</strong><button type="button" aria-label="关闭通知" @click="notificationOpen = false"><X :size="15" /></button></header>
            <p v-if="notificationsLoading">正在同步平台公告...</p>
            <button v-for="notice in userNotifications.slice(0, 6)" :key="notice.notificationId" type="button" @click="openUserNotification(notice)">
              <strong>{{ notice.isRead ? notice.title : `● ${notice.title}` }}</strong><span>{{ notice.message }}</span>
            </button>
            <button v-for="(notice, index) in startupAnnouncements.slice(0, 4)" :key="notice.id" type="button" @click="openAnnouncement(index)">
              <strong>{{ notice.title }}</strong><span>{{ notice.content }}</span>
            </button>
            <p v-if="!notificationsLoading && !userNotifications.length && !startupAnnouncements.length">没有未读通知</p>
          </div>
          <button v-if="auth.isAuthenticated" class="account-link" type="button" :aria-expanded="activeMenu === 'account'" @click="toggleMenu('account')">
            <span class="account-avatar">{{ accountInitial }}</span><span>{{ auth.currentUser?.account }}</span><ChevronDown :size="14" />
          </button>
          <div v-if="activeMenu === 'account'" class="account-popover">
            <RouterLink to="/profile">查看我的资料</RouterLink><RouterLink to="/account">账户明细</RouterLink><RouterLink to="/wallet">钱包余额</RouterLink><RouterLink to="/wallet/history">购买历史</RouterLink>
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

    <Transition name="announcement">
      <div v-if="startupAnnouncementOpen && currentAnnouncement" class="startup-announcement-backdrop" @click.self="dismissStartupAnnouncement">
        <article class="startup-announcement" role="dialog" aria-modal="true" aria-labelledby="startup-announcement-title">
          <button class="announcement-close-icon" type="button" aria-label="关闭启动公告" @click="dismissStartupAnnouncement"><X :size="22" /></button>
          <div class="announcement-media">
            <img :src="currentAnnouncement.image" :alt="currentAnnouncement.title" />
            <span>{{ currentAnnouncement.label }}</span>
          </div>
          <section class="announcement-copy">
            <small>{{ currentAnnouncement.label }} · {{ currentAnnouncement.publishedAt }}</small>
            <h2 id="startup-announcement-title">{{ currentAnnouncement.title }}</h2>
            <p>{{ currentAnnouncement.content }}</p>
            <button type="button" @click="openAnnouncementDetail">点击查看详细信息</button>
          </section>
          <nav class="announcement-pager" aria-label="公告轮播">
            <button type="button" aria-label="上一条公告" @click="stepAnnouncement(-1)"><ChevronLeft :size="34" /></button>
            <div>
              <button
                v-for="(notice, index) in startupAnnouncements"
                :key="`dot-${notice.id}`"
                type="button"
                :class="{ active: startupAnnouncementIndex === index }"
                :aria-label="`查看第 ${index + 1} 条公告`"
                @click="startupAnnouncementIndex = index"
              />
            </div>
            <button type="button" aria-label="下一条公告" @click="stepAnnouncement(1)"><ChevronRight :size="34" /></button>
          </nav>
          <footer>
            <span>Game Deck 平台公告与课程演示内容</span>
            <button type="button" @click="dismissStartupAnnouncement">关闭</button>
          </footer>
        </article>
      </div>
    </Transition>

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
          <div v-for="friend in filteredFriends" :key="friend.userId" class="friend-row">
            <button type="button" :class="{ active: selectedFriendId === friend.userId }" @click="selectFriend(friend.userId)">
              <span class="friend-avatar">{{ friend.nickname.slice(0, 1) }}</span><span><strong>{{ friend.nickname }}</strong><small>{{ friend.latestMessage || relationLabel(friend) }}</small></span><i :class="friend.relationStatus === 'ACCEPTED' ? 'online' : 'away'" />
            </button>
            <button v-if="friend.isIncomingRequest" class="friend-accept" type="button" @click="approveFriend(friend.relationId)">接受</button>
          </div>
          <p v-if="friendsLoading">正在同步好友列表...</p>
          <p v-else-if="!filteredFriends.length">暂无匹配的好友。</p>
        </div>
        <div class="friend-drawer-links"><RouterLink to="/community?tab=people" @click="activeDrawer = ''"><UserPlus :size="15" />添加好友</RouterLink><RouterLink to="/trade-offers" @click="activeDrawer = ''"><ArrowLeftRight :size="15" />交易报价</RouterLink></div>
        <section v-if="selectedFriend" class="chat-pane">
          <header>{{ selectedFriend.nickname }}</header><div class="chat-log"><p v-if="!chatMessages.length">现在可以开始聊天了。</p><p v-for="chat in chatMessages" :key="chat.messageId" :class="{ mine: chat.senderId === auth.currentUser?.principalId }"><small>{{ chat.senderNickname }}</small>{{ chat.content }}</p></div>
          <form @submit.prevent="sendChat"><input v-model.trim="chatDraft" maxlength="1000" placeholder="发送消息" /><button type="submit" :disabled="!chatDraft || sendingChat" aria-label="发送消息"><Send :size="16" /></button></form>
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
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { ArrowLeftRight, Bell, CheckCircle2, ChevronDown, ChevronLeft, ChevronRight, Cloud, Download, Gamepad2, Menu, Play, Plus, Search, Send, UserPlus, Users, WalletCards, X } from '@lucide/vue';
import { useRoute, useRouter } from 'vue-router';
import { getLibrary, type LibraryEntry } from './api/coreApi';
import { http } from './api/http';
import { acceptFriend, listFriends, listMessages, listNotifications as listUserNotifications, markNotificationRead, sendMessage } from './api/socialApi';
import { SocialRealtimeClient } from './api/socialRealtime';
import type { DirectMessageItem, DiscussionTopicView, FriendListItem, SysNotice, TradeOfferView, UserNotificationItem } from './api/types';
import { getGameMeta } from './data/gameCatalog';
import { useAuthStore } from './stores/auth';

type MenuKey = '' | 'store' | 'library' | 'community' | 'profile' | 'account';
type DrawerKey = '' | 'downloads' | 'friends';
interface StartupAnnouncement {
  id: string;
  title: string;
  content: string;
  image: string;
  label: string;
  route: string;
  publishedAt: string;
}
const auth = useAuthStore();
const router = useRouter();
const route = useRoute();
const mobileMenuOpen = ref(false);
const activeMenu = ref<MenuKey>('');
const activeDrawer = ref<DrawerKey>('');
const notificationOpen = ref(false);
const notificationsLoading = ref(false);
const notifications = ref<SysNotice[]>([]);
const userNotifications = ref<UserNotificationItem[]>([]);
const notificationsLoaded = ref(false);
const startupAnnouncementOpen = ref(false);
const startupAnnouncementIndex = ref(0);
const libraryLoading = ref(false);
const libraryEntries = ref<LibraryEntry[]>([]);
const toast = ref('');
const friendSearch = ref('');
const selectedFriendId = ref('');
const chatDraft = ref('');
const chatMessages = ref<DirectMessageItem[]>([]);
const friends = ref<FriendListItem[]>([]);
const friendsLoading = ref(false);
const sendingChat = ref(false);
const realtime = new SocialRealtimeClient();
const startupDismissKey = 'game-deck-startup-announcement-dismissed:2026-08-25';
let menuTimer: number | undefined;
let toastTimer: number | undefined;
const filteredFriends = computed(() => { const keyword = friendSearch.value.trim().toLocaleLowerCase('zh-CN'); return friends.value.filter((friend) => !keyword || friend.nickname.toLocaleLowerCase('zh-CN').includes(keyword)); });
const selectedFriend = computed(() => friends.value.find((friend) => friend.userId === selectedFriendId.value) || null);
const unreadNotificationCount = computed(() => userNotifications.value.filter((notice) => !notice.isRead).length);
const accountInitial = computed(() => auth.currentUser?.account?.trim().charAt(0).toUpperCase() || '?');
const gameMeta = getGameMeta;
const fallbackAnnouncements: StartupAnnouncement[] = [
  { id: 'WELCOME-DST', title: '荒野生存特别活动现已开放', content: '进入饥荒联机版商店页面，查看买断制购买、内容包、社区评测和自定义成就完整流程。', image: '/assets/games/dst-library-hero.jpg', label: '特别活动', route: '/games/GAME_DST', publishedAt: '本周' },
  { id: 'WELCOME-CS2', title: 'CS2 库存与社区市场全面联动', content: '免费加入游戏库，浏览饰品库存、市场挂单、价格历史、成交记录和资产转移账本。', image: '/assets/games/cs2-library-hero.jpg', label: '平台更新', route: '/games/GAME_CS2', publishedAt: '本周' },
  { id: 'WELCOME-WORKSHOP', title: '社区创意工坊浏览功能上线', content: '现在可以搜索、排序、查看详情并订阅 CS2 与饥荒联机版的课程演示工坊作品。', image: '/assets/games/cs2-header.jpg', label: '社区更新', route: '/games/GAME_CS2/community?section=workshop', publishedAt: '今天' }
];
const startupAnnouncements = computed<StartupAnnouncement[]>(() => notifications.value.length
  ? notifications.value.map((notice, index) => ({
      id: notice.noticeId,
      title: notice.title,
      content: notice.content,
      image: index % 2 === 0 ? '/assets/games/dst-library-hero.jpg' : '/assets/games/cs2-library-hero.jpg',
      label: notice.priority >= 8 ? '重要公告' : '平台公告',
      route: noticeRoute(notice),
      publishedAt: new Date(notice.publishTime).toLocaleDateString('zh-CN')
    }))
  : fallbackAnnouncements);
const currentAnnouncement = computed(() => startupAnnouncements.value[startupAnnouncementIndex.value] || null);

watch(() => route.fullPath, () => { activeMenu.value = ''; mobileMenuOpen.value = false; notificationOpen.value = false; });
watch(() => route.name, (name) => { if (name === 'store') void prepareStartupAnnouncement(); }, { immediate: true });
watch(() => auth.isAuthenticated && auth.currentUser?.role === 'PLAYER', async (isPlayer) => {
  await realtime.disconnect();
  if (!isPlayer) {
    friends.value = [];
    userNotifications.value = [];
    return;
  }

  await Promise.all([loadFriends(), loadUserNotifications()]);
  try {
    await realtime.connect({
      onDirectMessage: receiveRealtimeMessage,
      onNotification: receiveRealtimeNotification,
      onFriendChanged: loadFriends,
      onTradeOfferChanged: receiveTradeOffer,
      onDiscussionReply: receiveDiscussionReply
    });
  } catch {
    showToast('实时连接暂未建立，页面操作仍会正常保存。');
  }
}, { immediate: true });
function openMenu(menu: MenuKey) { cancelMenuClose(); activeMenu.value = menu; }
function toggleMenu(menu: MenuKey) { activeMenu.value = activeMenu.value === menu ? '' : menu; notificationOpen.value = false; }
function scheduleMenuClose() { cancelMenuClose(); menuTimer = window.setTimeout(() => { if (activeMenu.value !== 'account') activeMenu.value = ''; }, 160); }
function cancelMenuClose() { if (menuTimer) window.clearTimeout(menuTimer); }
async function toggleNotifications() {
  notificationOpen.value = !notificationOpen.value; activeMenu.value = '';
  if (!notificationOpen.value) return;
  await Promise.all([loadNotifications(), loadUserNotifications()]);
}
async function loadNotifications() {
  if (notificationsLoaded.value) return;
  notificationsLoading.value = true;
  try { const { data } = await http.get<SysNotice[]>('/api/notices'); notifications.value = data; } catch { notifications.value = []; } finally { notificationsLoaded.value = true; notificationsLoading.value = false; }
}
async function loadUserNotifications() {
  if (!auth.isAuthenticated || auth.currentUser?.role !== 'PLAYER') return;
  try { userNotifications.value = await listUserNotifications(); } catch { userNotifications.value = []; }
}
async function openUserNotification(notice: UserNotificationItem) {
  if (!notice.isRead) {
    try {
      const updated = await markNotificationRead(notice.notificationId);
      userNotifications.value = userNotifications.value.map((row) => row.notificationId === updated.notificationId ? updated : row);
    } catch { /* Keep navigation available if acknowledgement temporarily fails. */ }
  }
  notificationOpen.value = false;
  if (notice.targetUrl) await router.push(notice.targetUrl);
}
async function prepareStartupAnnouncement() {
  if (route.name !== 'store' || sessionStorage.getItem(startupDismissKey) === 'true') return;
  await loadNotifications();
  startupAnnouncementIndex.value = 0;
  startupAnnouncementOpen.value = true;
}
function openAnnouncement(index: number) {
  notificationOpen.value = false;
  startupAnnouncementIndex.value = index;
  startupAnnouncementOpen.value = true;
}
function dismissStartupAnnouncement() {
  startupAnnouncementOpen.value = false;
  sessionStorage.setItem(startupDismissKey, 'true');
}
function stepAnnouncement(direction: number) {
  const total = startupAnnouncements.value.length;
  if (!total) return;
  startupAnnouncementIndex.value = (startupAnnouncementIndex.value + direction + total) % total;
}
function openAnnouncementDetail() {
  const destination = currentAnnouncement.value?.route;
  dismissStartupAnnouncement();
  if (destination) router.push(destination);
}
function noticeRoute(notice: SysNotice) {
  const text = `${notice.title} ${notice.content}`.toLocaleLowerCase();
  if (text.includes('cs2') || text.includes('counter-strike')) return '/games/GAME_CS2';
  if (text.includes('饥荒') || text.includes("don't starve") || text.includes('dst')) return '/games/GAME_DST';
  return '/store';
}
async function openDownloads() {
  activeDrawer.value = 'downloads'; activeMenu.value = '';
  if (!auth.isAuthenticated) { await router.push({ name: 'login', query: { redirect: route.fullPath } }); activeDrawer.value = ''; return; }
  libraryLoading.value = true;
  try { libraryEntries.value = await getLibrary(); } catch { showToast('暂时无法读取下载列表，请稍后重试。'); } finally { libraryLoading.value = false; }
}
async function openFriends() {
  activeMenu.value = '';
  if (!auth.isAuthenticated || auth.currentUser?.role !== 'PLAYER') {
    await router.push({ name: 'login', query: { redirect: route.fullPath } });
    return;
  }
  activeDrawer.value = 'friends';
  await loadFriends();
}
async function loadFriends() {
  if (!auth.isAuthenticated || auth.currentUser?.role !== 'PLAYER') return;
  friendsLoading.value = true;
  try {
    friends.value = await listFriends();
    if (selectedFriendId.value && !friends.value.some((friend) => friend.userId === selectedFriendId.value)) selectedFriendId.value = '';
  } catch { showToast('好友列表同步失败，请稍后重试。'); }
  finally { friendsLoading.value = false; }
}
async function selectFriend(userId: string) {
  selectedFriendId.value = userId;
  chatMessages.value = [];
  try { chatMessages.value = await listMessages(userId); } catch { showToast('聊天记录同步失败，请稍后重试。'); }
}
async function approveFriend(relationId: string) {
  try { await acceptFriend(relationId); await loadFriends(); showToast('好友请求已接受。'); } catch { showToast('暂时无法接受好友请求。'); }
}
function relationLabel(friend: FriendListItem) { return friend.relationStatus === 'ACCEPTED' ? '在线' : friend.isIncomingRequest ? '请求添加你为好友' : '好友请求待确认'; }
function launchLibrary(gameId: string) { activeDrawer.value = ''; router.push({ name: 'game-library', params: { gameId } }); }
async function sendChat() {
  if (!selectedFriend.value || !chatDraft.value || sendingChat.value) return;
  const content = chatDraft.value;
  sendingChat.value = true;
  try {
    const message = await sendMessage(selectedFriend.value.userId, content);
    chatMessages.value = [...chatMessages.value, message];
    chatDraft.value = '';
    await loadFriends();
  } catch { showToast('消息发送失败，请稍后重试。'); }
  finally { sendingChat.value = false; }
}
function receiveRealtimeMessage(message: DirectMessageItem) {
  if (selectedFriendId.value === message.senderId && !chatMessages.value.some((row) => row.messageId === message.messageId)) chatMessages.value = [...chatMessages.value, message];
  void loadFriends();
  showToast(`${message.senderNickname} 发来一条新消息`);
}
function receiveRealtimeNotification(notice: UserNotificationItem) {
  if (!userNotifications.value.some((row) => row.notificationId === notice.notificationId)) userNotifications.value = [notice, ...userNotifications.value];
}
function receiveTradeOffer(offer: TradeOfferView, received: boolean) { showToast(received ? `${offer.senderNickname} 发来新的交易报价` : `交易报价状态已更新为${offer.status}`); }
function receiveDiscussionReply(topic: DiscussionTopicView) { showToast(`讨论“${topic.title}”收到新回复`); }
function scrollToTop() { window.scrollTo({ top: 0, behavior: 'smooth' }); showToast('已返回页面顶部'); }
function goToGames() { router.push(auth.isAuthenticated ? '/library' : '/store'); }
function showToast(text: string) { toast.value = text; if (toastTimer) window.clearTimeout(toastTimer); toastTimer = window.setTimeout(() => { toast.value = ''; }, 2400); }
function closeOverlays() { activeMenu.value = ''; activeDrawer.value = ''; notificationOpen.value = false; if (startupAnnouncementOpen.value) dismissStartupAnnouncement(); }
function logout() { void realtime.disconnect(); auth.logout(); closeOverlays(); router.push({ name: 'login' }); }

function handleAppToast(event: Event) {
  const customEvent = event as CustomEvent<string>;
  if (customEvent.detail) {
    showToast(customEvent.detail);
  }
}

function handleAuthUnauthorized() {
  void realtime.disconnect();
  auth.logout();
  closeOverlays();
}

onMounted(() => {
  window.addEventListener('app-toast', handleAppToast);
  window.addEventListener('auth-unauthorized', handleAuthUnauthorized);
});

onBeforeUnmount(() => {
  window.removeEventListener('app-toast', handleAppToast);
  window.removeEventListener('auth-unauthorized', handleAuthUnauthorized);
  void realtime.disconnect();
  cancelMenuClose();
  if (toastTimer) window.clearTimeout(toastTimer);
});
</script>
