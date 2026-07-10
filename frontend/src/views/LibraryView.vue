<template>
  <section class="view-stack">
    <div class="section-heading">
      <p class="eyebrow">Library</p>
      <h1>我的游戏库</h1>
    </div>

    <p v-if="message" class="message success">{{ message }}</p>
    <p v-if="error" class="message error">{{ error }}</p>

    <div class="library-grid">
      <article v-for="entry in library" :key="entry.libId" class="library-card">
        <RouterLink class="library-card-main" :to="{ name: 'game-library', params: { gameId: entry.gameId } }">
          <span class="pill">{{ entry.acquireWay }}</span>
          <h2>{{ entry.gameName }}</h2>
          <p>{{ entry.status }} · {{ minutesText(entry.playMinutes) }}</p>
          <small>最近游玩：{{ dateTime(entry.lastPlayTime) }}</small>
        </RouterLink>
        <button class="ghost-button" type="button" @click="addMinutes(entry.gameId)">增加 30 分钟</button>
      </article>
    </div>

    <p v-if="!library.length && !error" class="state-panel">暂无游戏，先去商店购买 DST 或免费领取 CS2。</p>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { RouterLink } from 'vue-router';
import { addPlaytime, getLibrary, type LibraryEntry } from '../api/coreApi';
import { getApiError } from '../api/http';
import { dateTime, minutesText } from '../utils/format';

const library = ref<LibraryEntry[]>([]);
const error = ref('');
const message = ref('');

async function load() {
  error.value = '';
  try {
    library.value = await getLibrary();
  } catch (requestError) {
    error.value = getApiError(requestError);
  }
}

async function addMinutes(gameId: string) {
  error.value = '';
  message.value = '';
  try {
    const updated = await addPlaytime(gameId, 30);
    message.value = `${updated.gameName} 游玩时长已更新为 ${minutesText(updated.playMinutes)}`;
    await load();
  } catch (requestError) {
    error.value = getApiError(requestError);
  }
}

onMounted(load);
</script>

<style scoped>
.library-card-main {
  display: block;
  min-width: 0;
  color: inherit;
  text-decoration: none;
}

.library-card-main h2 {
  color: var(--steam-text);
}

.library-card-main:hover h2 {
  color: var(--steam-blue);
}
</style>
