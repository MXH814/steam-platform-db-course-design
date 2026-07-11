<template>
  <section class="library-shell">
    <LibraryRail :entries="library" />

    <main class="library-home">
      <header class="library-home-header">
        <div>
          <span>游戏库</span>
          <h1>主页</h1>
        </div>
        <div class="home-tools" aria-label="游戏库工具">
          <button type="button" title="筛选" aria-label="筛选">
            <SlidersHorizontal :size="17" />
          </button>
          <button type="button" title="排序" aria-label="排序">
            <ArrowDownUp :size="17" />
          </button>
        </div>
      </header>

      <p v-if="message" class="library-message success">{{ message }}</p>
      <p v-if="error" class="library-message error">{{ error }}</p>

      <section class="shelf-section news-shelf">
        <header class="shelf-heading">
          <div>
            <h2>新情报速递</h2>
            <Settings :size="15" />
          </div>
          <span>来自你的游戏和社区</span>
        </header>

        <div class="news-grid">
          <RouterLink v-for="story in newsStories" :key="story.title" class="news-tile" :to="story.to">
            <img :src="story.image" :alt="story.game" />
            <div>
              <small>{{ story.date }}</small>
              <h3>{{ story.title }}</h3>
              <span>{{ story.game }}</span>
            </div>
          </RouterLink>
        </div>
      </section>

      <section class="shelf-section recent-shelf">
        <header class="shelf-heading">
          <div>
            <h2>最近游戏</h2>
            <ChevronDown :size="16" />
          </div>
          <span>{{ library.length }} 款已入库游戏</span>
        </header>

        <div v-if="library.length" class="recent-grid">
          <article v-for="entry in sortedLibrary" :key="entry.libId" class="recent-game">
            <RouterLink :to="{ name: 'game-library', params: { gameId: entry.gameId } }">
              <img :src="gameMeta(entry.gameId).coverImage" :alt="entry.gameName" />
            </RouterLink>
            <div class="recent-game-info">
              <button type="button" :disabled="playingGameId === entry.gameId" @click="play(entry.gameId)">
                <Play :size="24" fill="currentColor" />
              </button>
              <div>
                <strong>{{ entry.gameName }}</strong>
                <span>游戏时间 {{ minutesText(entry.playMinutes) }}</span>
                <small>上次运行 {{ dateTime(entry.lastPlayTime) }}</small>
              </div>
            </div>
          </article>
        </div>

        <div v-else-if="!error" class="library-empty">
          <Gamepad2 :size="34" />
          <h2>游戏库还是空的</h2>
          <p>前往商店购买饥荒联机版，或免费领取 Counter-Strike 2。</p>
          <RouterLink to="/store">浏览商店</RouterLink>
        </div>
      </section>
    </main>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { ArrowDownUp, ChevronDown, Gamepad2, Play, Settings, SlidersHorizontal } from '@lucide/vue';
import { RouterLink } from 'vue-router';
import { addPlaytime, getLibrary, type LibraryEntry } from '../api/coreApi';
import { getApiError } from '../api/http';
import LibraryRail from '../components/LibraryRail.vue';
import { getGameMeta } from '../data/gameCatalog';
import { dateTime, minutesText } from '../utils/format';

const library = ref<LibraryEntry[]>([]);
const error = ref('');
const message = ref('');
const playingGameId = ref('');

const sortedLibrary = computed(() => [...library.value].sort((a, b) => {
  const left = a.lastPlayTime ? new Date(a.lastPlayTime).getTime() : 0;
  const right = b.lastPlayTime ? new Date(b.lastPlayTime).getTime() : 0;
  return right - left;
}));

const newsStories = [
  {
    date: '今天',
    game: '饥荒联机版',
    title: '盛夏鸦年华回归，新的联机生存旅程已经开始',
    image: '/assets/games/dst-header.jpg',
    to: '/library/GAME_DST'
  },
  {
    date: '本周',
    game: 'Counter-Strike 2',
    title: '库存、饰品掉落与市场交易数据现已联动',
    image: '/assets/games/cs2-header.jpg',
    to: '/library/GAME_CS2'
  },
  {
    date: '本周',
    game: '社区动态',
    title: '查看最新评价、成就进度和游戏更新记录',
    image: '/assets/games/dst-library-hero.jpg',
    to: '/games/GAME_DST/community'
  }
];

async function load() {
  error.value = '';
  try {
    library.value = await getLibrary();
  } catch (requestError) {
    error.value = getApiError(requestError);
  }
}

async function play(gameId: string) {
  error.value = '';
  message.value = '';
  playingGameId.value = gameId;
  try {
    const updated = await addPlaytime(gameId, 30);
    message.value = `${updated.gameName} 已记录一次 30 分钟的课程演示游玩。`;
    await load();
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    playingGameId.value = '';
  }
}

const gameMeta = getGameMeta;
onMounted(load);
</script>

<style scoped>
.library-shell {
  display: grid;
  grid-template-columns: 268px minmax(0, 1fr);
  min-height: calc(100vh - 58px);
  background: #1b232e;
}

.library-home {
  min-width: 0;
  padding: 18px 24px 56px;
  background:
    linear-gradient(180deg, rgba(37, 46, 59, 0.9), rgba(24, 31, 41, 0.97)),
    url('/assets/games/dst-library-hero.jpg') center top / cover fixed;
}

.library-home-header,
.shelf-heading,
.shelf-heading > div,
.home-tools,
.recent-game-info {
  display: flex;
  align-items: center;
}

.library-home-header {
  justify-content: space-between;
  margin-bottom: 18px;
}

.library-home-header span,
.library-home-header h1 {
  margin: 0;
}

.library-home-header span {
  color: #8d9aaa;
  font-size: 0.72rem;
  text-transform: uppercase;
}

.library-home-header h1 {
  font-size: 1.45rem;
  font-weight: 500;
}

.home-tools {
  gap: 5px;
}

.home-tools button {
  display: grid;
  width: 34px;
  height: 32px;
  place-items: center;
  border: 0;
  border-radius: 2px;
  color: #aeb8c5;
  background: #2b3543;
  cursor: pointer;
}

.library-message {
  margin: 0 0 14px;
  border-left: 3px solid currentColor;
  padding: 9px 12px;
  background: rgba(14, 20, 28, 0.82);
}

.library-message.success {
  color: #b8f36f;
}

.library-message.error {
  color: #ffb7b7;
}

.shelf-section {
  margin-bottom: 26px;
}

.shelf-heading {
  justify-content: space-between;
  min-height: 38px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.12);
}

.shelf-heading > div {
  gap: 7px;
}

.shelf-heading h2 {
  margin: 0;
  color: #dfe4ea;
  font-size: 1rem;
  font-weight: 500;
}

.shelf-heading span {
  color: #7f8a98;
  font-size: 0.78rem;
}

.news-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 14px;
  padding-top: 12px;
}

.news-tile {
  min-width: 0;
  color: inherit;
  background: #222d3a;
  text-decoration: none;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.28);
  transition: transform 160ms ease, background 160ms ease;
}

.news-tile:hover {
  background: #2a3949;
  transform: translateY(-2px);
}

.news-tile img {
  display: block;
  width: 100%;
  aspect-ratio: 460 / 215;
  object-fit: cover;
}

.news-tile div {
  display: grid;
  gap: 4px;
  padding: 9px 10px 11px;
}

.news-tile small,
.news-tile span {
  color: #798696;
  font-size: 0.72rem;
}

.news-tile h3 {
  margin: 0;
  overflow: hidden;
  color: #d9dfe7;
  font-size: 0.94rem;
  font-weight: 500;
  line-height: 1.35;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.recent-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(210px, 280px));
  gap: 16px;
  padding-top: 12px;
}

.recent-game {
  position: relative;
  overflow: hidden;
  background: #202a36;
  box-shadow: 0 5px 16px rgba(0, 0, 0, 0.36);
}

.recent-game > a,
.recent-game > a img {
  display: block;
  width: 100%;
}

.recent-game > a img {
  aspect-ratio: 2 / 3;
  object-fit: cover;
}

.recent-game-info {
  position: absolute;
  right: 0;
  bottom: 0;
  left: 0;
  gap: 12px;
  padding: 10px;
  background: rgba(38, 47, 57, 0.94);
  backdrop-filter: blur(8px);
}

.recent-game-info button {
  display: grid;
  flex: 0 0 52px;
  width: 52px;
  height: 52px;
  place-items: center;
  border: 0;
  border-radius: 2px;
  color: #ffffff;
  background: linear-gradient(135deg, #77d54c, #4a9d2a);
  cursor: pointer;
}

.recent-game-info button:disabled {
  filter: grayscale(0.8);
}

.recent-game-info div {
  display: grid;
  min-width: 0;
}

.recent-game-info strong,
.recent-game-info span,
.recent-game-info small {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.recent-game-info strong {
  font-size: 0.86rem;
}

.recent-game-info span,
.recent-game-info small {
  color: #9aa4b0;
  font-size: 0.72rem;
}

.library-empty {
  display: grid;
  justify-items: center;
  gap: 8px;
  margin-top: 12px;
  padding: 50px 20px;
  color: #8d9aaa;
  background: rgba(18, 25, 34, 0.78);
  text-align: center;
}

.library-empty h2,
.library-empty p {
  margin: 0;
}

.library-empty a {
  margin-top: 8px;
  padding: 7px 14px;
  color: #ffffff;
  background: #3d6f96;
  text-decoration: none;
}

@media (prefers-reduced-motion: reduce) {
  .news-tile {
    transition: none;
  }
}

@media (max-width: 1050px) {
  .news-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 920px) {
  .library-shell {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 640px) {
  .library-home {
    padding: 16px 12px 40px;
  }

  .news-grid {
    grid-template-columns: 1fr;
  }

  .recent-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 10px;
  }

  .recent-game-info {
    position: static;
  }

  .recent-game-info button {
    flex-basis: 40px;
    width: 40px;
    height: 40px;
  }
}
</style>
