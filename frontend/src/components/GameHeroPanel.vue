<template>
  <section class="hero-panel">
    <div class="hero-copy">
      <p>Steam 风格精选</p>
      <h1>精选深度折扣</h1>
      <span>经典中的经典，特惠加上特惠</span>
    </div>
    <div class="hero-games">
      <RouterLink v-for="game in featured" :key="game.gameId" class="hero-tile" :to="{ name: 'game-detail', params: { gameId: game.gameId } }">
        <div class="hero-art" :class="`tone-${game.coverTone}`">
          <span>{{ game.shortName }}</span>
        </div>
        <GamePriceBlock :base-price="game.basePrice" :final-price="game.finalPrice" :discount-rate="game.discountRate" compact />
      </RouterLink>
    </div>
    <div class="hero-actions">
      <button type="button">查看全部</button>
      <div class="dots" aria-hidden="true">
        <span class="active"></span>
        <span></span>
        <span></span>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { GameListItem } from '../api/types';
import GamePriceBlock from './GamePriceBlock.vue';

const props = defineProps<{
  games: GameListItem[];
}>();

const featured = computed(() => props.games.slice(0, 2));
</script>

<style scoped>
.hero-panel {
  display: grid;
  grid-template-columns: minmax(190px, 0.52fr) minmax(0, 1.6fr) auto;
  gap: 1rem;
  align-items: center;
  min-height: 292px;
  overflow: hidden;
  border-radius: 6px;
  padding: clamp(1rem, 2vw, 1.4rem);
  background:
    linear-gradient(90deg, rgba(50, 103, 103, 0.82), rgba(37, 83, 90, 0.68)),
    radial-gradient(circle at 82% 18%, rgba(117, 197, 63, 0.16), transparent 14rem),
    #1b3540;
}

.hero-copy p,
.hero-copy h1,
.hero-copy span {
  margin: 0;
}

.hero-copy p {
  color: var(--steam-blue);
  font-weight: 900;
}

.hero-copy h1 {
  margin-top: 0.2rem;
  font-size: clamp(1.7rem, 4vw, 2.45rem);
  line-height: 1.08;
  text-wrap: balance;
}

.hero-copy span {
  display: block;
  margin-top: 0.38rem;
  color: #c7d5e0;
  font-size: 1.05rem;
}

.hero-games {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.9rem;
}

.hero-tile {
  position: relative;
  overflow: hidden;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 4px;
  background: #101822;
}

.hero-tile:hover {
  border-color: rgba(102, 192, 244, 0.55);
}

.hero-art {
  display: grid;
  min-height: 170px;
  place-items: center;
  background: linear-gradient(135deg, #17283a, #0d1118);
}

.hero-art.tone-cs2 {
  background: linear-gradient(135deg, #12202d, #1c5b72);
}

.hero-art.tone-dst {
  background: linear-gradient(135deg, #102015, #315438);
}

.hero-art span {
  color: #ffffff;
  font-size: clamp(2.6rem, 7vw, 4.6rem);
  font-weight: 950;
}

.hero-tile :deep(.game-price) {
  position: absolute;
  right: 0;
  bottom: 0;
}

.hero-actions {
  display: grid;
  gap: 1.5rem;
  justify-items: center;
}

.hero-actions button {
  min-width: 112px;
  border: 0;
  border-radius: 4px;
  padding: 0.65rem 1rem;
  color: #1b2838;
  background: #d7dadd;
  cursor: pointer;
  font-weight: 900;
}

.dots {
  display: flex;
  gap: 0.35rem;
}

.dots span {
  width: 20px;
  height: 8px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.28);
}

.dots .active {
  background: #ffffff;
}

@media (max-width: 900px) {
  .hero-panel {
    grid-template-columns: 1fr;
  }

  .hero-actions {
    justify-items: start;
  }
}

@media (max-width: 620px) {
  .hero-games {
    grid-template-columns: 1fr;
  }
}
</style>
