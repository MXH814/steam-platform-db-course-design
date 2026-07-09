<template>
  <RouterLink class="game-card" :to="{ name: 'game-detail', params: { gameId: game.gameId } }">
    <div class="game-art" :class="`tone-${game.coverTone}`">
      <span class="game-mark">{{ game.shortName }}</span>
      <span v-if="game.supportsMarket" class="corner-label">市场</span>
      <span v-else-if="game.hasContentPackages" class="corner-label">礼包</span>
    </div>
    <div class="game-meta">
      <div>
        <h3>{{ game.gameName }}</h3>
        <p>{{ game.reputation }} · {{ releaseYear }}</p>
      </div>
      <GamePriceBlock :base-price="game.basePrice" :final-price="game.finalPrice" :discount-rate="game.discountRate" compact />
    </div>
    <div class="tag-row">
      <span v-for="tag in game.tags.slice(0, 3)" :key="tag">{{ tag }}</span>
    </div>
  </RouterLink>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { GameListItem } from '../api/types';
import GamePriceBlock from './GamePriceBlock.vue';

const props = defineProps<{
  game: GameListItem;
}>();

const releaseYear = computed(() => (props.game.releaseDate ? new Date(props.game.releaseDate).getFullYear() : '待定'));
</script>

<style scoped>
.game-card {
  display: grid;
  min-width: 0;
  overflow: hidden;
  border: 1px solid transparent;
  border-radius: 6px;
  background: rgba(16, 24, 34, 0.9);
}

.game-card:hover {
  border-color: rgba(102, 192, 244, 0.55);
  background: rgba(27, 40, 56, 0.96);
}

.game-art {
  position: relative;
  display: grid;
  min-height: 148px;
  aspect-ratio: 16 / 7;
  place-items: center;
  overflow: hidden;
  background:
    radial-gradient(circle at 20% 30%, rgba(255, 255, 255, 0.18), transparent 0.8rem),
    linear-gradient(135deg, #20374d, #0e151f);
}

.game-art::before,
.game-art::after {
  position: absolute;
  inset: auto;
  content: "";
}

.game-art::before {
  width: 72%;
  height: 150%;
  border: 1px solid rgba(255, 255, 255, 0.09);
  transform: rotate(-16deg);
}

.game-art::after {
  right: -8%;
  bottom: -32%;
  width: 46%;
  aspect-ratio: 1;
  border-radius: 50%;
  background: rgba(102, 192, 244, 0.15);
}

.tone-cs2 {
  background:
    linear-gradient(90deg, rgba(9, 16, 24, 0.92), rgba(17, 82, 111, 0.48)),
    linear-gradient(135deg, #1b2634, #10151d 45%, #234a68);
}

.tone-dst {
  background:
    radial-gradient(circle at 22% 35%, rgba(117, 197, 63, 0.2), transparent 6rem),
    linear-gradient(135deg, #122416, #1d3422 52%, #0d1118);
}

.tone-background {
  background: linear-gradient(135deg, #23364b, #111923);
}

.game-mark {
  position: relative;
  z-index: 1;
  color: rgba(238, 242, 248, 0.96);
  font-size: clamp(2.1rem, 7vw, 3.8rem);
  font-weight: 900;
  letter-spacing: 0;
}

.corner-label {
  position: absolute;
  top: 0.58rem;
  left: 0.58rem;
  z-index: 1;
  border-radius: 4px;
  padding: 0.16rem 0.42rem;
  color: #dff4ff;
  background: rgba(8, 13, 19, 0.72);
  font-size: 0.76rem;
  font-weight: 800;
}

.game-meta {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 0.75rem;
  align-items: end;
  padding: 0.72rem;
}

.game-meta h3,
.game-meta p {
  margin: 0;
}

.game-meta h3 {
  overflow: hidden;
  color: var(--steam-text);
  font-size: 0.98rem;
  text-overflow: ellipsis;
  text-wrap: balance;
  white-space: nowrap;
}

.game-meta p {
  color: var(--steam-muted);
  font-size: 0.78rem;
}

.tag-row {
  display: flex;
  gap: 0.35rem;
  flex-wrap: wrap;
  padding: 0 0.72rem 0.72rem;
}

.tag-row span {
  border-radius: 3px;
  padding: 0.14rem 0.35rem;
  color: #b8c7d9;
  background: rgba(151, 170, 195, 0.15);
  font-size: 0.72rem;
}

@media (max-width: 620px) {
  .game-meta {
    grid-template-columns: 1fr;
    align-items: start;
  }

  .game-art {
    min-height: 126px;
  }
}
</style>
