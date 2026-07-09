<template>
  <div class="collection-view">
    <GameFilterBar v-model="query" />

    <section class="collection-hero" :class="`tone-${page.tone}`">
      <div class="title-band">
        <h1>{{ page.title }}</h1>
      </div>
    </section>

    <section class="feature-stage">
      <button class="stage-arrow left" type="button" aria-label="上一项">‹</button>
      <div class="stage-main">
        <div class="stage-label">{{ page.eyebrow }}</div>
        <div class="video-placeholder">
          <div class="rating-card">
            <strong>{{ page.mediaTitle }}</strong>
            <span>{{ page.mediaMeta }}</span>
          </div>
          <button class="play-button" type="button" aria-label="播放预览">▶</button>
          <div class="video-controls">
            <span>Ⅱ</span>
            <span>0:00 / 1:14</span>
            <span>⚙ ⛶</span>
          </div>
        </div>
      </div>

      <aside class="feature-card">
        <RouterLink class="wish-button" to="/store">☆</RouterLink>
        <div class="capsule-art" :class="`tone-${page.tone}`">{{ page.badge }}</div>
        <div class="tag-row">
          <span v-for="tag in page.tags" :key="tag">{{ tag }}</span>
        </div>
        <p>{{ page.description }}</p>
        <dl>
          <div>
            <dt>发行日期</dt>
            <dd>{{ page.releaseDate }}</dd>
          </div>
        </dl>
        <div class="price-row">
          <GamePriceBlock :base-price="page.basePrice" :final-price="page.finalPrice" :discount-rate="page.discountRate" />
        </div>
      </aside>
      <button class="stage-arrow right" type="button" aria-label="下一项">›</button>
    </section>

    <section v-if="page.children.length" class="collection-grid">
      <header>
        <h2>{{ page.childrenTitle }}</h2>
        <span>这些入口先复用栏目空白页，后续可接真实列表</span>
      </header>
      <div class="collection-cards">
        <RouterLink v-for="child in page.children" :key="child.slug" class="collection-card" :to="child.to">
          <strong>{{ child.title }}</strong>
          <span>{{ child.summary }}</span>
        </RouterLink>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive } from 'vue';
import { useRoute } from 'vue-router';
import type { GameQuery } from '../api/types';
import GameFilterBar from '../components/GameFilterBar.vue';
import GamePriceBlock from '../components/GamePriceBlock.vue';

type CollectionChild = {
  slug: string;
  title: string;
  summary: string;
  to: string;
};

type CollectionPage = {
  title: string;
  eyebrow: string;
  mediaTitle: string;
  mediaMeta: string;
  badge: string;
  tone: 'blue' | 'green' | 'amber' | 'steel';
  tags: string[];
  description: string;
  releaseDate: string;
  basePrice: number;
  finalPrice: number;
  discountRate: number;
  childrenTitle: string;
  children: CollectionChild[];
};

const route = useRoute();
const query = reactive<GameQuery>({ search: '', priceFilter: 'all', sort: 'default' });

const childGroups: Record<string, CollectionChild[]> = {
  categories: [
    { slug: 'action-rpg', title: '动作角色扮演', summary: '动作、成长和装备驱动的游戏集合。', to: '/store/categories/action-rpg' },
    { slug: 'survival', title: '生存合作', summary: '饥荒联机版这类生存与合作体验。', to: '/store/categories/survival' },
    { slug: 'shooter', title: '射击竞技', summary: 'CS2 代表的免费竞技和对战体验。', to: '/store/categories/shooter' },
    { slug: 'market-economy', title: '饰品经济', summary: '库存、挂单、撮合与价格历史。', to: '/store/categories/market-economy' }
  ],
  playstyles: [
    { slug: 'co-op', title: '在线合作', summary: '适合联机、组队和好友游玩。', to: '/store/playstyles/co-op' },
    { slug: 'single-player', title: '单人体验', summary: '强调剧情、探索和个人进度。', to: '/store/playstyles/single-player' },
    { slug: 'multiplayer', title: '多人竞技', summary: '匹配、竞技和持续对战。', to: '/store/playstyles/multiplayer' },
    { slug: 'free-to-play', title: '免费入库', summary: '先入库再体验扩展玩法。', to: '/store/playstyles/free-to-play' }
  ],
  specials: [
    { slug: 'deep-discount', title: '精选深度折扣', summary: '买断制价格、折扣和促销展示。', to: '/store/specials/deep-discount' },
    { slug: 'content-packages', title: '内容包 / 礼包', summary: 'DST 内容包和礼包入口。', to: '/store/specials/content-packages' },
    { slug: 'item-market', title: '饰品市场', summary: 'CS2 饰品经济演示入口。', to: '/store/specials/item-market' },
    { slug: 'achievements', title: '成就挑战', summary: '成就列表、解锁和达成率。', to: '/store/specials/achievements' }
  ]
};

const childDetails: Record<string, Partial<CollectionPage>> = {
  'action-rpg': { title: '动作角色扮演', mediaTitle: '一般 / CG / 动作角色扮演', badge: 'ARPG', tone: 'steel' },
  survival: { title: '生存合作', mediaTitle: '一般 / 玩法预览 / 生存合作', badge: 'DST', tone: 'green' },
  shooter: { title: '射击竞技', mediaTitle: '一般 / 竞技预览 / 射击竞技', badge: 'CS2', tone: 'blue' },
  'market-economy': { title: '饰品经济', mediaTitle: '一般 / 市场预览 / 饰品经济', badge: 'MARKET', tone: 'blue' },
  'co-op': { title: '在线合作', mediaTitle: '一般 / 玩法预览 / 在线合作', badge: 'CO-OP', tone: 'green' },
  'single-player': { title: '单人体验', mediaTitle: '一般 / 玩法预览 / 单人体验', badge: 'SOLO', tone: 'steel' },
  multiplayer: { title: '多人竞技', mediaTitle: '一般 / 玩法预览 / 多人竞技', badge: 'PVP', tone: 'blue' },
  'free-to-play': { title: '免费入库', mediaTitle: '一般 / 商店预览 / 免费入库', badge: 'FREE', tone: 'blue', basePrice: 0, finalPrice: 0, discountRate: 0 },
  'deep-discount': { title: '精选深度折扣', mediaTitle: '一般 / 促销预览 / 深度折扣', badge: 'SALE', tone: 'amber' },
  'content-packages': { title: '内容包 / 礼包', mediaTitle: '一般 / 商店预览 / 内容包', badge: 'DLC', tone: 'green' },
  'item-market': { title: '饰品市场', mediaTitle: '一般 / 市场预览 / 饰品市场', badge: 'SKIN', tone: 'blue' },
  achievements: { title: '成就挑战', mediaTitle: '一般 / 社区预览 / 成就挑战', badge: 'ACH', tone: 'amber' }
};

const basePages: Record<string, CollectionPage> = {
  recommend: makePage({
    title: '为你推荐',
    eyebrow: '精选 / 推荐 / 今日浏览',
    mediaTitle: '一般 / 商店预览 / 为你推荐',
    badge: 'REC',
    tone: 'blue',
    tags: ['推荐', '好评', '课程演示', '精选'],
    childrenTitle: '推荐栏目'
  }),
  categories: makePage({
    title: '类别',
    eyebrow: '商店 / 类别 / 游戏类型',
    mediaTitle: '一般 / 分类预览 / 游戏类别',
    badge: 'CAT',
    tone: 'steel',
    tags: ['分类', '类型', '标签', '浏览'],
    childrenTitle: '选择一个分类',
    children: childGroups.categories
  }),
  playstyles: makePage({
    title: '畅玩方式',
    eyebrow: '商店 / 畅玩方式 / 游玩偏好',
    mediaTitle: '一般 / 玩法预览 / 畅玩方式',
    badge: 'PLAY',
    tone: 'green',
    tags: ['联机', '单人', '多人', '免费'],
    childrenTitle: '选择一种畅玩方式',
    children: childGroups.playstyles
  }),
  specials: makePage({
    title: '特别栏目',
    eyebrow: '商店 / 特别栏目 / 演示入口',
    mediaTitle: '一般 / 特别栏目 / 课程演示',
    badge: 'SP',
    tone: 'amber',
    tags: ['折扣', '礼包', '市场', '成就'],
    childrenTitle: '特别栏目入口',
    children: childGroups.specials
  })
};

const page = computed(() => {
  const section = String(route.params.section || 'recommend');
  const collectionId = String(route.params.collectionId || '');
  const base = basePages[section] || basePages.recommend;

  if (!collectionId) return base;

  const detail = childDetails[collectionId] || {};
  return {
    ...base,
    ...detail,
    eyebrow: detail.mediaTitle || base.eyebrow,
    description: `${detail.title || base.title} 是一个预留栏目页，当前用于承载基础信息和统一视觉结构，后续可以接入真实分类列表。`,
    children: []
  };
});

function makePage(page: Partial<CollectionPage>): CollectionPage {
  return {
    title: page.title || '商店栏目',
    eyebrow: page.eyebrow || '商店 / 栏目',
    mediaTitle: page.mediaTitle || '一般 / 商店预览',
    mediaMeta: page.mediaMeta || '视频已暂停',
    badge: page.badge || 'STORE',
    tone: page.tone || 'blue',
    tags: page.tags || ['商店', '演示', 'Steam 风格'],
    description:
      page.description ||
      '这是 Group B 商店栏目预留页，当前展示基础信息、推荐卡片和分类入口，后续可以替换为后端返回的真实列表。',
    releaseDate: page.releaseDate || '2026年7月9日',
    basePrice: page.basePrice ?? 398,
    finalPrice: page.finalPrice ?? 238.8,
    discountRate: page.discountRate ?? 0.4,
    childrenTitle: page.childrenTitle || '栏目入口',
    children: page.children || []
  };
}
</script>

<style scoped>
.collection-view {
  display: grid;
  gap: 1.15rem;
}

.collection-hero {
  display: grid;
  min-height: 220px;
  align-items: end;
  overflow: hidden;
  border-radius: 4px;
  padding: clamp(1rem, 5vw, 4rem) clamp(1rem, 7vw, 5.5rem) 2rem;
  background:
    linear-gradient(180deg, rgba(9, 15, 25, 0.04), rgba(9, 15, 25, 0.82)),
    radial-gradient(circle at 22% 22%, rgba(102, 192, 244, 0.26), transparent 12rem),
    #102133;
}

.collection-hero.tone-green {
  background:
    linear-gradient(180deg, rgba(9, 15, 25, 0.04), rgba(9, 15, 25, 0.82)),
    radial-gradient(circle at 20% 24%, rgba(117, 197, 63, 0.24), transparent 13rem),
    #123023;
}

.collection-hero.tone-amber {
  background:
    linear-gradient(180deg, rgba(9, 15, 25, 0.04), rgba(9, 15, 25, 0.82)),
    radial-gradient(circle at 22% 24%, rgba(190, 142, 55, 0.3), transparent 13rem),
    #2e2416;
}

.collection-hero.tone-steel {
  background:
    linear-gradient(180deg, rgba(9, 15, 25, 0.08), rgba(9, 15, 25, 0.84)),
    radial-gradient(circle at 24% 24%, rgba(172, 190, 206, 0.18), transparent 13rem),
    #172232;
}

.title-band {
  display: grid;
  place-items: center;
  min-height: 58px;
  background: rgba(19, 125, 158, 0.82);
}

.title-band h1 {
  margin: 0;
  color: #ffffff;
  font-size: clamp(1.8rem, 4vw, 3rem);
  line-height: 1.1;
  text-align: center;
}

.feature-stage {
  position: relative;
  display: grid;
  grid-template-columns: minmax(0, 1.75fr) minmax(300px, 0.85fr);
  gap: 0;
  padding: 0 clamp(1rem, 7vw, 5.5rem);
}

.stage-main {
  min-width: 0;
  background: #000000;
}

.stage-label {
  width: fit-content;
  padding: 0.28rem 0.5rem;
  color: #ffffff;
  background: rgba(151, 170, 195, 0.34);
  font-weight: 850;
}

.video-placeholder {
  position: relative;
  display: grid;
  min-height: 430px;
  place-items: center;
  overflow: hidden;
  background: #000000;
}

.rating-card {
  display: grid;
  width: min(70%, 520px);
  min-height: 190px;
  align-content: center;
  border: 8px solid #f1f1f1;
  padding: 1rem;
  color: #111111;
  background: #f7f7f7;
}

.rating-card strong {
  font-size: clamp(2.2rem, 7vw, 5rem);
  line-height: 0.9;
}

.rating-card span {
  font-size: clamp(1.2rem, 3vw, 2rem);
}

.play-button {
  position: absolute;
  width: 110px;
  height: 70px;
  border: 0;
  border-radius: 10px;
  color: #242424;
  background: rgba(255, 255, 255, 0.9);
  cursor: pointer;
  font-size: 2rem;
}

.video-controls {
  position: absolute;
  right: 0.85rem;
  bottom: 0.85rem;
  left: 0.85rem;
  display: flex;
  justify-content: space-between;
  color: #dce7f3;
  font-variant-numeric: tabular-nums;
}

.feature-card {
  position: relative;
  display: grid;
  gap: 0.85rem;
  align-content: start;
  padding: 1rem;
  background: rgba(31, 40, 53, 0.98);
}

.wish-button {
  position: absolute;
  top: 0.55rem;
  right: 0.55rem;
  display: grid;
  width: 38px;
  height: 38px;
  place-items: center;
  border-radius: 4px;
  color: #ffffff;
  background: var(--steam-blue-strong);
  font-size: 1.8rem;
}

.capsule-art {
  display: grid;
  min-height: 170px;
  place-items: center;
  color: #ffffff;
  background: linear-gradient(135deg, #17283a, #0d1118);
  font-size: clamp(2.6rem, 6vw, 4.2rem);
  font-weight: 950;
}

.capsule-art.tone-green {
  background: linear-gradient(135deg, #102015, #315438);
}

.capsule-art.tone-amber {
  background: linear-gradient(135deg, #2e2416, #7b5a20);
}

.capsule-art.tone-steel {
  background: linear-gradient(135deg, #273244, #d7d2c5);
  color: #111927;
}

.tag-row {
  display: flex;
  gap: 0.35rem;
  flex-wrap: wrap;
}

.tag-row span {
  border-radius: 3px;
  padding: 0.18rem 0.4rem;
  color: #d7e4f0;
  background: rgba(151, 170, 195, 0.14);
  font-size: 0.78rem;
}

.feature-card p {
  margin: 0;
  color: #b8c7d9;
  text-wrap: pretty;
}

.feature-card dl {
  margin: 0;
}

.feature-card dl div {
  display: grid;
  grid-template-columns: 90px 1fr;
  gap: 0.7rem;
}

.price-row {
  display: flex;
  justify-content: flex-end;
}

.stage-arrow {
  position: absolute;
  z-index: 1;
  top: 50%;
  border: 0;
  color: rgba(238, 242, 248, 0.86);
  background: transparent;
  cursor: pointer;
  font-size: 5rem;
  transform: translateY(-50%);
}

.stage-arrow.left {
  left: 0.6rem;
}

.stage-arrow.right {
  right: 0.6rem;
}

.collection-grid {
  display: grid;
  gap: 0.8rem;
  padding: 0 clamp(0rem, 4vw, 3rem);
}

.collection-grid header {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: 1rem;
}

.collection-grid h2,
.collection-grid span {
  margin: 0;
}

.collection-grid span {
  color: var(--steam-muted);
}

.collection-cards {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 0.85rem;
}

.collection-card {
  display: grid;
  gap: 0.35rem;
  min-height: 110px;
  align-content: end;
  border: 1px solid var(--steam-border);
  border-radius: 4px;
  padding: 0.8rem;
  background: rgba(22, 32, 45, 0.9);
}

.collection-card:hover {
  border-color: rgba(102, 192, 244, 0.55);
  background: rgba(27, 40, 56, 0.98);
}

.collection-card span {
  color: var(--steam-muted);
  font-size: 0.86rem;
}

@media (max-width: 980px) {
  .feature-stage {
    grid-template-columns: 1fr;
  }

  .stage-arrow {
    display: none;
  }

  .collection-cards {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 620px) {
  .feature-stage {
    padding: 0;
  }

  .video-placeholder {
    min-height: 300px;
  }

  .collection-cards,
  .collection-grid header {
    grid-template-columns: 1fr;
  }

  .collection-grid header {
    display: grid;
    align-items: start;
  }
}
</style>
