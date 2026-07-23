<template>
  <article class="detail-template" :class="`variant-${variant}`">
    <section class="hero-shell">
      <div class="media-layout">
        <div class="media-column">
          <slot name="media">
            <div class="default-media">
              <span>{{ game.shortName }}</span>
            </div>
          </slot>
        </div>

        <aside class="summary-card">
          <slot name="summary-art">
            <div class="default-capsule">{{ game.gameName }}</div>
          </slot>
          <div class="summary-copy">
            <p>{{ summaryText }}</p>
            <dl>
              <div>
                <dt>最近评测</dt>
                <dd>{{ review?.ratingText || game.reputation || '暂无数据' }}</dd>
              </div>
              <div>
                <dt>发行日期</dt>
                <dd>{{ game.releaseDate || '待后端补充' }}</dd>
              </div>
              <div>
                <dt>开发商</dt>
                <dd>{{ game.developerName || '待后端补充' }}</dd>
              </div>
              <div>
                <dt>发行商</dt>
                <dd>{{ game.developerName || '待后端补充' }}</dd>
              </div>
            </dl>
          </div>
          <slot name="summary-extra" />
        </aside>
      </div>
    </section>

    <slot name="primary-action" />

    <div class="content-layout">
      <main class="main-stack">
        <slot name="main" />
      </main>
      <aside class="side-stack">
        <slot name="side" />
      </aside>
    </div>
  </article>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { GameDetail, GameReviewSummary } from '../api/types';

const props = withDefaults(
  defineProps<{
    game: GameDetail;
    review?: GameReviewSummary | null;
    variant?: 'cs2' | 'dst' | 'default';
    summary?: string;
  }>(),
  {
    review: null,
    variant: 'default',
    summary: ''
  }
);

const summaryText = computed(() => props.summary || props.game.description || props.game.summary || '该游戏详情信息等待后端继续补充。');
</script>

<style scoped>
.detail-template {
  display: grid;
  gap: 1rem;
}

.hero-shell {
  display: grid;
  gap: 0.9rem;
}

.default-banner {
  display: grid;
  min-height: 128px;
  align-content: center;
  border: 12px solid rgba(7, 12, 18, 0.78);
  padding: 1rem 1.5rem;
  background:
    radial-gradient(circle at 74% 28%, rgba(102, 192, 244, 0.2), transparent 12rem),
    linear-gradient(100deg, #132235, #24445d 56%, #111923);
}

.default-banner span {
  color: var(--steam-blue);
  font-weight: 900;
  letter-spacing: 0;
}

.default-banner strong {
  color: #ffffff;
  font-size: clamp(1.7rem, 4vw, 3rem);
  line-height: 1.08;
  text-wrap: balance;
}

.media-layout {
  display: grid;
  grid-template-columns: minmax(0, 1.85fr) minmax(320px, 0.85fr);
  gap: 1rem;
  align-items: stretch;
}

.media-column,
.summary-card {
  min-width: 0;
}

.default-media {
  display: grid;
  min-height: 420px;
  place-items: center;
  color: rgba(255, 255, 255, 0.72);
  background:
    radial-gradient(circle at 56% 42%, rgba(102, 192, 244, 0.2), transparent 10rem),
    linear-gradient(135deg, #080d13, #273b50);
  font-size: clamp(2.4rem, 8vw, 5rem);
  font-weight: 950;
}

.summary-card {
  display: grid;
  align-content: start;
  background: linear-gradient(180deg, rgba(24, 36, 49, 0.96), rgba(13, 20, 29, 0.96));
}

.default-capsule {
  display: grid;
  min-height: 172px;
  place-items: center;
  padding: 1rem;
  color: #ffffff;
  background: linear-gradient(135deg, #32465d, #111923);
  font-size: clamp(1.4rem, 3vw, 2rem);
  font-weight: 950;
  text-align: center;
  text-wrap: balance;
}

.summary-copy {
  display: grid;
  gap: 0.8rem;
  padding: 1rem;
}

.summary-copy p {
  margin: 0;
  color: #c7d5e0;
  line-height: 1.65;
  text-wrap: pretty;
}

.summary-copy dl {
  display: grid;
  gap: 0.48rem;
  margin: 0;
}

.summary-copy dl div {
  display: grid;
  grid-template-columns: 86px minmax(0, 1fr);
  gap: 0.75rem;
}

.summary-copy dt {
  color: var(--steam-muted);
}

.summary-copy dd {
  margin: 0;
  color: var(--steam-blue);
  font-weight: 800;
}

.content-layout {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(280px, 0.34fr);
  gap: 1rem;
  align-items: start;
}

.main-stack,
.side-stack {
  display: grid;
  gap: 1rem;
  min-width: 0;
}

@media (max-width: 1020px) {
  .media-layout,
  .content-layout {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 620px) {
  .default-banner {
    border-width: 8px;
    padding: 0.9rem;
  }

  .default-media {
    min-height: 280px;
  }

  .summary-copy dl div {
    grid-template-columns: 1fr;
    gap: 0.2rem;
  }
}
</style>
