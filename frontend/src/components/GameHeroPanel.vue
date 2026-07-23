<template>
  <section class="hero-panel">
    <p class="section-kicker">精选与推荐</p>
    <RouterLink v-if="activeGame" class="feature" :to="{ name: 'game-detail', params: { gameId: activeGame.gameId } }">
      <div class="feature-media">
        <img :src="activeMeta.heroImage" :alt="activeGame.gameName" />
      </div>
      <aside class="feature-summary">
        <img class="feature-header" :src="activeMeta.headerImage" alt="" />
        <h1>{{ activeGame.gameName }}</h1>
        <p>{{ activeMeta.storeLine }}</p>
        <div class="feature-thumbs" aria-hidden="true">
          <img :src="activeMeta.headerImage" alt="" />
          <img :src="activeMeta.heroImage" alt="" />
        </div>
        <span class="available-now">现已推出</span>
        <div class="feature-tags">
          <span v-for="tag in activeGame.tags.slice(0, 3)" :key="tag">{{ tag }}</span>
        </div>
        <GamePriceBlock :base-price="activeGame.basePrice" :final-price="activeGame.finalPrice" :discount-rate="activeGame.discountRate" compact />
      </aside>
    </RouterLink>

    <div class="hero-actions">
      <button
        v-for="(game, index) in featured"
        :key="game.gameId"
        type="button"
        :class="{ active: index === activeIndex }"
        :aria-label="`展示 ${game.gameName}`"
        @click="activeIndex = index"
      >
        <span></span>
      </button>
      <RouterLink to="/store/categories">浏览更多</RouterLink>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import type { GameListItem } from '../api/types';
import { getGameMeta } from '../data/gameCatalog';
import GamePriceBlock from './GamePriceBlock.vue';

const props = defineProps<{
  games: GameListItem[];
}>();

const activeIndex = ref(0);
const featured = computed(() => {
  const priority = ['GAME_DST', 'GAME_CS2'];
  return [...props.games].sort((a, b) => priority.indexOf(a.gameId) - priority.indexOf(b.gameId)).slice(0, 2);
});
const activeGame = computed(() => featured.value[activeIndex.value] ?? featured.value[0]);
const activeMeta = computed(() => getGameMeta(activeGame.value?.gameId || 'GAME_DST'));

watch(featured, (items) => {
  if (activeIndex.value >= items.length) activeIndex.value = 0;
});
</script>

<style scoped>
.hero-panel {
  display: grid;
  gap: 0.55rem;
}

.section-kicker {
  margin: 0;
  color: #ffffff;
  font-size: 0.88rem;
  text-transform: uppercase;
}

.feature {
  display: grid;
  grid-template-columns: minmax(0, 2fr) minmax(260px, 0.84fr);
  min-height: 354px;
  overflow: hidden;
  background: #0e151f;
  box-shadow: 0 0 10px 4px #000000;
}

.feature-media {
  min-width: 0;
  overflow: hidden;
}

.feature-media img {
  width: 100%;
  height: 100%;
  min-height: 354px;
  object-fit: cover;
}

.feature-summary {
  position: relative;
  display: grid;
  align-content: start;
  gap: 0.55rem;
  min-width: 0;
  padding: 0.9rem 0.9rem 2.6rem;
  background: #101822;
}

.feature-header {
  width: 100%;
  aspect-ratio: 460 / 215;
  object-fit: cover;
}

.feature-summary h1,
.feature-summary p {
  margin: 0;
}

.feature-summary h1 {
  color: #ffffff;
  font-size: 1.42rem;
  font-weight: 400;
}

.feature-summary p {
  display: -webkit-box;
  overflow: hidden;
  color: #c7d5e0;
  font-size: 0.86rem;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.feature-thumbs {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.35rem;
}

.feature-thumbs img {
  width: 100%;
  aspect-ratio: 16 / 7;
  object-fit: cover;
}

.available-now {
  margin-top: 0.2rem;
  color: #ffffff;
  font-size: 1.05rem;
}

.feature-tags {
  display: flex;
  gap: 0.3rem;
  flex-wrap: wrap;
}

.feature-tags span {
  padding: 0.13rem 0.42rem;
  color: #d6e8f5;
  background: rgba(103, 193, 245, 0.18);
  font-size: 0.7rem;
}

.feature-summary :deep(.game-price) {
  position: absolute;
  right: 0.9rem;
  bottom: 0.75rem;
}

.hero-actions {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.35rem;
}

.hero-actions button {
  width: 18px;
  height: 9px;
  border: 0;
  border-radius: 2px;
  padding: 0;
  background: rgba(255, 255, 255, 0.25);
  cursor: pointer;
}

.hero-actions button.active {
  background: rgba(255, 255, 255, 0.7);
}

.hero-actions a {
  margin-left: 0.45rem;
  color: #8f98a0;
  font-size: 0.76rem;
}

@media (max-width: 820px) {
  .feature {
    grid-template-columns: 1fr;
  }

  .feature-summary {
    grid-template-columns: 120px minmax(0, 1fr);
    min-height: 160px;
  }

  .feature-summary p,
  .feature-tags,
  .feature-thumbs {
    grid-column: 2;
  }
}

@media (max-width: 560px) {
  .feature-summary {
    display: grid;
    grid-template-columns: 1fr;
  }

  .feature-summary p,
  .feature-tags,
  .feature-thumbs {
    grid-column: auto;
  }
}
</style>
