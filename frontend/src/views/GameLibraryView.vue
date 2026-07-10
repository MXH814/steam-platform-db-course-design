<template>
  <section class="steam-library-page">
    <aside class="library-rail">
      <div class="rail-home">主页</div>
      <div class="rail-filter">游戏和软件</div>
      <div class="rail-search">搜索</div>
      <ul>
        <li v-for="entry in libraryEntries" :key="entry.gameId" :class="{ active: entry.gameId === gameId }">
          <RouterLink :to="`/library/${entry.gameId}`">
            <span>{{ entry.shortName }}</span>
            {{ entry.title }}
          </RouterLink>
        </li>
      </ul>
    </aside>

    <main class="library-detail">
      <header class="library-hero" :class="`tone-${game.heroTone}`">
        <button class="install-button" type="button" @click="notice = '安装按钮是库页面展示占位，真实安装不属于本课程系统。'">安装</button>
        <div class="library-game-title">
          <span>{{ game.capsuleLabel }}</span>
          <h1>{{ game.title }}</h1>
        </div>
        <div class="library-actions">
          <RouterLink :to="`/games/${gameId}/store`">商店页面</RouterLink>
          <RouterLink :to="`/games/${gameId}/community`">评测与成就</RouterLink>
        </div>
      </header>

      <div v-if="notice" class="library-notice">{{ notice }}</div>

      <section class="library-grid">
        <div class="activity-feed">
          <article v-for="update in game.updates" :key="update.title" class="activity-card">
            <div class="activity-art">{{ game.shortName }}</div>
            <div>
              <small>{{ update.date }}</small>
              <p>{{ update.type }}</p>
              <h2>{{ update.title }}</h2>
              <p>{{ update.body }}</p>
            </div>
          </article>

          <article class="activity-card compact">
            <div class="tool-icon">工具</div>
            <div>
              <p>社区入口</p>
              <h2>发表评价、查看历史版本并解锁成就</h2>
              <RouterLink :to="`/games/${gameId}/community`">打开社区页面</RouterLink>
            </div>
          </article>
        </div>

        <aside class="library-sidebar">
          <section class="library-panel achievements-card">
            <div class="panel-title-row">
              <h2>成就</h2>
              <RouterLink :to="`/games/${gameId}/community`">查看我的成就</RouterLink>
            </div>
            <strong>您已解锁 {{ unlockedCount }}/{{ achievements.length }}（{{ achievementPercent }}%）</strong>
            <div class="library-progress"><i :style="{ width: achievementPercent + '%' }" /></div>
            <div v-if="loading" class="library-state">正在加载成就...</div>
            <div v-else-if="achievements.length === 0" class="library-state">暂无成就数据。</div>
            <div v-else class="library-achievement-grid">
              <div v-for="achievement in achievementPreview" :key="achievement.achId" :class="['library-achievement', { locked: !achievement.isUnlocked }]">
                <img :src="achievement.iconUrl" :alt="achievement.achName" />
                <small>{{ achievement.achName }}</small>
              </div>
            </div>
          </section>

          <section class="library-panel notes-card">
            <div class="panel-title-row">
              <h2>笔记</h2>
              <button type="button" @click="notice = '笔记需要后端或本地存储设计，当前先不改后端。'">新笔记</button>
            </div>
            <p>可记录测试流程：购买 GAME_DST 后发表评价，再解锁自定义成就。</p>
          </section>

          <section class="library-panel links-card">
            <h2>快捷链接</h2>
            <RouterLink :to="`/games/${gameId}/store`">在商店中查看</RouterLink>
            <RouterLink :to="`/games/${gameId}/community`">查看讨论与评测</RouterLink>
            <button type="button" @click="notice = 'DLC 管理需要 Group B/C 的商品扩展接口。'">管理 DLC</button>
          </section>
        </aside>
      </section>
    </main>
  </section>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { RouterLink, useRoute } from 'vue-router';
import { listGameAchievements } from '../api/communityApi';
import { getApiError } from '../api/http';
import { mergeAchievementCatalog, type AchievementDisplayItem } from '../data/achievementCatalog';
import { gameCatalog, getGameMeta } from '../data/gameCatalog';

const route = useRoute();
const gameId = computed(() => String(route.params.gameId || 'GAME_DST'));
const game = computed(() => getGameMeta(gameId.value));
const libraryEntries = Object.values(gameCatalog);
const achievements = ref<AchievementDisplayItem[]>([]);
const loading = ref(false);
const notice = ref('');

const unlockedCount = computed(() => achievements.value.filter((achievement) => achievement.isUnlocked).length);
const achievementPercent = computed(() => achievements.value.length === 0 ? 0 : Math.round((unlockedCount.value / achievements.value.length) * 100));
const achievementPreview = computed(() => achievements.value.slice(0, 5));

watch(gameId, loadLibrary, { immediate: true });

async function loadLibrary() {
  loading.value = true;
  notice.value = '';
  try {
    const achievementRows = await listGameAchievements(gameId.value);
    achievements.value = mergeAchievementCatalog(gameId.value, achievementRows);
  } catch (error) {
    achievements.value = mergeAchievementCatalog(gameId.value, []);
    notice.value = getApiError(error);
  } finally {
    loading.value = false;
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
    radial-gradient(circle at 82% 10%, rgba(156, 108, 55, 0.26), transparent 34rem);
}

.library-rail {
  display: grid;
  align-content: start;
  gap: 8px;
  padding: 8px;
  background: rgba(19, 26, 36, 0.94);
  border-right: 1px solid rgba(255, 255, 255, 0.06);
}

.rail-home,
.rail-filter,
.rail-search {
  min-height: 34px;
  padding: 7px 10px;
  color: #c7d5e0;
  background: rgba(255, 255, 255, 0.06);
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
.library-grid,
.activity-card,
.library-actions,
.library-achievement-grid,
.links-card {
  display: flex;
  align-items: center;
  gap: 12px;
}

.library-hero {
  min-height: 64px;
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
  font-size: 1.2rem;
  font-weight: 900;
}

.library-game-title {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.library-game-title span {
  display: grid;
  place-items: center;
  width: 44px;
  height: 44px;
  color: #ffffff;
  background: linear-gradient(135deg, #66c0f4, #1b2838);
  font-weight: 900;
}

.library-game-title h1 {
  overflow: hidden;
  margin: 0;
  color: #dfe3e6;
  font-size: 1.35rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.library-actions {
  margin-left: auto;
}

.library-actions a {
  min-height: 34px;
  padding: 7px 12px;
  color: #c7d5e0;
  background: rgba(255, 255, 255, 0.08);
}

.library-grid {
  align-items: start;
  display: grid;
  grid-template-columns: minmax(0, 1fr) 360px;
  gap: 18px;
  padding: 18px;
}

.activity-feed,
.library-sidebar {
  display: grid;
  gap: 16px;
}

.activity-card,
.library-panel,
.library-notice {
  border: 1px solid rgba(102, 192, 244, 0.12);
  border-radius: 2px;
  background: rgba(29, 41, 55, 0.84);
}

.activity-card {
  align-items: stretch;
  padding: 14px;
}

.activity-card.compact {
  background: rgba(26, 39, 54, 0.9);
}

.activity-art {
  display: grid;
  place-items: center;
  width: 270px;
  min-height: 120px;
  color: #ffffff;
  background:
    linear-gradient(135deg, rgba(72, 111, 140, 0.96), rgba(12, 18, 26, 0.96)),
    repeating-linear-gradient(45deg, rgba(255, 255, 255, 0.06) 0 12px, transparent 12px 24px);
  font-size: 2.4rem;
  font-weight: 900;
}

.tool-icon {
  display: grid;
  place-items: center;
  width: 100px;
  color: #c7d5e0;
  background: rgba(255, 255, 255, 0.08);
  font-weight: 900;
}

.activity-card small,
.activity-card p,
.library-panel p,
.library-state {
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

.activity-card a,
.panel-title-row a,
.links-card a {
  color: #66c0f4;
  font-weight: 800;
}

.library-panel,
.library-notice {
  padding: 14px;
}

.panel-title-row {
  justify-content: space-between;
}

.panel-title-row h2 {
  margin: 0;
}

.panel-title-row button,
.links-card button {
  border: 0;
  color: #66c0f4;
  background: rgba(102, 192, 244, 0.13);
  cursor: pointer;
  font-weight: 800;
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

.library-achievement img {
  display: block;
  width: 64px;
  height: 54px;
  object-fit: cover;
  color: #ffffff;
  background: linear-gradient(135deg, #2f89bc, #1b2838);
}

.library-achievement.locked img {
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

.links-card {
  align-items: stretch;
  flex-direction: column;
}

.links-card a,
.links-card button {
  min-height: 34px;
  padding: 7px 10px;
  text-align: left;
  background: rgba(42, 71, 94, 0.72);
}

@media (max-width: 1120px) {
  .steam-library-page,
  .library-grid {
    grid-template-columns: 1fr;
  }

  .library-rail {
    display: none;
  }
}

@media (max-width: 760px) {
  .library-hero,
  .activity-card,
  .library-actions {
    align-items: stretch;
    flex-direction: column;
  }

  .activity-art,
  .install-button,
  .library-actions a {
    width: 100%;
  }
}
</style>
