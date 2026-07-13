<template>
  <section class="library-shell">
    <LibraryRail :entries="library" :active-game-id="gameId" />

    <main class="game-library-page" :style="heroStyle">
      <header class="game-hero">
        <div class="hero-spacer" />

        <section class="game-command-bar">
          <button class="play-button" type="button" :disabled="playing || !ownedEntry" @click="playGame">
            <Play :size="25" fill="currentColor" />
            <span>{{ playing ? '记录中...' : '开始游戏' }}</span>
          </button>
          <button class="play-menu" type="button" title="启动选项" aria-label="启动选项">
            <ChevronDown :size="18" />
          </button>

          <div class="game-identity">
            <img :src="game.coverImage" alt="" />
            <div>
              <strong>{{ game.title }}</strong>
              <span>{{ ownedEntry ? '已在库中' : '尚未入库' }}</span>
            </div>
          </div>

          <dl class="play-stats">
            <div>
              <dt><Cloud :size="15" /> 云状态</dt>
              <dd>{{ ownedEntry ? '已同步' : '无数据' }}</dd>
            </div>
            <div>
              <dt>最后运行日期</dt>
              <dd>{{ compactDate(ownedEntry?.lastPlayTime) }}</dd>
            </div>
            <div>
              <dt><Clock3 :size="15" /> 游戏时间</dt>
              <dd>{{ minutesText(ownedEntry?.playMinutes || 0) }}</dd>
            </div>
          </dl>

          <div class="command-tools">
            <button type="button" title="设置" aria-label="设置"><Settings :size="19" /></button>
            <button type="button" title="游戏信息" aria-label="游戏信息"><Info :size="19" /></button>
            <button type="button" title="收藏" aria-label="收藏"><Heart :size="20" fill="currentColor" /></button>
          </div>
        </section>

        <nav class="game-subnav" aria-label="游戏库功能">
          <RouterLink :to="{ name: 'game-detail', params: { gameId } }">商店页面</RouterLink>
          <RouterLink v-if="isDst" :to="{ name: 'game-detail', params: { gameId }, hash: '#packages' }">DLC</RouterLink>
          <RouterLink :to="{ name: 'game-community', params: { gameId } }">社区中心</RouterLink>
          <RouterLink :to="{ name: 'game-community', params: { gameId } }">讨论区</RouterLink>
          <RouterLink :to="{ name: 'game-community', params: { gameId }, query: { tab: 'achievements' } }">指南</RouterLink>
          <RouterLink v-if="isCs2" :to="{ name: 'inventory', query: { gameId } }">库存</RouterLink>
          <RouterLink v-if="isCs2" :to="{ name: 'market', query: { gameId } }">市场</RouterLink>
        </nav>
      </header>

      <p v-if="notice" class="library-notice">{{ notice }}</p>

      <section class="library-content">
        <div class="activity-feed">
          <article class="featured-update">
            <div class="featured-label">精选</div>
            <img :src="game.headerImage" :alt="game.title" />
            <div class="featured-copy">
              <small>新闻 · {{ game.updates[0]?.date || '最近' }}</small>
              <h1>{{ game.updates[0]?.title || game.title }}</h1>
              <p>{{ game.updates[0]?.body || game.libraryLine }}</p>
            </div>
          </article>

          <section class="activity-composer">
            <h2>动态</h2>
            <RouterLink :to="{ name: 'game-community', params: { gameId } }">
              将此游戏介绍给您的好友...
              <MessageSquare :size="16" />
            </RouterLink>
          </section>

          <article v-for="update in game.updates" :key="update.title" class="timeline-update">
            <time>{{ update.date }}</time>
            <div>
              <span class="update-icon"><Wrench :size="29" /></span>
              <section>
                <small>{{ update.type }}</small>
                <h2>{{ update.title }}</h2>
                <p>{{ update.body }}</p>
              </section>
            </div>
          </article>
        </div>

        <aside class="library-sidebar">
          <section class="sidebar-section friends-panel">
            <h2>玩过的好友</h2>
            <p><strong>5</strong> 位好友之前玩过</p>
            <div class="friend-row">
              <span v-for="friend in friends" :key="friend" :title="friend">{{ friend.slice(0, 1) }}</span>
            </div>
            <RouterLink :to="{ name: 'game-community', params: { gameId } }">查看玩过此游戏的所有好友</RouterLink>
          </section>

          <section class="sidebar-section notes-panel">
            <div class="sidebar-heading">
              <h2>笔记</h2>
              <NotebookPen :size="17" />
            </div>
            <textarea v-model="noteDraft" rows="3" placeholder="为这款游戏写一条私人笔记" />
            <button type="button" @click="saveNote">保存笔记</button>
            <small v-if="noteSaved">笔记已保存在此浏览器</small>
          </section>

          <section class="sidebar-section achievements-panel">
            <div class="sidebar-heading">
              <h2>成就</h2>
              <RouterLink :to="{ name: 'game-community', params: { gameId } }">查看全部</RouterLink>
            </div>
            <p>已解锁 {{ unlockedCount }}/{{ achievements.length }}（{{ achievementPercent }}%）</p>
            <div class="achievement-progress"><i :style="{ width: achievementPercent + '%' }" /></div>
            <div v-if="loading" class="sidebar-state">正在加载...</div>
            <div v-else class="achievement-row">
              <span
                v-for="achievement in achievementPreview"
                :key="achievement.achId"
                :class="{ locked: !achievement.isUnlocked }"
                :title="achievement.achName"
              >
                <img :src="achievement.iconUrl" :alt="achievement.achName" />
              </span>
            </div>
          </section>

          <section class="sidebar-section links-panel">
            <RouterLink v-if="isCs2" :to="{ name: 'inventory', query: { gameId } }"><Boxes :size="16" /> 我的库存</RouterLink>
            <RouterLink v-if="isCs2" :to="{ name: 'market', query: { gameId } }"><ShoppingCart :size="16" /> 饰品市场</RouterLink>
            <RouterLink :to="{ name: 'game-detail', params: { gameId } }"><ExternalLink :size="16" /> 商店页面</RouterLink>
            <RouterLink :to="{ name: 'game-community', params: { gameId } }"><Users :size="16" /> 社区中心</RouterLink>
          </section>
        </aside>
      </section>
    </main>
  </section>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import {
  Boxes,
  ChevronDown,
  Clock3,
  Cloud,
  ExternalLink,
  Heart,
  Info,
  MessageSquare,
  NotebookPen,
  Play,
  Settings,
  ShoppingCart,

  Users,
  Wrench
} from '@lucide/vue';
import { RouterLink, useRoute } from 'vue-router';
import { addPlaytime, getLibrary, type LibraryEntry } from '../api/coreApi';
import { listGameAchievements } from '../api/communityApi';
import { getApiError } from '../api/http';
import LibraryRail from '../components/LibraryRail.vue';
import { withAchievementIcons, type AchievementDisplayItem } from '../data/achievementCatalog';
import { getGameMeta } from '../data/gameCatalog';
import { minutesText } from '../utils/format';

const route = useRoute();
const gameId = computed(() => String(route.params.gameId || 'GAME_DST'));
const game = computed(() => getGameMeta(gameId.value));
const isCs2 = computed(() => gameId.value === 'GAME_CS2');
const isDst = computed(() => gameId.value === 'GAME_DST');
const heroStyle = computed(() => ({ '--library-hero': `url("${game.value.heroImage}")` }));

const library = ref<LibraryEntry[]>([]);
const achievements = ref<AchievementDisplayItem[]>([]);
const loading = ref(false);
const playing = ref(false);
const notice = ref('');
const noteDraft = ref('');
const noteSaved = ref(false);
const friends = ['Stephen', 'Alice', 'Bob', 'Klei', 'Valve'];

const ownedEntry = computed(() => library.value.find((entry) => entry.gameId === gameId.value) || null);
const unlockedCount = computed(() => achievements.value.filter((achievement) => achievement.isUnlocked).length);
const achievementPercent = computed(() => achievements.value.length === 0 ? 0 : Math.round((unlockedCount.value / achievements.value.length) * 100));
const achievementPreview = computed(() => achievements.value.slice(0, 5));

watch(gameId, loadLibrary, { immediate: true });

async function loadLibrary() {
  loading.value = true;
  notice.value = '';
  noteSaved.value = false;
  noteDraft.value = localStorage.getItem(noteStorageKey()) || '';

  try {
    library.value = await getLibrary();
  } catch (error) {
    notice.value = getApiError(error);
  }

  try {
    const achievementRows = await listGameAchievements(gameId.value);
    achievements.value = withAchievementIcons(achievementRows);
  } catch (error) {
    achievements.value = [];
    notice.value ||= getApiError(error);
  } finally {
    loading.value = false;
  }
}

async function playGame() {
  if (!ownedEntry.value || playing.value) return;
  playing.value = true;
  notice.value = '';
  try {
    const updated = await addPlaytime(gameId.value, 30);
    const index = library.value.findIndex((entry) => entry.gameId === gameId.value);
    if (index >= 0) library.value.splice(index, 1, updated);
    notice.value = `已为 ${updated.gameName} 记录 30 分钟课程演示游玩。`;
  } catch (error) {
    notice.value = getApiError(error);
  } finally {
    playing.value = false;
  }
}

function compactDate(value?: string | null) {
  if (!value) return '尚未运行';
  return new Intl.DateTimeFormat('zh-CN', { month: 'numeric', day: 'numeric' }).format(new Date(value));
}

function noteStorageKey() {
  return `steam-platform-library-note:${gameId.value}`;
}

function saveNote() {
  localStorage.setItem(noteStorageKey(), noteDraft.value.trim());
  noteSaved.value = true;
}
</script>

<style scoped>
.library-shell {
  display: grid;
  grid-template-columns: 268px minmax(0, 1fr);
  min-height: calc(100vh - 58px);
  background: #1b232e;
}

.game-library-page {
  --library-hero: none;
  min-width: 0;
  background:
    linear-gradient(180deg, rgba(18, 22, 28, 0.25) 0, #1b232c 510px),
    var(--library-hero) center top / 100% auto no-repeat,
    #1b232c;
}

.game-hero {
  min-height: 510px;
  padding-top: 318px;
}

.hero-spacer {
  display: none;
}

.game-command-bar {
  display: grid;
  grid-template-columns: 190px 34px minmax(180px, 1fr) auto auto;
  align-items: center;
  min-height: 72px;
  margin: 0 22px;
  background: linear-gradient(90deg, rgba(36, 44, 56, 0.98), rgba(40, 43, 51, 0.96));
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.36);
}

.play-button,
.play-menu {
  height: 48px;
  border: 0;
  color: #ffffff;
  background: linear-gradient(90deg, #65b933, #78d347);
  cursor: pointer;
}

.play-button {
  display: flex;
  gap: 10px;
  align-items: center;
  justify-content: center;
  margin-left: 8px;
  font-size: 1.08rem;
}

.play-menu {
  display: grid;
  width: 32px;
  place-items: center;
  border-left: 1px solid rgba(0, 0, 0, 0.18);
}

.play-button:disabled {
  filter: grayscale(0.8);
}

.game-identity {
  display: flex;
  gap: 10px;
  align-items: center;
  min-width: 0;
  padding: 0 14px;
}

.game-identity img {
  width: 42px;
  height: 42px;
  object-fit: cover;
}

.game-identity div {
  display: grid;
  min-width: 0;
}

.game-identity strong,
.game-identity span {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.game-identity span {
  color: #8f98a0;
  font-size: 0.76rem;
}

.play-stats {
  display: flex;
  gap: 24px;
  margin: 0;
  padding: 0 16px;
}

.play-stats div {
  min-width: 88px;
}

.play-stats dt,
.play-stats dd {
  display: flex;
  gap: 5px;
  align-items: center;
  margin: 0;
  text-transform: none;
  white-space: nowrap;
}

.play-stats dt {
  color: #77818f;
  font-size: 0.68rem;
}

.play-stats dd {
  color: #b8bec8;
  font-size: 0.78rem;
}

.command-tools {
  display: flex;
  gap: 5px;
  padding-right: 9px;
}

.command-tools button {
  display: grid;
  width: 34px;
  height: 34px;
  place-items: center;
  border: 0;
  border-radius: 2px;
  color: #98a3b1;
  background: rgba(255, 255, 255, 0.08);
  cursor: pointer;
}

.command-tools button:last-child {
  color: #66c0f4;
}

.game-subnav {
  display: flex;
  justify-content: center;
  min-height: 42px;
  margin: 0 22px;
  background: rgba(30, 38, 48, 0.96);
}

.game-subnav a {
  display: grid;
  min-width: 105px;
  place-items: center;
  color: #8994a2;
  font-size: 0.78rem;
}

.game-subnav a:hover {
  color: #ffffff;
  background: rgba(255, 255, 255, 0.06);
}

.library-notice {
  margin: 12px 22px 0;
  border-left: 3px solid #66c0f4;
  padding: 9px 12px;
  color: #c7d5e0;
  background: rgba(22, 31, 42, 0.94);
}

.library-content {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 310px;
  gap: 38px;
  align-items: start;
  max-width: 1050px;
  margin: 18px auto 0;
  padding: 0 20px 60px;
}

.activity-feed,
.library-sidebar {
  display: grid;
  gap: 18px;
}

.featured-update {
  display: grid;
  grid-template-columns: 42% minmax(0, 1fr);
  min-height: 180px;
  overflow: hidden;
  border-left: 3px solid #1a9fff;
  background: rgba(34, 44, 56, 0.96);
  box-shadow: 0 3px 12px rgba(0, 0, 0, 0.28);
}

.featured-label {
  grid-column: 1 / -1;
  padding: 4px 9px;
  color: #e8f5ff;
  background: linear-gradient(90deg, #148ed2, #14547d);
  font-size: 0.76rem;
}

.featured-update img {
  width: 100%;
  height: 100%;
  min-height: 145px;
  object-fit: cover;
}

.featured-copy {
  padding: 12px 14px;
}

.featured-copy small,
.featured-copy p {
  color: #8995a3;
}

.featured-copy h1,
.featured-copy p {
  margin: 0;
}

.featured-copy h1 {
  margin: 5px 0 8px;
  font-size: 1.08rem;
  font-weight: 500;
}

.featured-copy p {
  display: -webkit-box;
  overflow: hidden;
  font-size: 0.82rem;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 4;
}

.activity-composer {
  display: grid;
  gap: 8px;
}

.activity-composer h2 {
  margin: 0;
  color: #aab4c0;
  font-size: 0.9rem;
  font-weight: 500;
}

.activity-composer a {
  display: flex;
  justify-content: space-between;
  padding: 10px 12px;
  color: #718091;
  background: rgba(18, 25, 34, 0.9);
  font-size: 0.8rem;
}

.timeline-update > time {
  display: block;
  margin-bottom: 6px;
  color: #818b98;
  font-size: 0.78rem;
}

.timeline-update > div {
  display: grid;
  grid-template-columns: 60px minmax(0, 1fr);
  min-height: 105px;
  background: rgba(35, 44, 56, 0.86);
}

.update-icon {
  display: grid;
  place-items: center;
  color: #8c96a2;
  background: rgba(255, 255, 255, 0.035);
}

.timeline-update section {
  padding: 11px 14px;
}

.timeline-update small,
.timeline-update p {
  color: #84909e;
}

.timeline-update h2,
.timeline-update p {
  margin: 0;
}

.timeline-update h2 {
  margin: 2px 0 5px;
  font-size: 1rem;
  font-weight: 500;
}

.timeline-update p {
  font-size: 0.8rem;
}

.sidebar-section {
  border-top: 1px solid rgba(255, 255, 255, 0.1);
  padding-top: 10px;
}

.sidebar-section h2,
.sidebar-section p {
  margin: 0;
}

.sidebar-section h2 {
  color: #9aa5b2;
  font-size: 0.9rem;
  font-weight: 500;
}

.sidebar-section p,
.sidebar-section small {
  color: #818c99;
  font-size: 0.78rem;
}

.friends-panel p {
  margin: 8px 0;
}

.friends-panel p strong {
  color: #d5dae1;
  font-size: 1.25rem;
}

.friend-row,
.achievement-row {
  display: flex;
  gap: 6px;
  margin: 8px 0 10px;
}

.friend-row span,
.achievement-row span {
  display: grid;
  width: 38px;
  height: 38px;
  place-items: center;
  border-bottom: 2px solid #66c0f4;
  color: #e7eef6;
  background: linear-gradient(135deg, #466b85, #222c38);
  font-weight: 700;
}

.friends-panel a,
.sidebar-heading a {
  color: #8299ad;
  font-size: 0.76rem;
}

.sidebar-heading {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.notes-panel textarea {
  margin: 8px 0 6px;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 1px;
  padding: 8px;
  background: rgba(31, 40, 51, 0.86);
  font-size: 0.78rem;
  resize: vertical;
}

.notes-panel button {
  border: 0;
  padding: 5px 10px;
  color: #c9d4df;
  background: #34475a;
  cursor: pointer;
  font-size: 0.76rem;
}

.notes-panel small {
  margin-left: 8px;
}

.achievement-progress {
  height: 7px;
  margin: 8px 0;
  background: #0d1218;
}

.achievement-progress i {
  display: block;
  height: 100%;
  background: linear-gradient(90deg, #47a6d8, #70c8ef);
}

.achievement-row span {
  width: 42px;
  height: 42px;
  border: 0;
  color: #f0c768;
  background: #344454;
  overflow: hidden;
}

.achievement-row img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.achievement-row span.locked {
  filter: grayscale(1);
  opacity: 0.42;
}

.links-panel {
  display: grid;
  gap: 5px;
}

.links-panel a {
  display: flex;
  gap: 8px;
  align-items: center;
  min-height: 34px;
  padding: 0 9px;
  color: #a7b1bd;
  background: rgba(42, 55, 69, 0.76);
  font-size: 0.8rem;
}

@media (max-width: 1120px) {
  .game-command-bar {
    grid-template-columns: 170px 34px minmax(180px, 1fr) auto;
  }

  .play-stats {
    display: none;
  }

  .library-content {
    grid-template-columns: minmax(0, 1fr) 270px;
    gap: 24px;
  }
}

@media (max-width: 920px) {
  .library-shell {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 760px) {
  .game-library-page {
    background-size: auto 280px;
  }

  .game-hero {
    min-height: 360px;
    padding-top: 225px;
  }

  .game-command-bar {
    grid-template-columns: minmax(0, 1fr) 34px auto;
    margin: 0 10px;
  }

  .game-identity {
    display: none;
  }

  .command-tools button:not(:last-child) {
    display: none;
  }

  .game-subnav {
    justify-content: flex-start;
    margin: 0 10px;
    overflow-x: auto;
  }

  .game-subnav a {
    min-width: 92px;
  }

  .library-content {
    grid-template-columns: 1fr;
    padding: 0 12px 40px;
  }

  .featured-update {
    grid-template-columns: 1fr;
  }

  .featured-update img {
    max-height: 190px;
  }
}
</style>