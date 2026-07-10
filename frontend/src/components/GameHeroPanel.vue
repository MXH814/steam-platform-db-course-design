<template>
  <section class="hero-panel">
    <div class="hero-copy">
      <p>Group B 商店演示</p>
      <h1>CS2 免费入库与 DST 折扣买断</h1>
      <span>首屏只突出课程设计中需要验收的两款主演示游戏。</span>
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
      <RouterLink to="/store">查看全部</RouterLink>
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

const featured = computed(() => {
  const priority = ['GAME_CS2', 'GAME_DST'];
  return [...props.games].sort((a, b) => priority.indexOf(b.gameId) - priority.indexOf(a.gameId)).slice(0, 2);
});
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
  text-wrap: pretty;
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
  transition: border-color 160ms ease-out;
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

.hero-actions a {
  min-width: 112px;
  border-radius: 4px;
  padding: 0.65rem 1rem;
  color: #1b2838;
  background: #d7dadd;
  cursor: pointer;
  font-weight: 900;
  text-align: center;
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

@media (prefers-reduced-motion: reduce) {
  .hero-tile {
    transition: none;
  }
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
