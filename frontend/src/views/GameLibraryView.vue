<template>
  <section class="steam-library-page">
    <aside class="library-rail">
      <div class="rail-home">我的游戏库</div>
      <div class="rail-filter">已入库游戏</div>
      <RouterLink class="rail-search" to="/store">返回商店</RouterLink>
      <ul>
        <li v-for="entry in libraryNavEntries" :key="entry.gameId" :class="{ active: entry.gameId === gameId }">
          <RouterLink :to="{ name: 'game-library', params: { gameId: entry.gameId } }">
            <span>{{ entry.shortName }}</span>
            {{ entry.title }}
          </RouterLink>
        </li>
      </ul>
    </aside>

    <main class="library-detail">
      <header class="library-hero" :class="`tone-${game.heroTone}`">
        <button class="install-button" type="button" @click="addMinutes(30)">增加 30 分钟</button>
        <div class="library-game-title">
          <span>{{ game.capsuleLabel }}</span>
          <div>
            <h1>{{ game.title }}</h1>
            <p>{{ ownedEntry ? '已在库中' : '暂未确认入库状态' }}</p>
          </div>
        </div>
        <div class="library-actions">
          <RouterLink :to="{ name: 'game-detail', params: { gameId } }">商店页面</RouterLink>
          <RouterLink :to="{ name: 'game-community', params: { gameId } }">评价与成就</RouterLink>
        </div>
      </header>

      <div v-if="notice" class="library-notice">{{ notice }}</div>

      <section class="library-grid">
        <div class="activity-feed">
          <section class="library-panel stats-panel">
            <h2>我的游戏数据</h2>
            <dl>
              <div>
                <dt>总游玩时长</dt>
                <dd>{{ playtimeText }}</dd>
              </div>
              <div>
                <dt>最近游玩</dt>
                <dd>{{ lastPlayText }}</dd>
              </div>
              <div>
                <dt>入库方式</dt>
                <dd>{{ ownedEntry?.acquireWay || '等待接口确认' }}</dd>
              </div>
              <div>
                <dt>库状态</dt>
                <dd>{{ ownedEntry?.status || '未确认' }}</dd>
              </div>
            </dl>
          </section>

          <section class="library-panel owned-actions">
            <h2>{{ game.shortName }} 已入库入口</h2>
            <div class="entry-grid">
              <RouterLink :to="{ name: 'game-community', params: { gameId } }">社区评价</RouterLink>
              <RouterLink :to="{ name: 'game-community', params: { gameId }, query: { tab: 'achievements' } }">我的成就</RouterLink>
              <RouterLink v-if="isCs2" :to="{ name: 'inventory', query: { gameId } }">我的库存</RouterLink>
              <RouterLink v-if="isCs2" :to="{ name: 'market', query: { gameId } }">饰品市场</RouterLink>
              <RouterLink v-if="isDst" :to="{ name: 'game-detail', params: { gameId }, hash: '#packages' }">DLC / 礼包</RouterLink>
              <RouterLink v-if="isDst" :to="{ name: 'store' }">更新公告</RouterLink>
            </div>
          </section>

          <article v-for="update in game.updates" :key="update.title" class="activity-card">
            <div class="activity-art">{{ game.shortName }}</div>
            <div>
              <small>{{ update.date }}</small>
              <p>{{ update.type }}</p>
              <h2>{{ update.title }}</h2>
              <p>{{ update.body }}</p>
            </div>
          </article>
        </div>

        <aside class="library-sidebar">
          <section class="library-panel achievements-card">
            <div class="panel-title-row">
              <h2>我的成就</h2>
              <RouterLink :to="{ name: 'game-community', params: { gameId } }">查看全部</RouterLink>
            </div>
            <strong>已解锁 {{ unlockedCount }}/{{ achievements.length }}（{{ achievementPercent }}%）</strong>
            <div class="library-progress"><i :style="{ width: achievementPercent + '%' }" /></div>
            <div v-if="loading" class="library-state">正在加载成就...</div>
            <div v-else-if="achievements.length === 0" class="library-state">暂无成就数据。</div>
            <div v-else class="library-achievement-grid">
              <div v-for="achievement in achievementPreview" :key="achievement.achId" :class="['library-achievement', { locked: !achievement.isUnlocked }]">
                <span>{{ achievement.achName.slice(0, 1) }}</span>
                <small>{{ achievement.achName }}</small>
              </div>
            </div>
          </section>

          <section v-if="isCs2" class="library-panel links-card">
            <h2>CS2 库内能力</h2>
            <RouterLink :to="{ name: 'inventory', query: { gameId } }">查看我的饰品库存</RouterLink>
            <RouterLink :to="{ name: 'market', query: { gameId } }">进入饰品市场</RouterLink>
            <RouterLink :to="{ name: 'game-community', params: { gameId }, query: { section: 'workshop' } }">创意工坊入口</RouterLink>
          </section>

          <section v-else class="library-panel links-card">
            <h2>DST 库内能力</h2>
            <RouterLink :to="{ name: 'game-detail', params: { gameId }, hash: '#packages' }">查看 DLC / 礼包</RouterLink>
            <RouterLink :to="{ name: 'game-community', params: { gameId } }">社区评价与成就</RouterLink>
            <RouterLink :to="{ name: 'game-community', params: { gameId }, query: { section: 'workshop' } }">创意工坊入口</RouterLink>
          </section>
        </aside>
      </section>
    </main>
  </section>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { RouterLink, useRoute } from 'vue-router';
import { addPlaytime, getLibrary, type LibraryEntry } from '../api/coreApi';
import { listGameAchievements } from '../api/communityApi';
import { getApiError } from '../api/http';
import type { AchievementListItem } from '../api/types';
import { gameCatalog, getGameMeta } from '../data/gameCatalog';
import { dateTime, minutesText } from '../utils/format';

const route = useRoute();
const gameId = computed(() => String(route.params.gameId || 'GAME_DST'));
const game = computed(() => getGameMeta(gameId.value));
const isCs2 = computed(() => game.value.shortName === 'CS2');
const isDst = computed(() => game.value.shortName === 'DST');
const library = ref<LibraryEntry[]>([]);
const achievements = ref<AchievementListItem[]>([]);
const loading = ref(false);
const notice = ref('');

const ownedEntry = computed(() => library.value.find((entry) => entry.gameId === gameId.value) || null);
const playtimeText = computed(() => minutesText(ownedEntry.value?.playMinutes || 0));
const lastPlayText = computed(() => ownedEntry.value?.lastPlayTime ? dateTime(ownedEntry.value.lastPlayTime) : '暂无记录');
const unlockedCount = computed(() => achievements.value.filter((achievement) => achievement.isUnlocked).length);
const achievementPercent = computed(() => achievements.value.length === 0 ? 0 : Math.round((unlockedCount.value / achievements.value.length) * 100));
const achievementPreview = computed(() => achievements.value.slice(0, 5));
const libraryNavEntries = computed(() => {
  const ids = library.value.length ? library.value.map((entry) => entry.gameId) : Object.keys(gameCatalog);
  return ids.map((id) => getGameMeta(id));
});

watch(gameId, loadLibraryDetail, { immediate: true });

async function loadLibraryDetail() {
  loading.value = true;
  notice.value = '';
  try {
    const [libraryRows, achievementRows] = await Promise.all([
      getLibrary(),
      listGameAchievements(gameId.value)
    ]);
    library.value = libraryRows;
    achievements.value = achievementRows;
    if (!libraryRows.some((entry) => entry.gameId === gameId.value)) {
      notice.value = '当前账号暂未确认拥有此游戏。你可以返回商店详情查看购买或入库入口。';
    }
  } catch (error) {
    notice.value = getApiError(error);
  } finally {
    loading.value = false;
  }
}

async function addMinutes(minutesToAdd: number) {
  notice.value = '';
  try {
    const updated = await addPlaytime(gameId.value, minutesToAdd);
    const index = library.value.findIndex((entry) => entry.gameId === gameId.value);
    if (index >= 0) {
      library.value.splice(index, 1, updated);
    } else {
      library.value.push(updated);
    }
    notice.value = `${updated.gameName} 游玩时长已更新为 ${minutesText(updated.playMinutes)}`;
  } catch (error) {
    notice.value = getApiError(error);
  }
}
</script>

<style scoped>
.steam-library-page {
  display: grid;
  grid-template-columns: 260px minmax(0, 1fr);
  min-height: calc(100vh - 128px);
  background:
    linear-gradient(90deg, rgba(20, 31, 44, 0.96), rgba(20, 31, 44, 0.76)),
    radial-gradient(circle at 82% 10%, rgba(156, 108, 55, 0.18), transparent 34rem);
}

.library-rail {
  display: grid;
  align-content: start;
  gap: 8px;
  border-right: 1px solid rgba(255, 255, 255, 0.06);
  padding: 8px;
  background: rgba(19, 26, 36, 0.94);
}

.rail-home,
.rail-filter,
.rail-search {
  min-height: 34px;
  padding: 7px 10px;
  color: #c7d5e0;
  background: rgba(255, 255, 255, 0.06);
  text-decoration: none;
}

.library-rail ul {
  display: grid;
  gap: 2px;
  margin: 8px 0 0;
  padding: 0;
  list-style: none;
}

.library-rail a {
  display: grid;
  grid-template-columns: 34px minmax(0, 1fr);
  gap: 8px;
  align-items: center;
  min-height: 34px;
  padding: 4px;
  color: #a8b3c2;
  text-decoration: none;
}

.library-rail span {
  display: grid;
  place-items: center;
  width: 28px;
  height: 28px;
  color: #ffffff;
  background: #2f5f86;
  font-size: 0.72rem;
  font-weight: 900;
}

.library-rail li.active a,
.library-rail a:hover {
  color: #ffffff;
  background: #3b5b83;
}

.library-detail {
  min-width: 0;
}

.library-hero,
.panel-title-row,
.activity-card,
.library-actions,
.library-achievement-grid {
  display: flex;
  align-items: center;
  gap: 12px;
}

.library-hero {
  min-height: 76px;
  padding: 12px 18px;
  background:
    linear-gradient(90deg, rgba(42, 71, 94, 0.88), rgba(20, 31, 44, 0.78)),
    radial-gradient(circle at 80% 0%, rgba(102, 192, 244, 0.22), transparent 26rem);
}

.library-hero.tone-tactical {
  background:
    linear-gradient(90deg, rgba(65, 57, 43, 0.88), rgba(20, 31, 44, 0.78)),
    radial-gradient(circle at 80% 0%, rgba(216, 169, 71, 0.2), transparent 26rem);
}

.install-button {
  min-width: 150px;
  min-height: 42px;
  border: 0;
  border-radius: 2px;
  color: #ffffff;
  background: linear-gradient(90deg, #1a9fff, #3b8edb);
  cursor: pointer;
  font-size: 1rem;
  font-weight: 900;
}

.library-game-title {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.library-game-title > span {
  display: grid;
  place-items: center;
  width: 44px;
  height: 44px;
  color: #ffffff;
  background: linear-gradient(135deg, #66c0f4, #1b2838);
  font-weight: 900;
}

.library-game-title h1,
.library-game-title p {
  margin: 0;
}

.library-game-title h1 {
  overflow: hidden;
  color: #dfe3e6;
  font-size: 1.35rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.library-game-title p {
  color: #8f98a0;
}

.library-actions {
  margin-left: auto;
}

.library-actions a,
.entry-grid a,
.panel-title-row a,
.links-card a {
  color: #66c0f4;
  font-weight: 800;
  text-decoration: none;
}

.library-actions a {
  min-height: 34px;
  padding: 7px 12px;
  color: #c7d5e0;
  background: rgba(255, 255, 255, 0.08);
}

.library-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 360px;
  gap: 18px;
  align-items: start;
  padding: 18px;
}

.activity-feed,
.library-sidebar,
.links-card,
.owned-actions,
.stats-panel {
  display: grid;
  gap: 16px;
}

.activity-card,
.library-panel,
.library-notice {
  border: 1px solid rgba(102, 192, 244, 0.12);
  border-radius: 4px;
  background: rgba(29, 41, 55, 0.84);
}

.activity-card,
.library-panel,
.library-notice {
  padding: 14px;
}

.activity-card {
  align-items: stretch;
}

.activity-art {
  display: grid;
  place-items: center;
  width: 240px;
  min-height: 120px;
  color: #ffffff;
  background:
    linear-gradient(135deg, rgba(72, 111, 140, 0.96), rgba(12, 18, 26, 0.96));
  font-size: 2.4rem;
  font-weight: 900;
}

.activity-card small,
.activity-card p,
.library-panel p,
.library-state,
.stats-panel dt {
  color: #8f98a0;
}

.activity-card p,
.activity-card h2,
.library-panel h2,
.library-panel p {
  margin: 0;
}

.activity-card h2 {
  margin: 4px 0 8px;
  color: #dfe3e6;
}

.stats-panel dl {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 10px;
  margin: 0;
}

.stats-panel div {
  border-radius: 4px;
  padding: 12px;
  background: rgba(9, 14, 22, 0.42);
}

.stats-panel dd {
  margin: 4px 0 0;
  color: #dfe3e6;
  font-size: 1.2rem;
  font-weight: 900;
  font-variant-numeric: tabular-nums;
}

.entry-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 10px;
}

.entry-grid a,
.links-card a {
  border: 1px solid rgba(102, 192, 244, 0.16);
  border-radius: 4px;
  padding: 10px 12px;
  background: rgba(102, 192, 244, 0.08);
}

.panel-title-row {
  justify-content: space-between;
}

.panel-title-row h2 {
  margin: 0;
}

.achievements-card strong {
  display: block;
  margin-top: 12px;
  color: #c7d5e0;
}

.library-progress {
  height: 10px;
  margin: 10px 0 14px;
  overflow: hidden;
  background: #05080c;
}

.library-progress i {
  display: block;
  height: 100%;
  background: linear-gradient(90deg, #66c0f4, #a4d007);
}

.library-achievement-grid {
  flex-wrap: wrap;
}

.library-achievement {
  display: grid;
  gap: 6px;
  width: 64px;
}

.library-achievement span {
  display: grid;
  place-items: center;
  height: 54px;
  color: #ffffff;
  background: linear-gradient(135deg, #2f89bc, #1b2838);
  font-weight: 900;
}

.library-achievement.locked span {
  filter: grayscale(1);
  opacity: 0.5;
}

.library-achievement small {
  overflow: hidden;
  color: #8f98a0;
  font-size: 0.72rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

@media (max-width: 1120px) {
  .steam-library-page {
    grid-template-columns: 1fr;
  }

  .library-rail {
    display: none;
  }

  .library-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 760px) {
  .library-hero {
    align-items: flex-start;
    flex-direction: column;
  }

  .library-actions {
    margin-left: 0;
    flex-wrap: wrap;
  }

  .stats-panel dl {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .activity-card {
    display: grid;
  }

  .activity-art {
    width: 100%;
  }
}

@media (max-width: 520px) {
  .stats-panel dl {
    grid-template-columns: 1fr;
  }
}
</style>
