<template>
  <section class="community-page">
    <header class="community-hero">
      <div><p>Game Deck 社区</p><h1>社区活动</h1><span>与好友、游戏群组和内容创作者保持联系。</span></div>
      <div v-if="auth.isAuthenticated" class="hero-links"><RouterLink to="/profile"><UserRound :size="16" />我的资料</RouterLink><RouterLink to="/trade-offers"><ArrowLeftRight :size="16" />交易报价</RouterLink></div>
    </header>

    <nav class="community-tabs">
      <button type="button" :class="{ active: activeTab === 'activity' }" @click="activeTab = 'activity'"><Activity :size="16" />活动</button>
      <button type="button" :class="{ active: activeTab === 'discussions' }" @click="activeTab = 'discussions'"><MessagesSquare :size="16" />讨论</button>
      <button type="button" :class="{ active: activeTab === 'people' }" @click="activeTab = 'people'"><Users :size="16" />寻找玩家</button>
    </nav>

    <div v-if="pageMessage" class="community-message" role="status">{{ pageMessage }}</div>

    <div v-if="activeTab === 'activity'" class="community-layout">
      <main>
        <form v-if="auth.isAuthenticated" class="post-composer" @submit.prevent="publishPost">
          <div class="composer-avatar">{{ currentInitial }}</div>
          <div>
            <textarea v-model.trim="postDraft.content" maxlength="1000" rows="3" placeholder="与社区分享游戏动态..." />
            <div class="composer-options">
              <select v-model="postDraft.gameId"><option :value="null">平台动态</option><option value="GAME_CS2">Counter-Strike 2</option><option value="GAME_DST">饥荒联机版</option></select>
              <select v-model="postDraft.postType"><option value="STATUS">状态</option><option value="ACHIEVEMENT">成就</option><option value="SCREENSHOT">截图</option><option value="TRADE">交易</option></select>
              <select v-model="postDraft.visibility"><option value="PUBLIC">所有人可见</option><option value="FRIENDS">仅好友可见</option></select>
              <button type="submit" :disabled="publishing || !postDraft.content"><Send :size="15" />发布</button>
            </div>
          </div>
        </form>

        <div v-if="loadingPosts" class="panel-state">正在载入社区活动...</div>
        <div v-else-if="!posts.length" class="panel-state">还没有社区动态。</div>
        <article v-for="post in posts" :key="post.postId" class="activity-card">
          <RouterLink class="activity-avatar" :class="avatarClass(post.avatarKey)" :to="`/profiles/${post.authorId}`">{{ post.authorNickname.slice(0, 1) }}</RouterLink>
          <div class="activity-content">
            <header><div><RouterLink :to="`/profiles/${post.authorId}`">{{ post.authorNickname }}</RouterLink><span>{{ postTypeLabel(post.postType) }}<template v-if="post.gameName"> · {{ post.gameName }}</template></span></div><time>{{ relativeTime(post.createdAt) }}</time></header>
            <p>{{ post.content }}</p>
            <img v-if="post.mediaUrl" class="post-media" :src="post.mediaUrl" alt="社区动态媒体" />
            <footer>
              <button type="button" :class="{ active: post.myReaction === 'LIKE' }" @click="react(post, post.myReaction === 'LIKE' ? null : 'LIKE')"><ThumbsUp :size="15" />赞 {{ post.likeCount }}</button>
              <button type="button" :class="{ active: post.myReaction === 'AWARD' }" @click="react(post, post.myReaction === 'AWARD' ? null : 'AWARD')"><Award :size="15" />奖励 {{ post.awardCount }}</button>
              <span><Globe2 v-if="post.visibility === 'PUBLIC'" :size="14" /><Users v-else :size="14" />{{ post.visibility === 'PUBLIC' ? '公开' : '好友' }}</span>
            </footer>
          </div>
        </article>
      </main>

      <aside>
        <section class="side-panel"><header><strong>游戏中心</strong></header><RouterLink to="/games/GAME_CS2/community"><img src="/assets/games/cs2-header.jpg" alt="" /><span>Counter-Strike 2<small>评测、成就与工坊</small></span></RouterLink><RouterLink to="/games/GAME_DST/community"><img src="/assets/games/dst-header.jpg" alt="" /><span>饥荒联机版<small>评测、成就与工坊</small></span></RouterLink></section>
        <section v-if="auth.isAuthenticated" class="side-panel"><header><strong>好友</strong><button type="button" @click="activeTab = 'people'">查找</button></header><RouterLink v-for="friend in acceptedFriends.slice(0, 5)" :key="friend.userId" :to="`/profiles/${friend.userId}`"><span class="mini-avatar">{{ friend.nickname.slice(0, 1) }}</span><span>{{ friend.nickname }}<small>在线</small></span></RouterLink><p v-if="!acceptedFriends.length">还没有已接受的好友。</p></section>
      </aside>
    </div>

    <div v-else-if="activeTab === 'discussions'" class="discussion-layout">
      <main>
        <header class="discussion-toolbar">
          <div class="game-segment"><button type="button" :class="{ active: discussionGameId === 'GAME_CS2' }" @click="selectDiscussionGame('GAME_CS2')">Counter-Strike 2</button><button type="button" :class="{ active: discussionGameId === 'GAME_DST' }" @click="selectDiscussionGame('GAME_DST')">饥荒联机版</button></div>
          <button v-if="auth.isAuthenticated" type="button" @click="topicComposerOpen = !topicComposerOpen"><Plus :size="15" />发起新讨论</button>
        </header>
        <form v-if="topicComposerOpen" class="topic-composer" @submit.prevent="publishTopic"><input v-model.trim="topicDraft.title" maxlength="160" placeholder="讨论标题" /><textarea v-model.trim="topicDraft.body" maxlength="4000" rows="5" placeholder="详细描述你的问题或观点" /><footer><button type="button" @click="topicComposerOpen = false">取消</button><button type="submit" :disabled="!topicDraft.title || !topicDraft.body || publishingTopic">发布讨论</button></footer></form>
        <div v-if="loadingTopics" class="panel-state">正在读取讨论区...</div>
        <div v-else class="topic-list">
          <button v-for="topic in topics" :key="topic.topicId" type="button" :class="{ selected: selectedTopic?.topicId === topic.topicId }" @click="openTopic(topic.topicId)">
            <span class="topic-icon"><MessageSquare :size="20" /></span><span><strong>{{ topic.title }}</strong><small>{{ topic.authorNickname }} · {{ relativeTime(topic.updatedAt) }}</small><p>{{ topic.body }}</p></span><b>{{ topic.replyCount }}<small>回复</small></b>
          </button>
          <p v-if="!topics.length">该游戏还没有讨论主题。</p>
        </div>
      </main>
      <aside class="discussion-detail">
        <div v-if="!selectedTopic" class="panel-state">选择一个主题查看完整讨论。</div>
        <template v-else>
          <header><small>{{ selectedTopic.gameName }} 讨论区</small><h2>{{ selectedTopic.title }}</h2><span>由 <RouterLink :to="`/profiles/${selectedTopic.authorId}`">{{ selectedTopic.authorNickname }}</RouterLink> 发布</span></header>
          <article class="topic-root"><p>{{ selectedTopic.body }}</p><time>{{ relativeTime(selectedTopic.createdAt) }}</time></article>
          <article v-for="reply in selectedTopic.replies" :key="reply.replyId" class="topic-reply"><RouterLink :to="`/profiles/${reply.authorId}`" :class="['reply-avatar', avatarClass(reply.avatarKey)]">{{ reply.authorNickname.slice(0, 1) }}</RouterLink><div><header><RouterLink :to="`/profiles/${reply.authorId}`">{{ reply.authorNickname }}</RouterLink><time>{{ relativeTime(reply.createdAt) }}</time></header><p>{{ reply.body }}</p></div></article>
          <form v-if="auth.isAuthenticated && selectedTopic.status === 'OPEN'" class="reply-form" @submit.prevent="publishReply"><textarea v-model.trim="replyDraft" maxlength="4000" rows="3" placeholder="回复这个主题" /><button type="submit" :disabled="!replyDraft || publishingReply"><Send :size="15" />回复</button></form>
          <RouterLink v-else-if="!auth.isAuthenticated" class="login-prompt" :to="{ name: 'login', query: { redirect: route.fullPath } }">登录后参与讨论</RouterLink>
        </template>
      </aside>
    </div>

    <div v-else class="people-layout">
      <main>
        <header><div><h2>寻找玩家</h2><span>按昵称或账号搜索社区成员</span></div><label><Search :size="17" /><input v-model.trim="playerQuery" type="search" placeholder="搜索玩家" @input="schedulePlayerSearch" /></label></header>
        <div v-if="searchingPlayers" class="panel-state">正在搜索社区成员...</div>
        <div v-else class="player-grid">
          <article v-for="player in playerResults" :key="player.userId"><RouterLink :class="['player-avatar', avatarClass(player.avatarKey)]" :to="`/profiles/${player.userId}`">{{ player.nickname.slice(0, 1) }}</RouterLink><div><RouterLink :to="`/profiles/${player.userId}`">{{ player.nickname }}</RouterLink><p>{{ player.headline || '这位玩家还没有个人签名。' }}</p><span>{{ relationLabel(player) }}</span></div><button v-if="!player.relationStatus" type="button" @click="sendFriendRequest(player)"><UserPlus :size="15" />添加好友</button><button v-else-if="player.isIncomingRequest" type="button" @click="approvePlayer(player)"><Check :size="15" />接受</button><RouterLink v-else-if="player.relationStatus === 'ACCEPTED'" :to="{ name: 'trade-offers', query: { recipient: player.userId } }"><ArrowLeftRight :size="15" />交易</RouterLink></article>
          <p v-if="!playerResults.length">没有找到匹配的玩家。</p>
        </div>
      </main>
      <aside><section class="friend-summary"><Users :size="24" /><strong>{{ acceptedFriends.length }}</strong><span>位好友</span></section><section class="request-list"><header><strong>待处理请求</strong></header><article v-for="friend in incomingFriends" :key="friend.relationId"><span class="mini-avatar">{{ friend.nickname.slice(0, 1) }}</span><div><strong>{{ friend.nickname }}</strong><small>希望添加你为好友</small></div><button type="button" @click="approveRelation(friend.relationId)"><Check :size="15" /></button></article><p v-if="!incomingFriends.length">没有待处理请求。</p></section></aside>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { Activity, ArrowLeftRight, Award, Check, Globe2, MessageSquare, MessagesSquare, Plus, Search, Send, ThumbsUp, UserPlus, UserRound, Users } from '@lucide/vue';
import { useRoute, useRouter } from 'vue-router';
import { createCommunityPost, createDiscussionTopic, getDiscussionTopic, listCommunityPosts, listDiscussionTopics, replyToDiscussion, searchPlayers, setCommunityPostReaction } from '../api/engagementApi';
import { acceptFriend, listFriends, requestFriend } from '../api/socialApi';
import type { CommunityPostView, DiscussionTopicView, FriendListItem, PlayerSearchItem, ProfileAvatarKey } from '../api/types';
import { useAuthStore } from '../stores/auth';

const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const activeTab = ref<'activity' | 'discussions' | 'people'>('activity');
const posts = ref<CommunityPostView[]>([]);
const topics = ref<DiscussionTopicView[]>([]);
const selectedTopic = ref<DiscussionTopicView | null>(null);
const friends = ref<FriendListItem[]>([]);
const playerResults = ref<PlayerSearchItem[]>([]);
const loadingPosts = ref(false);
const loadingTopics = ref(false);
const searchingPlayers = ref(false);
const publishing = ref(false);
const publishingTopic = ref(false);
const publishingReply = ref(false);
const topicComposerOpen = ref(false);
const discussionGameId = ref<'GAME_CS2' | 'GAME_DST'>('GAME_CS2');
const playerQuery = ref('');
const replyDraft = ref('');
const pageMessage = ref('');
const postDraft = reactive<{ gameId: string | null; postType: 'STATUS' | 'ACHIEVEMENT' | 'SCREENSHOT' | 'TRADE'; content: string; visibility: 'PUBLIC' | 'FRIENDS' }>({ gameId: null, postType: 'STATUS', content: '', visibility: 'PUBLIC' });
const topicDraft = reactive({ title: '', body: '' });
const acceptedFriends = computed(() => friends.value.filter((friend) => friend.relationStatus === 'ACCEPTED'));
const incomingFriends = computed(() => friends.value.filter((friend) => friend.isIncomingRequest));
const currentInitial = computed(() => auth.currentUser?.account?.slice(0, 1).toUpperCase() || '?');
let playerSearchTimer: number | undefined;

onMounted(async () => {
  if (route.query.tab === 'people' || route.query.tab === 'discussions') activeTab.value = route.query.tab;
  await Promise.all([loadPosts(), loadFriends(), loadPlayers()]);
  const topicId = typeof route.params.topicId === 'string' ? route.params.topicId : '';
  if (topicId) { activeTab.value = 'discussions'; await openTopic(topicId, false); }
});
watch(activeTab, async (tab) => { if (tab === 'discussions' && !topics.value.length) await loadTopics(); if (tab === 'people') await loadPlayers(); });

async function loadPosts() { loadingPosts.value = true; try { posts.value = await listCommunityPosts(); } catch (reason) { showMessage(reason); } finally { loadingPosts.value = false; } }
async function loadFriends() { if (!auth.isAuthenticated) return; try { friends.value = await listFriends(); } catch (reason) { showMessage(reason); } }
async function loadPlayers() { if (!auth.isAuthenticated) return; searchingPlayers.value = true; try { playerResults.value = await searchPlayers(playerQuery.value); } catch (reason) { showMessage(reason); } finally { searchingPlayers.value = false; } }
async function loadTopics() { loadingTopics.value = true; try { topics.value = await listDiscussionTopics(discussionGameId.value); } catch (reason) { showMessage(reason); } finally { loadingTopics.value = false; } }

async function publishPost() {
  if (!postDraft.content || publishing.value) return;
  publishing.value = true;
  try { const post = await createCommunityPost({ ...postDraft, mediaUrl: null }); posts.value = [post, ...posts.value]; postDraft.content = ''; showMessage('动态已发布。'); }
  catch (reason) { showMessage(reason); } finally { publishing.value = false; }
}

async function react(post: CommunityPostView, reaction: 'LIKE' | 'AWARD' | null) {
  if (!auth.isAuthenticated) return void router.push({ name: 'login', query: { redirect: route.fullPath } });
  try { const updated = await setCommunityPostReaction(post.postId, reaction); posts.value = posts.value.map((row) => row.postId === updated.postId ? updated : row); }
  catch (reason) { showMessage(reason); }
}

async function selectDiscussionGame(gameId: 'GAME_CS2' | 'GAME_DST') { discussionGameId.value = gameId; selectedTopic.value = null; await loadTopics(); }
async function openTopic(topicId: string, updateRoute = true) {
  try { selectedTopic.value = await getDiscussionTopic(topicId); discussionGameId.value = selectedTopic.value.gameId as 'GAME_CS2' | 'GAME_DST'; if (!topics.value.length) await loadTopics(); if (updateRoute) await router.replace({ name: 'discussion-topic', params: { topicId } }); }
  catch (reason) { showMessage(reason); }
}

async function publishTopic() {
  if (!topicDraft.title || !topicDraft.body || publishingTopic.value) return;
  publishingTopic.value = true;
  try { const topic = await createDiscussionTopic({ gameId: discussionGameId.value, title: topicDraft.title, body: topicDraft.body }); topicDraft.title = ''; topicDraft.body = ''; topicComposerOpen.value = false; await loadTopics(); await openTopic(topic.topicId); }
  catch (reason) { showMessage(reason); } finally { publishingTopic.value = false; }
}

async function publishReply() {
  if (!selectedTopic.value || !replyDraft.value || publishingReply.value) return;
  publishingReply.value = true;
  try { selectedTopic.value = await replyToDiscussion(selectedTopic.value.topicId, replyDraft.value); replyDraft.value = ''; await loadTopics(); }
  catch (reason) { showMessage(reason); } finally { publishingReply.value = false; }
}

function schedulePlayerSearch() { if (playerSearchTimer) window.clearTimeout(playerSearchTimer); playerSearchTimer = window.setTimeout(loadPlayers, 250); }
async function sendFriendRequest(player: PlayerSearchItem) { try { await requestFriend(player.userId); await Promise.all([loadPlayers(), loadFriends()]); showMessage('好友请求已发送。'); } catch (reason) { showMessage(reason); } }
async function approvePlayer(player: PlayerSearchItem) { if (player.relationId) await approveRelation(player.relationId); }
async function approveRelation(relationId: string) { try { await acceptFriend(relationId); await Promise.all([loadPlayers(), loadFriends()]); showMessage('好友请求已接受。'); } catch (reason) { showMessage(reason); } }
function avatarClass(key: ProfileAvatarKey) { return key.toLowerCase().replace(/_/g, '-'); }
function postTypeLabel(value: string) { return ({ STATUS: '发布了状态', ACHIEVEMENT: '解锁了成就', SCREENSHOT: '分享了截图', TRADE: '完成了交易' } as Record<string, string>)[value] || '发布了动态'; }
function relationLabel(player: PlayerSearchItem) { if (player.relationStatus === 'ACCEPTED') return '已经是好友'; if (player.isIncomingRequest) return '请求添加你为好友'; if (player.relationStatus === 'PENDING') return '请求等待确认'; return '尚未添加好友'; }
function relativeTime(value: string) { const seconds = Math.max(1, Math.round((Date.now() - new Date(value).getTime()) / 1000)); if (seconds < 60) return `${seconds} 秒前`; if (seconds < 3600) return `${Math.floor(seconds / 60)} 分钟前`; if (seconds < 86400) return `${Math.floor(seconds / 3600)} 小时前`; return new Date(value).toLocaleDateString('zh-CN'); }
function showMessage(reason: unknown) { pageMessage.value = reason instanceof Error ? reason.message : String(reason); window.setTimeout(() => { pageMessage.value = ''; }, 2600); }
</script>

<style scoped>
.community-page { width: min(1160px, calc(100% - 32px)); margin: 18px auto 72px; color: #c7d5e0; }
.community-hero { display: flex; min-height: 180px; align-items: flex-end; justify-content: space-between; gap: 18px; padding: 25px 28px; background: #142638 url('/assets/games/dst-library-hero.jpg') center/cover; box-shadow: inset 0 0 0 1000px rgba(9, 19, 29, .68); border-bottom: 2px solid #66c0f4; }
.community-hero p { margin: 0; color: #66c0f4; font-size: .75rem; text-transform: uppercase; }
.community-hero h1 { margin: 2px 0; color: #fff; font-size: 2.1rem; font-weight: 400; }
.community-hero span { color: #b8c6d2; }
.hero-links { display: flex; gap: 8px; }
.hero-links a { display: inline-flex; align-items: center; gap: 7px; padding: 8px 11px; color: #d6e7f2; background: rgba(36, 65, 88, .92); }
.hero-links a:hover { background: #417a9b; }
.community-tabs { display: flex; border-bottom: 1px solid #3a4e61; background: #152536; }
.community-tabs button { display: inline-flex; align-items: center; gap: 7px; border: 0; border-bottom: 3px solid transparent; padding: 11px 18px; color: #8f98a0; background: transparent; cursor: pointer; }
.community-tabs button.active, .community-tabs button:hover { border-bottom-color: #66c0f4; color: #fff; background: #1f3549; }
.community-message { position: fixed; z-index: 80; right: 22px; bottom: 48px; max-width: 360px; padding: 10px 15px; color: #fff; background: #294963; box-shadow: 0 8px 24px rgba(0, 0, 0, .4); }
.community-layout, .people-layout { display: grid; grid-template-columns: minmax(0, 1fr) 280px; gap: 18px; margin-top: 18px; }
.post-composer { display: grid; grid-template-columns: 46px minmax(0, 1fr); gap: 12px; margin-bottom: 12px; padding: 14px; background: #1b2d3e; border: 1px solid #30465a; }
.composer-avatar, .activity-avatar, .player-avatar, .reply-avatar, .mini-avatar { display: grid; place-items: center; color: #fff; background: #315873; }
.composer-avatar { width: 46px; height: 46px; font-size: 1.2rem; }
.post-composer textarea { width: 100%; border: 1px solid #2d4358; padding: 9px; color: #fff; background: #101a25; resize: vertical; }
.composer-options { display: flex; flex-wrap: wrap; gap: 7px; margin-top: 7px; }
.composer-options select { border: 0; padding: 5px 8px; color: #c7d5e0; background: #273b4e; }
.composer-options button, .discussion-toolbar > button, .topic-composer footer button, .reply-form button { display: inline-flex; align-items: center; gap: 6px; margin-left: auto; border: 0; padding: 7px 12px; color: #fff; background: #417a9b; cursor: pointer; }
.activity-card { display: grid; grid-template-columns: 48px minmax(0, 1fr); gap: 12px; margin-bottom: 10px; padding: 14px; background: #1b2d3e; border: 1px solid transparent; }
.activity-card:hover { border-color: #38536b; }
.activity-avatar { width: 48px; height: 48px; font-size: 1.25rem; }
.avatar-orange { background: #9b542b; }.avatar-green { background: #3f6c42; }.avatar-purple { background: #60447c; }
.activity-content > header { display: flex; justify-content: space-between; gap: 8px; }
.activity-content > header a { color: #66c0f4; }.activity-content > header span, .activity-content time { color: #788b9d; font-size: .75rem; }
.activity-content p { color: #d6d7d8; white-space: pre-wrap; }
.post-media { width: 100%; max-height: 420px; object-fit: cover; }
.activity-content footer { display: flex; align-items: center; gap: 5px; border-top: 1px solid #2d4052; padding-top: 8px; }
.activity-content footer button { display: inline-flex; align-items: center; gap: 5px; border: 0; padding: 5px 8px; color: #8f98a0; background: #21364a; cursor: pointer; }
.activity-content footer button.active, .activity-content footer button:hover { color: #fff; background: #3b647f; }
.activity-content footer > span { display: inline-flex; align-items: center; gap: 4px; margin-left: auto; color: #748698; font-size: .72rem; }
.side-panel, .friend-summary, .request-list { margin-bottom: 12px; background: #1b2d3e; border: 1px solid #30465a; }
.side-panel > header, .request-list > header { display: flex; align-items: center; justify-content: space-between; padding: 10px 12px; color: #fff; background: #152536; }
.side-panel > header strong, .request-list strong { font-weight: 400; }.side-panel > header button { border: 0; color: #66c0f4; background: transparent; cursor: pointer; }
.side-panel > a { display: flex; gap: 9px; padding: 9px 10px; border-bottom: 1px solid #263a4d; }
.side-panel > a:hover { background: #253e53; }.side-panel img, .mini-avatar { width: 38px; height: 38px; object-fit: cover; }.side-panel span { min-width: 0; color: #fff; }.side-panel small { display: block; color: #7d90a2; }.side-panel p, .request-list > p { padding: 12px; color: #7d90a2; }
.panel-state { display: grid; min-height: 240px; place-content: center; color: #7d90a2; text-align: center; }
.discussion-layout { display: grid; grid-template-columns: minmax(430px, .9fr) minmax(0, 1.1fr); gap: 14px; margin-top: 18px; }
.discussion-layout > main, .discussion-detail { min-width: 0; background: #17293a; border: 1px solid #30465a; }
.discussion-toolbar { display: flex; align-items: center; justify-content: space-between; gap: 10px; padding: 11px; background: #10202e; }
.game-segment { display: flex; }.game-segment button { border: 0; padding: 7px 10px; color: #8f98a0; background: #26394a; cursor: pointer; }.game-segment button.active { color: #fff; background: #417a9b; }
.topic-composer { display: grid; gap: 8px; padding: 12px; border-bottom: 1px solid #30465a; }.topic-composer input, .topic-composer textarea, .reply-form textarea { border: 1px solid #30465a; padding: 8px; color: #fff; background: #0e1924; resize: vertical; }.topic-composer footer { display: flex; justify-content: flex-end; gap: 7px; }
.topic-list > button { display: grid; width: 100%; grid-template-columns: 38px minmax(0, 1fr) 48px; gap: 9px; border: 0; border-bottom: 1px solid #293e52; padding: 12px; color: #c7d5e0; background: transparent; text-align: left; cursor: pointer; }.topic-list > button:hover, .topic-list > button.selected { background: #253e53; }.topic-icon { display: grid; width: 36px; height: 36px; place-items: center; color: #66c0f4; background: #10202e; }.topic-list strong, .topic-list small { display: block; }.topic-list strong { color: #fff; font-weight: 400; }.topic-list small, .topic-list p { color: #7d90a2; font-size: .72rem; }.topic-list p { overflow: hidden; margin: 4px 0 0; text-overflow: ellipsis; white-space: nowrap; }.topic-list > button > b { color: #66c0f4; font-size: 1.05rem; font-weight: 400; text-align: center; }.topic-list > p { padding: 30px; color: #7d90a2; text-align: center; }
.discussion-detail > header { padding: 16px; background: #10202e; }.discussion-detail > header small { color: #66c0f4; }.discussion-detail h2 { margin: 3px 0; color: #fff; font-size: 1.25rem; font-weight: 400; }.discussion-detail > header span { color: #7d90a2; font-size: .78rem; }.discussion-detail a { color: #66c0f4; }
.topic-root { margin: 12px; padding: 14px; background: #1e3347; }.topic-root p, .topic-reply p { white-space: pre-wrap; }.topic-root time { color: #788b9d; font-size: .72rem; }
.topic-reply { display: grid; grid-template-columns: 38px minmax(0, 1fr); gap: 10px; margin: 0 12px; padding: 12px 0; border-top: 1px solid #2b4154; }.reply-avatar { width: 38px; height: 38px; }.topic-reply header { display: flex; justify-content: space-between; }.topic-reply time { color: #718496; font-size: .72rem; }.topic-reply p { margin: 6px 0; }
.reply-form { display: grid; gap: 8px; margin: 12px; }.reply-form button { justify-self: end; }.login-prompt { display: block; margin: 12px; padding: 9px; background: #203a50; text-align: center; }
.people-layout > main, .people-layout aside > section { background: #17293a; border: 1px solid #30465a; }.people-layout > main > header { display: flex; align-items: center; justify-content: space-between; gap: 15px; padding: 16px; background: #10202e; }.people-layout h2 { margin: 0; color: #fff; font-weight: 400; }.people-layout header span { color: #7d90a2; font-size: .78rem; }.people-layout header label { display: flex; align-items: center; gap: 7px; padding: 7px 9px; color: #66c0f4; background: #0b151f; }.people-layout header input { border: 0; color: #fff; background: transparent; outline: 0; }
.player-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 8px; padding: 12px; }.player-grid article { display: grid; grid-template-columns: 52px minmax(0, 1fr) auto; align-items: center; gap: 10px; padding: 10px; background: #20364a; }.player-avatar { width: 52px; height: 52px; font-size: 1.3rem; }.player-grid article > div > a { color: #fff; }.player-grid p { overflow: hidden; margin: 2px 0; color: #8f98a0; font-size: .75rem; text-overflow: ellipsis; white-space: nowrap; }.player-grid span { color: #6fa9cc; font-size: .7rem; }.player-grid button, .player-grid article > a:last-child { display: inline-flex; align-items: center; gap: 5px; border: 0; padding: 6px 8px; color: #fff; background: #417a9b; cursor: pointer; }.player-grid > p { grid-column: 1 / -1; padding: 30px; text-align: center; }
.friend-summary { display: grid; grid-template-columns: 32px 1fr; padding: 15px; color: #66c0f4; }.friend-summary strong { color: #fff; font-size: 1.35rem; font-weight: 400; }.friend-summary span { grid-column: 2; color: #8f98a0; }.request-list article { display: grid; grid-template-columns: 38px minmax(0, 1fr) 30px; align-items: center; gap: 8px; padding: 9px; }.request-list small { display: block; color: #7d90a2; }.request-list button { display: grid; width: 28px; height: 28px; place-items: center; border: 0; color: #fff; background: #4c7a31; cursor: pointer; }
@media (max-width: 860px) { .community-layout, .people-layout, .discussion-layout { grid-template-columns: 1fr; }.discussion-detail { min-height: 300px; }.player-grid { grid-template-columns: 1fr; } }
@media (max-width: 600px) { .community-page { width: 100%; margin-top: 0; }.community-hero { align-items: stretch; flex-direction: column; }.hero-links { flex-wrap: wrap; }.community-tabs { overflow-x: auto; }.community-tabs button { flex: 0 0 auto; }.post-composer { grid-template-columns: 1fr; }.composer-avatar { display: none; }.people-layout > main > header { align-items: stretch; flex-direction: column; }.player-grid article { grid-template-columns: 48px minmax(0, 1fr); }.player-grid article > button, .player-grid article > a:last-child { grid-column: 2; justify-self: start; } }
</style>
