<template>
  <section
    class="media-gallery"
    :aria-label="`${title} 媒体画廊`"
    tabindex="0"
    @keydown.left.prevent="previous"
    @keydown.right.prevent="next"
    @keydown.enter.prevent="openViewer"
  >
    <div class="media-stage">
      <video
        v-if="activeItem.type === 'video' && !failedSources.has(activeItem.src)"
        ref="mainVideo"
        class="stage-media"
        :src="activeItem.src"
        :poster="poster"
        controls
        playsinline
        preload="metadata"
        @error="markFailed(activeItem.src)"
      >
        你的浏览器暂不支持视频播放。
      </video>

      <button
        v-else
        ref="stageButton"
        type="button"
        class="stage-image-button"
        :aria-label="`全屏查看：${activeItem.label}`"
        @click="openViewer"
      >
        <img
          class="stage-media"
          :src="resolvedImage(activeItem)"
          :alt="activeItem.label"
          @error="markFailed(activeItem.src)"
        />
        <span class="expand-hint"><Maximize2 :size="18" /> 全屏查看</span>
      </button>

      <div v-if="activeItem.type === 'video' && failedSources.has(activeItem.src)" class="video-fallback">
        <img :src="poster" :alt="`${title} 视频封面`" />
        <span>预告片暂时无法播放，可继续查看截图</span>
      </div>

      <button type="button" class="stage-nav stage-nav-left" title="上一个媒体" aria-label="上一个媒体" @click="previous">
        <ChevronLeft :size="28" />
      </button>
      <button type="button" class="stage-nav stage-nav-right" title="下一个媒体" aria-label="下一个媒体" @click="next">
        <ChevronRight :size="28" />
      </button>

      <div class="stage-caption">
        <span><Film v-if="activeItem.type === 'video'" :size="15" /><Images v-else :size="15" />{{ activeItem.label }}</span>
        <button type="button" title="全屏查看" aria-label="全屏查看当前媒体" @click="openViewer">
          <Maximize2 :size="17" />
        </button>
      </div>
    </div>

    <div class="thumbnail-strip" role="list" aria-label="选择媒体">
      <button
        v-for="(item, index) in items"
        :key="item.src"
        type="button"
        role="listitem"
        class="thumbnail-button"
        :class="{ active: index === activeIndex }"
        :aria-current="index === activeIndex ? 'true' : undefined"
        :aria-label="`查看 ${item.label}`"
        @click="select(index)"
      >
        <img :src="thumbnailSource(item)" alt="" loading="lazy" @error="markFailed(item.src)" />
        <span v-if="item.type === 'video'" class="video-badge"><PlayCircle :size="20" /></span>
      </button>
    </div>
  </section>

  <Teleport to="body">
    <div v-if="viewerOpen" class="viewer-backdrop" role="presentation" @click.self="closeViewer">
      <section
        ref="viewerDialog"
        class="viewer-dialog"
        role="dialog"
        aria-modal="true"
        :aria-label="`${title} 全屏媒体查看`"
        tabindex="-1"
      >
        <header>
          <div>
            <strong>{{ title }}</strong>
            <span>{{ activeItem.label }} · {{ activeIndex + 1 }} / {{ items.length }}</span>
          </div>
          <button type="button" title="关闭" aria-label="关闭全屏媒体" @click="closeViewer"><X :size="25" /></button>
        </header>

        <div class="viewer-media-shell">
          <video
            v-if="activeItem.type === 'video' && !failedSources.has(activeItem.src)"
            class="viewer-media"
            :src="activeItem.src"
            :poster="poster"
            controls
            autoplay
            playsinline
            preload="metadata"
            @error="markFailed(activeItem.src)"
          />
          <img v-else class="viewer-media" :src="resolvedImage(activeItem)" :alt="activeItem.label" @error="markFailed(activeItem.src)" />

          <button type="button" class="viewer-nav viewer-nav-left" title="上一个媒体" aria-label="上一个媒体" @click="previous">
            <ChevronLeft :size="36" />
          </button>
          <button type="button" class="viewer-nav viewer-nav-right" title="下一个媒体" aria-label="下一个媒体" @click="next">
            <ChevronRight :size="36" />
          </button>
        </div>
      </section>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue';
import { ChevronLeft, ChevronRight, Film, Images, Maximize2, PlayCircle, X } from '@lucide/vue';
import type { GameMediaItem } from '../data/gameCatalog';

const props = defineProps<{
  title: string;
  poster: string;
  items: GameMediaItem[];
}>();

const activeIndex = ref(0);
const viewerOpen = ref(false);
const failedSources = ref(new Set<string>());
const mainVideo = ref<HTMLVideoElement | null>(null);
const stageButton = ref<HTMLButtonElement | null>(null);
const viewerDialog = ref<HTMLElement | null>(null);

const activeItem = computed(() => props.items[activeIndex.value] ?? {
  type: 'image' as const,
  src: props.poster,
  label: `${props.title} 游戏画面`
});

watch(
  () => props.items,
  () => {
    activeIndex.value = 0;
    failedSources.value = new Set();
    closeViewer();
  }
);

function select(index: number) {
  mainVideo.value?.pause();
  activeIndex.value = index;
}

function previous() {
  select((activeIndex.value - 1 + props.items.length) % props.items.length);
}

function next() {
  select((activeIndex.value + 1) % props.items.length);
}

async function openViewer() {
  mainVideo.value?.pause();
  viewerOpen.value = true;
  document.body.classList.add('media-viewer-open');
  document.addEventListener('keydown', handleViewerKeydown);
  await nextTick();
  viewerDialog.value?.focus();
}

function closeViewer() {
  if (!viewerOpen.value) return;
  viewerOpen.value = false;
  document.body.classList.remove('media-viewer-open');
  document.removeEventListener('keydown', handleViewerKeydown);
  void nextTick(() => stageButton.value?.focus());
}

function handleViewerKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') closeViewer();
  if (event.key === 'ArrowLeft') {
    event.preventDefault();
    previous();
  }
  if (event.key === 'ArrowRight') {
    event.preventDefault();
    next();
  }
}

function markFailed(src: string) {
  failedSources.value = new Set(failedSources.value).add(src);
}

function resolvedImage(item: GameMediaItem) {
  if (item.type === 'image' && !failedSources.value.has(item.src)) return item.src;
  return props.poster;
}

function thumbnailSource(item: GameMediaItem) {
  if (item.type === 'video' || failedSources.value.has(item.src)) return props.poster;
  return item.src;
}

onBeforeUnmount(() => {
  document.body.classList.remove('media-viewer-open');
  document.removeEventListener('keydown', handleViewerKeydown);
});
</script>

<style scoped>
.media-gallery {
  min-width: 0;
  outline: none;
}

.media-gallery:focus-visible {
  outline: 2px solid #67c1f5;
  outline-offset: 3px;
}

.media-stage {
  position: relative;
  aspect-ratio: 16 / 9;
  overflow: hidden;
  background: #05080c;
  box-shadow: 0 0 18px rgba(0, 0, 0, 0.5);
}

.stage-media,
.stage-image-button,
.video-fallback,
.video-fallback img {
  width: 100%;
  height: 100%;
}

.stage-media,
.video-fallback img {
  display: block;
  object-fit: cover;
}

.stage-image-button {
  position: relative;
  display: block;
  border: 0;
  padding: 0;
  background: transparent;
  cursor: zoom-in;
}

.expand-hint {
  position: absolute;
  right: 0.75rem;
  top: 0.75rem;
  display: inline-flex;
  gap: 0.4rem;
  align-items: center;
  border-radius: 2px;
  padding: 0.38rem 0.55rem;
  color: #ffffff;
  background: rgba(12, 18, 26, 0.78);
  opacity: 0;
  transform: translateY(-4px);
  transition: opacity 150ms ease, transform 150ms ease;
}

.stage-image-button:hover .expand-hint,
.stage-image-button:focus-visible .expand-hint {
  opacity: 1;
  transform: translateY(0);
}

.video-fallback {
  position: relative;
}

.video-fallback span {
  position: absolute;
  inset: auto 0 0;
  padding: 0.8rem 1rem;
  color: #c7d5e0;
  background: rgba(9, 15, 22, 0.9);
  text-align: center;
}

.stage-nav,
.viewer-nav,
.stage-caption button,
.viewer-dialog header button {
  display: grid;
  place-items: center;
  border: 0;
  color: #ffffff;
  background: rgba(8, 13, 19, 0.72);
  cursor: pointer;
}

.stage-nav {
  position: absolute;
  top: 50%;
  width: 42px;
  height: 64px;
  transform: translateY(-50%);
  opacity: 0;
  transition: opacity 150ms ease, background 150ms ease;
}

.media-stage:hover .stage-nav,
.stage-nav:focus-visible {
  opacity: 1;
}

.stage-nav:hover,
.viewer-nav:hover,
.stage-caption button:hover,
.viewer-dialog header button:hover {
  background: #1a9fff;
}

.stage-nav-left { left: 0; }
.stage-nav-right { right: 0; }

.stage-caption {
  position: absolute;
  inset: auto 0 0;
  display: flex;
  justify-content: space-between;
  align-items: center;
  min-height: 38px;
  padding-left: 0.75rem;
  color: #d6e2ec;
  background: linear-gradient(0deg, rgba(5, 8, 12, 0.92), rgba(5, 8, 12, 0.15));
  pointer-events: none;
}

.stage-caption span {
  display: inline-flex;
  gap: 0.42rem;
  align-items: center;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.stage-caption button {
  align-self: stretch;
  width: 42px;
  pointer-events: auto;
}

.thumbnail-strip {
  display: grid;
  grid-auto-flow: column;
  grid-auto-columns: minmax(92px, 1fr);
  gap: 0.38rem;
  overflow-x: auto;
  padding-top: 0.45rem;
  scrollbar-color: #4b6b85 #101822;
  scrollbar-width: thin;
}

.thumbnail-button {
  position: relative;
  aspect-ratio: 16 / 9;
  overflow: hidden;
  border: 2px solid transparent;
  border-radius: 2px;
  padding: 0;
  background: #080d13;
  opacity: 0.62;
  cursor: pointer;
  transition: border-color 150ms ease, opacity 150ms ease, transform 150ms ease;
}

.thumbnail-button:hover,
.thumbnail-button:focus-visible,
.thumbnail-button.active {
  border-color: #ffffff;
  opacity: 1;
}

.thumbnail-button:hover {
  transform: translateY(-1px);
}

.thumbnail-button img {
  display: block;
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.video-badge {
  position: absolute;
  inset: 0;
  display: grid;
  place-items: center;
  color: #ffffff;
  background: rgba(0, 0, 0, 0.25);
  filter: drop-shadow(0 1px 3px #000000);
}

.viewer-backdrop {
  position: fixed;
  z-index: 3000;
  inset: 0;
  display: grid;
  place-items: center;
  padding: 1.25rem;
  background: rgba(0, 0, 0, 0.92);
}

.viewer-dialog {
  display: grid;
  width: min(1440px, 100%);
  max-height: 100%;
  outline: none;
  background: #10161e;
  box-shadow: 0 20px 80px rgba(0, 0, 0, 0.65);
}

.viewer-dialog header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  min-height: 58px;
  padding-left: 1rem;
  color: #ffffff;
  background: #171f29;
}

.viewer-dialog header div {
  display: grid;
  gap: 0.15rem;
  min-width: 0;
}

.viewer-dialog header strong,
.viewer-dialog header span {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.viewer-dialog header span {
  color: #8f98a0;
  font-size: 0.82rem;
}

.viewer-dialog header button {
  align-self: stretch;
  width: 58px;
}

.viewer-media-shell {
  position: relative;
  display: grid;
  min-height: 0;
  place-items: center;
  background: #000000;
}

.viewer-media {
  display: block;
  max-width: 100%;
  max-height: calc(100vh - 100px);
  object-fit: contain;
}

.viewer-nav {
  position: absolute;
  top: 50%;
  width: 56px;
  height: 84px;
  transform: translateY(-50%);
}

.viewer-nav-left { left: 0; }
.viewer-nav-right { right: 0; }

@media (max-width: 620px) {
  .thumbnail-strip {
    grid-auto-columns: 94px;
  }

  .stage-nav {
    width: 34px;
    height: 52px;
    opacity: 1;
  }

  .expand-hint {
    display: none;
  }

  .viewer-backdrop {
    padding: 0;
  }

  .viewer-dialog {
    width: 100%;
  }

  .viewer-nav {
    width: 42px;
    height: 68px;
  }
}

@media (prefers-reduced-motion: reduce) {
  .expand-hint,
  .stage-nav,
  .thumbnail-button {
    transition: none;
  }
}
</style>

<style>
body.media-viewer-open {
  overflow: hidden;
}
</style>
