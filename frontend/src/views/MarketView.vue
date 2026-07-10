<script setup lang="ts">
import {
  ArrowRightLeft,
  ChevronDown,
  History,
  PackageSearch,
  RefreshCw,
  Search,
  ShieldAlert,
  ShoppingCart,
  Tag,
  X
} from '@lucide/vue';
import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  cancelOrder,
  createOrder,
  getMarket,
  getMyOrders,
  getTrades,
  getTransfers,
  matchMarket,
  type CreateMarketOrderPayload,
  type ItemTransfer,
  type MarketListing,
  type MarketOrder,
  type MarketTrade
} from '../api/marketApi';
import { useAuthStore } from '../stores/auth';

type TabKey = 'market' | 'orders' | 'trades' | 'transfers';
type SortKey = 'popular' | 'price-asc' | 'price-desc' | 'name';
type CategoryKey = 'all' | 'rifle' | 'pistol' | 'case' | 'sticker';
type AssetGalleryItem = {
  name: string;
  rarity: string;
  category: CategoryKey;
  imageUrl: string;
};

const tabs: Array<{ key: TabKey; label: string }> = [
  { key: 'market', label: '市场首页' },
  { key: 'orders', label: '我的上架物品' },
  { key: 'trades', label: '市场历史记录' },
  { key: 'transfers', label: '物品流转' }
];

const categories: Array<{ key: CategoryKey; label: string }> = [
  { key: 'all', label: '全部物品' },
  { key: 'rifle', label: '步枪' },
  { key: 'pistol', label: '手枪' },
  { key: 'case', label: '武器箱' },
  { key: 'sticker', label: '印花' }
];

const assetGallery: AssetGalleryItem[] = [
  { name: 'AK-47 | Redline', rarity: 'EPIC', category: 'rifle', imageUrl: '/assets/items/cs2-ak-redline.png' },
  { name: 'AK-47 | Neon Rider', rarity: 'LEGENDARY', category: 'rifle', imageUrl: '/assets/items/cs2-ak-neon-rider.png' },
  { name: 'AWP | Asiimov', rarity: 'LEGENDARY', category: 'rifle', imageUrl: '/assets/items/cs2-awp-asiimov.png' },
  { name: 'AWP | Dragon Lore', rarity: 'LEGENDARY', category: 'rifle', imageUrl: '/assets/items/cs2-awp-dragon-lore.png' },
  { name: 'Desert Eagle | Blaze', rarity: 'LEGENDARY', category: 'pistol', imageUrl: '/assets/items/cs2-deagle-blaze.png' },
  { name: 'Desert Eagle | Printstream', rarity: 'LEGENDARY', category: 'pistol', imageUrl: '/assets/items/cs2-deagle-printstream.png' },
  { name: 'Dreams & Nightmares Case', rarity: 'RARE', category: 'case', imageUrl: '/assets/items/cs2-dreams-case.png' },
  { name: 'FAMAS | Commemoration', rarity: 'LEGENDARY', category: 'rifle', imageUrl: '/assets/items/cs2-famas-commemoration.png' },
  { name: 'FAMAS | Mecha Industries', rarity: 'LEGENDARY', category: 'rifle', imageUrl: '/assets/items/cs2-famas-mecha.png' },
  { name: 'Galil AR | Chromatic Aberration', rarity: 'EPIC', category: 'rifle', imageUrl: '/assets/items/cs2-galil-chromatic.png' },
  { name: 'Galil AR | Phoenix Blacklight', rarity: 'EPIC', category: 'rifle', imageUrl: '/assets/items/cs2-galil-phoenix.png' },
  { name: 'Glock-18 | Fade', rarity: 'LEGENDARY', category: 'pistol', imageUrl: '/assets/items/cs2-glock-fade.png' },
  { name: 'Glock-18 | Water Elemental', rarity: 'EPIC', category: 'pistol', imageUrl: '/assets/items/cs2-glock-water.png' },
  { name: 'M4A1-S | Printstream', rarity: 'LEGENDARY', category: 'rifle', imageUrl: '/assets/items/cs2-m4a1-printstream.png' },
  { name: 'M4A4 | Howl', rarity: 'LEGENDARY', category: 'rifle', imageUrl: '/assets/items/cs2-m4a4-howl.png' },
  { name: 'M4A4 | Tooth Fairy', rarity: 'EPIC', category: 'rifle', imageUrl: '/assets/items/cs2-m4a4-tooth-fairy.png' },
  { name: 'MP9 | Dark Tide', rarity: 'RARE', category: 'rifle', imageUrl: '/assets/items/cs2-mp9-dark-tide.png' },
  { name: 'MP9 | Starlight Protector', rarity: 'LEGENDARY', category: 'rifle', imageUrl: '/assets/items/cs2-mp9-starlight.png' },
  { name: 'P250 | Cyber Shell', rarity: 'RARE', category: 'pistol', imageUrl: '/assets/items/cs2-p250-cyber-shell.png' },
  { name: 'P250 | See Ya Later', rarity: 'LEGENDARY', category: 'pistol', imageUrl: '/assets/items/cs2-p250-see-ya-later.png' },
  { name: 'P90 | Asiimov', rarity: 'LEGENDARY', category: 'rifle', imageUrl: '/assets/items/cs2-p90-asiimov.png' },
  { name: 'P90 | Elite Build', rarity: 'RARE', category: 'rifle', imageUrl: '/assets/items/cs2-p90-elite-build.png' },
  { name: 'Sticker | Crown', rarity: 'LEGENDARY', category: 'sticker', imageUrl: '/assets/items/cs2-sticker-crown.png' },
  { name: 'Tec-9 | Decimator', rarity: 'EPIC', category: 'pistol', imageUrl: '/assets/items/cs2-tec9-decimator.png' },
  { name: 'Tec-9 | Nuclear Threat', rarity: 'LEGENDARY', category: 'pistol', imageUrl: '/assets/items/cs2-tec9-nuclear-threat.png' },
  { name: 'USP-S | Kill Confirmed', rarity: 'LEGENDARY', category: 'pistol', imageUrl: '/assets/items/cs2-usp-kill-confirmed.png' },
  { name: 'USP-S | The Traitor', rarity: 'EPIC', category: 'pistol', imageUrl: '/assets/items/cs2-usp-traitor.png' }
];

const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const activeTab = computed(() => (route.meta.tab as TabKey) || 'market');
const isPlayer = computed(() => auth.currentUser?.role.toUpperCase() === 'PLAYER');
const currentPlayerLabel = computed(() =>
  auth.currentUser ? `${auth.currentUser.account} / ${auth.currentUser.principalId}` : '尚未登录'
);

const loading = ref(false);
const actionLoading = ref(false);
const errorMessage = ref('');
const successMessage = ref('');
const searchQuery = ref('');
const category = ref<CategoryKey>('all');
const sortKey = ref<SortKey>('popular');
const includeDescription = ref(false);
const showOrderComposer = ref(false);
const selectedListing = ref<MarketListing | null>(null);

const listings = ref<MarketListing[]>([]);
const orders = ref<MarketOrder[]>([]);
const trades = ref<MarketTrade[]>([]);
const transfers = ref<ItemTransfer[]>([]);

const orderForm = reactive<CreateMarketOrderPayload>({
  orderType: 'BUY',
  templateId: 'ITPL_CS2_AK_REDLINE',
  itemId: 'ITEM_CS2_002',
  targetPrice: 50
});

const transferItemId = ref('ITEM_CS2_001');
const matchTemplateId = ref('ITPL_CS2_AK_REDLINE');
const matchingOrders = computed(() => orders.value.filter((order) => order.status === 'MATCHING'));

const filteredListings = computed(() => {
  const query = searchQuery.value.trim().toLowerCase();
  const rows = listings.value.filter((item) => {
    const matchesQuery =
      !query ||
      item.itemName.toLowerCase().includes(query) ||
      item.templateId.toLowerCase().includes(query) ||
      (includeDescription.value && item.rarity.toLowerCase().includes(query));
    return matchesQuery && (category.value === 'all' || itemCategory(item) === category.value);
  });

  return [...rows].sort((left, right) => {
    if (sortKey.value === 'price-asc') {
      return listingPrice(left) - listingPrice(right);
    }
    if (sortKey.value === 'price-desc') {
      return listingPrice(right) - listingPrice(left);
    }
    if (sortKey.value === 'name') {
      return left.itemName.localeCompare(right.itemName);
    }
    return listingCount(right) - listingCount(left);
  });
});

const filteredAssetGallery = computed(() => {
  const query = searchQuery.value.trim().toLowerCase();
  return assetGallery.filter((item) => {
    const matchesQuery =
      !query ||
      item.name.toLowerCase().includes(query) ||
      (includeDescription.value && item.rarity.toLowerCase().includes(query));
    return matchesQuery && (category.value === 'all' || item.category === category.value);
  });
});

function money(value?: number | null) {
  return typeof value === 'number' ? `¥${value.toFixed(2)}` : '暂无挂单';
}

function versionedImageUrl(value?: string | null) {
  if (!value) return '';
  return `${value}${value.includes('?') ? '&' : '?'}v=7787514-transparent`;
}

function listingPrice(item: MarketListing) {
  return item.lowestSellPrice ?? item.highestBuyPrice ?? Number.MAX_SAFE_INTEGER;
}

function listingCount(item: MarketListing) {
  return item.activeBuyCount + item.activeSellCount;
}

function shortId(value: string) {
  return value.length > 12 ? `${value.slice(0, 8)}...` : value;
}

function formatTime(value: string) {
  return new Date(value).toLocaleString('zh-CN', { hour12: false });
}

function itemCategory(item: MarketListing): CategoryKey {
  const text = `${item.itemName} ${item.templateId}`.toLowerCase();
  if (text.includes('case')) return 'case';
  if (text.includes('sticker')) return 'sticker';
  if (text.includes('glock') || text.includes('usp') || text.includes('deagle')) return 'pistol';
  return 'rifle';
}

function rarityLabel(rarity: string) {
  const labels: Record<string, string> = {
    COMMON: '普通级',
    UNCOMMON: '消费级',
    RARE: '军规级',
    EPIC: '受限级',
    LEGENDARY: '保密级'
  };
  return labels[rarity.toUpperCase()] || rarity;
}

function orderTypeLabel(type: string) {
  return type === 'SELL' ? '出售' : '求购';
}

function statusLabel(status: string) {
  const labels: Record<string, string> = {
    MATCHING: '等待成交',
    MATCHED: '已成交',
    CANCELLED: '已取消',
    CANCELED: '已取消'
  };
  return labels[status.toUpperCase()] || status;
}

function openTab(tab: TabKey) {
  const paths: Record<TabKey, string> = {
    market: '/market',
    orders: '/market/orders',
    trades: '/market/trades',
    transfers: '/market/transfers'
  };
  router.push(paths[tab]);
}

function openOrder(item: MarketListing, orderType: 'BUY' | 'SELL' = 'BUY') {
  selectedListing.value = item;
  orderForm.orderType = orderType;
  orderForm.templateId = item.templateId;
  orderForm.targetPrice = item.lowestSellPrice ?? item.highestBuyPrice ?? 1;
  matchTemplateId.value = item.templateId;
  showOrderComposer.value = true;
}

async function refreshAll() {
  loading.value = true;
  errorMessage.value = '';
  try {
    const [marketRows, tradeRows] = await Promise.all([getMarket('GAME_CS2'), getTrades()]);
    listings.value = marketRows;
    trades.value = tradeRows;
    orders.value = isPlayer.value ? await getMyOrders() : [];
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '刷新市场失败';
  } finally {
    loading.value = false;
  }
}

async function submitOrder() {
  if (!isPlayer.value) {
    errorMessage.value = '请先登录玩家账号后再创建挂单。';
    return;
  }
  if (!orderForm.templateId.trim()) {
    errorMessage.value = '饰品模板不能为空。';
    return;
  }
  if (orderForm.targetPrice <= 0) {
    errorMessage.value = '价格必须大于 0。';
    return;
  }
  if (orderForm.orderType === 'SELL' && !orderForm.itemId?.trim()) {
    errorMessage.value = '出售物品时必须填写饰品编号。';
    return;
  }

  await runAction(async () => {
    await createOrder({
      ...orderForm,
      itemId: orderForm.orderType === 'SELL' ? orderForm.itemId?.trim() : null
    });
    successMessage.value = `${orderTypeLabel(orderForm.orderType)}挂单已创建。`;
    showOrderComposer.value = false;
    await refreshAll();
  });
}

async function cancelSelectedOrder(order: MarketOrder) {
  await runAction(async () => {
    await cancelOrder(order.marketOrderId);
    successMessage.value = '挂单已取消。';
    await refreshAll();
  });
}

async function submitMatch() {
  await runAction(async () => {
    const trade = await matchMarket(matchTemplateId.value.trim());
    successMessage.value = `成交 ${shortId(trade.tradeId)}，物品 ${trade.itemId} 已完成流转。`;
    showOrderComposer.value = false;
    await refreshAll();
  });
}

async function loadTransfers() {
  if (!transferItemId.value.trim()) {
    errorMessage.value = '请输入饰品编号。';
    return;
  }
  await runAction(async () => {
    transfers.value = await getTransfers(transferItemId.value.trim());
    successMessage.value = '物品流转记录已更新。';
  });
}

async function runAction(action: () => Promise<void>) {
  actionLoading.value = true;
  errorMessage.value = '';
  successMessage.value = '';
  try {
    await action();
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '操作失败';
  } finally {
    actionLoading.value = false;
  }
}

onMounted(async () => {
  await refreshAll();
});
</script>

<template>
  <div class="community-market">
    <header class="market-hero">
      <div class="hero-copy">
        <span class="steam-mark">STEAM</span>
        <h1>社区市场</h1>
        <p>与社区成员买卖 Counter-Strike 2 饰品</p>
      </div>
      <div class="hero-search">
        <Search :size="18" aria-hidden="true" />
        <input v-model="searchQuery" type="search" placeholder="搜索市场中的物品..." aria-label="搜索市场物品" />
        <button type="button" title="搜索" aria-label="搜索">
          <Search :size="17" />
        </button>
      </div>
    </header>

    <nav class="account-tabs" aria-label="市场导航">
      <button
        v-for="tab in tabs"
        :key="tab.key"
        type="button"
        :class="{ active: activeTab === tab.key }"
        @click="openTab(tab.key)"
      >
        <ShoppingCart v-if="tab.key === 'market'" :size="16" />
        <Tag v-else-if="tab.key === 'orders'" :size="16" />
        <History v-else-if="tab.key === 'trades'" :size="16" />
        <ArrowRightLeft v-else :size="16" />
        {{ tab.label }}
        <span v-if="tab.key === 'orders'">({{ orders.length }})</span>
      </button>
      <button class="refresh-button" type="button" :disabled="loading" title="刷新市场" @click="refreshAll">
        <RefreshCw :size="17" :class="{ spinning: loading }" />
        <span>{{ currentPlayerLabel }}</span>
      </button>
    </nav>

    <div v-if="errorMessage || successMessage" class="message-stack" aria-live="polite">
      <p v-if="errorMessage" class="market-message error">
        <ShieldAlert :size="18" />
        {{ errorMessage }}
      </p>
      <p v-if="successMessage" class="market-message success">{{ successMessage }}</p>
    </div>

    <section v-if="activeTab === 'market'" class="browse-layout">
      <aside class="filter-sidebar">
        <div class="filter-heading">
          <div>
            <span>筛选条件</span>
            <strong>Counter-Strike 2</strong>
          </div>
          <ChevronDown :size="18" />
        </div>

        <label class="sidebar-search">
          <Search :size="16" />
          <input v-model="searchQuery" type="search" placeholder="筛选结果..." />
        </label>

        <label class="check-row">
          <input v-model="includeDescription" type="checkbox" />
          <span>搜索内容包含物品品质</span>
        </label>

        <div class="category-list">
          <button
            v-for="item in categories"
            :key="item.key"
            type="button"
            :class="{ active: category === item.key }"
            @click="category = item.key"
          >
            <span class="category-art">{{ item.label.slice(0, 1) }}</span>
            <span>{{ item.label }}</span>
            <small>{{ item.key === 'all' ? listings.length : listings.filter((row) => itemCategory(row) === item.key).length }}</small>
          </button>
        </div>

        <div class="market-security">
          <ShieldAlert :size="20" />
          <p>所有交易均绑定当前登录玩家身份，并由平台完成钱包与库存校验。</p>
        </div>
      </aside>

      <div class="market-results">
        <div class="result-toolbar">
          <div>
            <p>找到 {{ filteredListings.length }} 个搜索结果</p>
            <span>显示社区中可交易的饰品模板</span>
          </div>
          <label>
            <span>排序方式</span>
            <select v-model="sortKey">
              <option value="popular">热门程度</option>
              <option value="price-asc">价格从低到高</option>
              <option value="price-desc">价格从高到低</option>
              <option value="name">名称</option>
            </select>
          </label>
        </div>

        <div v-if="loading" class="market-empty">
          <RefreshCw :size="30" class="spinning" />
          <strong>正在载入社区市场...</strong>
        </div>
        <div v-else-if="!filteredListings.length" class="market-empty">
          <PackageSearch :size="34" />
          <strong>没有找到符合条件的物品</strong>
          <span>请尝试其他关键词或分类。</span>
        </div>
        <div v-else class="item-grid">
          <article
            v-for="item in filteredListings"
            :key="item.templateId"
            class="market-item"
            :class="`rarity-${item.rarity.toLowerCase()}`"
          >
            <button class="item-main" type="button" @click="openOrder(item)">
              <div class="item-meta">
                <span>{{ rarityLabel(item.rarity) }}</span>
                <h2>{{ item.itemName }}</h2>
              </div>
              <div class="item-image">
                <img v-if="item.imageUrl" :src="versionedImageUrl(item.imageUrl)" :alt="item.itemName" />
                <PackageSearch v-else :size="64" />
              </div>
              <div class="item-foot">
                <span>在售数量：{{ item.activeSellCount.toLocaleString('zh-CN') }}</span>
                <span>求购数量：{{ item.activeBuyCount.toLocaleString('zh-CN') }}</span>
              </div>
            </button>
            <button class="price-button" type="button" @click="openOrder(item)">
              <span>起价</span>
              <strong>{{ money(item.lowestSellPrice ?? item.highestBuyPrice) }}</strong>
            </button>
          </article>
        </div>

        <section class="asset-showcase" aria-labelledby="asset-showcase-title">
          <div class="asset-showcase-head">
            <div>
              <span>本地饰品资源</span>
              <h2 id="asset-showcase-title">饰品图鉴</h2>
            </div>
            <small>{{ filteredAssetGallery.length }} / {{ assetGallery.length }}</small>
          </div>
          <div class="asset-grid">
            <article
              v-for="item in filteredAssetGallery"
              :key="item.imageUrl"
              class="asset-card"
              :class="`rarity-${item.rarity.toLowerCase()}`"
            >
              <img :src="versionedImageUrl(item.imageUrl)" :alt="item.name" loading="lazy" />
              <div>
                <span>{{ rarityLabel(item.rarity) }}</span>
                <strong>{{ item.name }}</strong>
              </div>
            </article>
          </div>
        </section>
      </div>
    </section>

    <section v-else-if="activeTab === 'orders'" class="steam-section">
      <header>
        <div>
          <span>我的市场</span>
          <h2>我的上架物品与订购单</h2>
        </div>
        <button type="button" :disabled="!listings.length" @click="listings[0] && openOrder(listings[0], 'SELL')">
          <Tag :size="17" />
          出售物品
        </button>
      </header>
      <div v-if="!isPlayer" class="market-empty compact">
        <ShieldAlert :size="30" />
        <strong>请使用玩家账号登录</strong>
        <span>挂单只对已认证玩家开放。</span>
      </div>
      <div v-else-if="!orders.length" class="market-empty compact">
        <PackageSearch :size="30" />
        <strong>您目前没有市场挂单</strong>
      </div>
      <div v-else class="steam-rows">
        <article v-for="order in orders" :key="order.marketOrderId">
          <div class="row-art"><Tag :size="22" /></div>
          <div class="row-main">
            <strong>{{ order.itemName }}</strong>
            <span>{{ orderTypeLabel(order.orderType) }} · {{ shortId(order.marketOrderId) }}</span>
          </div>
          <span>{{ formatTime(order.createTime) }}</span>
          <strong>{{ money(order.targetPrice) }}</strong>
          <span class="status-chip">{{ statusLabel(order.status) }}</span>
          <button
            type="button"
            :disabled="order.status !== 'MATCHING' || actionLoading"
            @click="cancelSelectedOrder(order)"
          >
            取消
          </button>
        </article>
      </div>
      <p class="section-foot">等待撮合：{{ matchingOrders.length }} 条</p>
    </section>

    <section v-else-if="activeTab === 'trades'" class="steam-section">
      <header>
        <div>
          <span>社区市场</span>
          <h2>最近成交记录</h2>
        </div>
      </header>
      <div v-if="!trades.length" class="market-empty compact">
        <History :size="30" />
        <strong>暂无成交记录</strong>
      </div>
      <div v-else class="steam-rows trade-rows">
        <article v-for="trade in trades" :key="trade.tradeId">
          <div class="row-art"><History :size="22" /></div>
          <div class="row-main">
            <strong>{{ trade.itemName }}</strong>
            <span>{{ shortId(trade.tradeId) }} · {{ trade.itemId }}</span>
          </div>
          <span>{{ trade.sellerId }} → {{ trade.buyerId }}</span>
          <strong>{{ money(trade.tradePrice) }}</strong>
          <time>{{ formatTime(trade.tradeTime) }}</time>
        </article>
      </div>
    </section>

    <section v-else class="steam-section">
      <header>
        <div>
          <span>库存账本</span>
          <h2>物品流转记录</h2>
        </div>
        <form class="transfer-search" @submit.prevent="loadTransfers">
          <input v-model="transferItemId" placeholder="输入物品编号" />
          <button type="submit" :disabled="actionLoading">
            <Search :size="17" />
            查询
          </button>
        </form>
      </header>
      <div v-if="!transfers.length" class="market-empty compact">
        <ArrowRightLeft :size="30" />
        <strong>输入物品编号查询流转记录</strong>
      </div>
      <div v-else class="steam-rows transfer-rows">
        <article v-for="transfer in transfers" :key="transfer.transferId">
          <div class="row-art"><ArrowRightLeft :size="22" /></div>
          <div class="row-main">
            <strong>{{ transfer.itemName }}</strong>
            <span>{{ transfer.transferType }}</span>
          </div>
          <span>{{ transfer.fromUserId ?? 'SYSTEM' }} → {{ transfer.toUserId }}</span>
          <time>{{ formatTime(transfer.transferTime) }}</time>
        </article>
      </div>
    </section>

    <div v-if="showOrderComposer && selectedListing" class="order-overlay" @click.self="showOrderComposer = false">
      <section class="order-composer" role="dialog" aria-modal="true" aria-labelledby="order-title">
        <header>
          <div>
            <span>社区市场交易</span>
            <h2 id="order-title">{{ selectedListing.itemName }}</h2>
          </div>
          <button type="button" title="关闭" aria-label="关闭" @click="showOrderComposer = false">
            <X :size="20" />
          </button>
        </header>

        <div class="composer-body">
          <div class="composer-item" :class="`rarity-${selectedListing.rarity.toLowerCase()}`">
            <img
              v-if="selectedListing.imageUrl"
              :src="versionedImageUrl(selectedListing.imageUrl)"
              :alt="selectedListing.itemName"
            />
            <span>{{ rarityLabel(selectedListing.rarity) }}</span>
            <strong>{{ selectedListing.templateId }}</strong>
          </div>

          <form class="order-form" @submit.prevent="submitOrder">
            <div class="segmented-control">
              <button
                type="button"
                :class="{ active: orderForm.orderType === 'BUY' }"
                @click="orderForm.orderType = 'BUY'"
              >
                创建求购单
              </button>
              <button
                type="button"
                :class="{ active: orderForm.orderType === 'SELL' }"
                @click="orderForm.orderType = 'SELL'"
              >
                出售物品
              </button>
            </div>
            <label v-if="orderForm.orderType === 'SELL'">
              <span>库存物品编号</span>
              <input v-model="orderForm.itemId" placeholder="例如 ITEM_CS2_002" />
            </label>
            <label>
              <span>您的价格</span>
              <input v-model.number="orderForm.targetPrice" type="number" min="0.01" step="0.01" />
            </label>
            <div class="price-summary">
              <span>当前最低售价</span>
              <strong>{{ money(selectedListing.lowestSellPrice) }}</strong>
              <span>当前最高求购价</span>
              <strong>{{ money(selectedListing.highestBuyPrice) }}</strong>
            </div>
            <p v-if="!isPlayer" class="login-note">请先登录玩家账号后再提交交易。</p>
            <div class="composer-actions">
              <button class="match-button" type="button" :disabled="actionLoading || !isPlayer" @click="submitMatch">
                <ArrowRightLeft :size="17" />
                撮合测试
              </button>
              <button class="submit-button" type="submit" :disabled="actionLoading || !isPlayer">
                <ShoppingCart :size="17" />
                {{ actionLoading ? '处理中...' : orderTypeLabel(orderForm.orderType) }}
              </button>
            </div>
          </form>
        </div>
      </section>
    </div>
  </div>
</template>

<style scoped>
.community-market {
  display: grid;
  gap: 18px;
  color: #d6d7d8;
}

.market-hero {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(340px, 520px);
  align-items: center;
  gap: 32px;
  min-height: 124px;
  padding: 26px 32px;
  border: 1px solid rgba(104, 134, 122, 0.35);
  border-radius: 6px;
  background:
    linear-gradient(90deg, rgba(65, 94, 82, 0.82), rgba(43, 54, 63, 0.96)),
    #29333c;
  box-shadow: inset 0 1px rgba(255, 255, 255, 0.04);
}

.hero-copy {
  display: flex;
  align-items: baseline;
  gap: 10px;
  flex-wrap: wrap;
}

.hero-copy h1,
.hero-copy p {
  margin: 0;
}

.hero-copy h1 {
  color: #fff;
  font-size: 1.8rem;
  font-weight: 400;
}

.hero-copy p {
  flex-basis: 100%;
  color: #aeb6bf;
  font-size: 0.9rem;
}

.steam-mark {
  color: #fff;
  font-size: 1.15rem;
  font-weight: 800;
}

.hero-search,
.sidebar-search {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) auto;
  align-items: center;
  background: #171d23;
}

.hero-search {
  min-height: 48px;
  border: 1px solid #10151a;
  padding-left: 14px;
  color: #7d8994;
}

.hero-search input,
.sidebar-search input {
  border: 0;
  border-radius: 0;
  background: transparent;
}

.hero-search button,
.account-tabs button,
.category-list button,
.price-button,
.steam-section button,
.order-composer button {
  border: 0;
  border-radius: 2px;
  color: inherit;
  cursor: pointer;
}

.hero-search button {
  display: grid;
  width: 44px;
  height: 44px;
  place-items: center;
  background: #2d3944;
}

.account-tabs {
  display: flex;
  align-items: stretch;
  min-height: 48px;
  background: #0f1822;
}

.account-tabs button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  min-width: 170px;
  padding: 0 18px;
  color: #8493a3;
  background: transparent;
}

.account-tabs button:hover,
.account-tabs button.active {
  color: #fff;
  background: linear-gradient(180deg, #6f943c, #4e702a);
}

.account-tabs .refresh-button {
  min-width: 0;
  margin-left: auto;
  color: #aeb6bf;
}

.account-tabs .refresh-button:hover {
  color: #fff;
  background: #263747;
}

.message-stack {
  display: grid;
  gap: 8px;
}

.market-message {
  display: flex;
  align-items: center;
  gap: 9px;
  margin: 0;
  padding: 12px 16px;
  border-left: 3px solid currentColor;
  background: #17212c;
}

.market-message.error {
  color: #ff8d8d;
}

.market-message.success {
  color: #9bcf62;
}

.browse-layout {
  display: grid;
  grid-template-columns: 278px minmax(0, 1fr);
  gap: 28px;
  align-items: start;
  padding-top: 18px;
}

.filter-sidebar {
  display: grid;
  gap: 12px;
}

.filter-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  min-height: 58px;
  padding: 0 14px;
  background: #303b49;
}

.filter-heading div {
  display: grid;
  gap: 2px;
}

.filter-heading span {
  color: #8c98a5;
  font-size: 0.75rem;
}

.filter-heading strong {
  color: #fff;
  font-size: 0.95rem;
}

.sidebar-search {
  grid-template-columns: auto minmax(0, 1fr);
  min-height: 42px;
  padding-left: 12px;
  color: #8e9aa6;
  background: #3a4555;
}

.check-row {
  display: flex;
  grid-template-columns: none;
  align-items: center;
  gap: 9px;
  padding: 3px 4px 7px;
  color: #8f99a5;
  font-size: 0.8rem;
  font-weight: 400;
}

.check-row input {
  width: 18px;
  height: 18px;
  accent-color: #6c9139;
}

.category-list {
  display: grid;
  gap: 8px;
}

.category-list button {
  display: grid;
  grid-template-columns: 42px minmax(0, 1fr) auto;
  align-items: center;
  gap: 10px;
  min-height: 58px;
  padding: 7px 12px;
  color: #d5d9dd;
  text-align: left;
  background: linear-gradient(90deg, #2e3b47, #343a43);
}

.category-list button:hover,
.category-list button.active {
  color: #fff;
  background: linear-gradient(90deg, #3f5865, #36404b);
  box-shadow: inset 3px 0 #66c0f4;
}

.category-list small {
  color: #929da8;
}

.category-art {
  display: grid;
  width: 38px;
  height: 38px;
  place-items: center;
  color: #d7e9f4;
  background: #1b2731;
  font-weight: 800;
}

.market-security {
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 10px;
  margin-top: 8px;
  padding: 14px;
  color: #87929d;
  background: #111a23;
}

.market-security p {
  margin: 0;
  font-size: 0.78rem;
}

.market-results {
  min-width: 0;
}

.result-toolbar {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: 20px;
  margin-bottom: 12px;
}

.result-toolbar p,
.result-toolbar span {
  margin: 0;
}

.result-toolbar p {
  color: #9ca6b1;
}

.result-toolbar > div > span {
  color: #687685;
  font-size: 0.78rem;
}

.result-toolbar label {
  display: flex;
  grid-template-columns: none;
  align-items: center;
  gap: 9px;
  color: #87939f;
  font-weight: 400;
}

.result-toolbar select {
  width: 178px;
  border: 0;
  border-radius: 2px;
  background: #3a4555;
}

.item-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 18px;
}

.market-item {
  position: relative;
  min-width: 0;
  border: 1px solid #56616d;
  background:
    linear-gradient(180deg, rgba(96, 106, 119, 0.28), transparent 18%),
    #1f2630;
}

.market-item::before {
  position: absolute;
  inset: 0 0 auto;
  height: 3px;
  content: "";
  background: #7692a7;
}

.market-item.rarity-uncommon::before {
  background: #5e98d9;
}

.market-item.rarity-rare::before {
  background: #4b69ff;
}

.market-item.rarity-epic::before {
  background: #8847ff;
}

.market-item.rarity-legendary::before {
  background: #d32ce6;
}

.item-main {
  display: grid;
  width: 100%;
  padding: 14px 14px 0;
  color: inherit;
  text-align: left;
  background: transparent;
}

.item-meta {
  min-height: 55px;
}

.item-meta span {
  color: #8f99a3;
  font-size: 0.75rem;
}

.item-meta h2 {
  margin: 1px 0 0;
  overflow: hidden;
  color: #f2f2f2;
  font-size: 0.98rem;
  line-height: 1.28;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.item-image {
  display: grid;
  height: 170px;
  place-items: center;
  padding: 8px;
  color: #556474;
}

.item-image img {
  width: 100%;
  height: 100%;
  object-fit: contain;
  filter: drop-shadow(0 16px 12px rgba(0, 0, 0, 0.42));
  transition: transform 0.18s ease;
}

.market-item:hover .item-image img {
  transform: scale(1.045);
}

.item-foot {
  display: flex;
  justify-content: space-between;
  gap: 8px;
  min-height: 42px;
  color: #8f989f;
  font-size: 0.73rem;
}

.price-button {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 7px;
  width: calc(100% - 28px);
  min-height: 42px;
  margin: 0 14px 14px;
  color: #d7dce1;
  background: #3a4555;
}

.price-button span {
  color: #9aa4ad;
  font-size: 0.74rem;
}

.price-button:hover {
  color: #fff;
  background: linear-gradient(180deg, #79a83b, #567b2a);
}

.asset-showcase {
  display: grid;
  gap: 12px;
  margin-top: 24px;
  padding: 16px;
  border: 1px solid #293745;
  background: #15202b;
}

.asset-showcase-head {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: 16px;
}

.asset-showcase-head span,
.asset-showcase-head small {
  color: #7890a4;
  font-size: 0.75rem;
}

.asset-showcase-head h2 {
  margin: 2px 0 0;
  color: #fff;
  font-size: 1.1rem;
}

.asset-grid {
  display: grid;
  grid-template-columns: repeat(6, minmax(0, 1fr));
  gap: 10px;
}

.asset-card {
  position: relative;
  display: grid;
  gap: 8px;
  min-width: 0;
  min-height: 172px;
  padding: 10px;
  border: 1px solid #354556;
  background:
    linear-gradient(180deg, rgba(97, 109, 123, 0.22), transparent 26%),
    #202832;
}

.asset-card::before {
  position: absolute;
  inset: 0 0 auto;
  height: 2px;
  content: "";
  background: #7692a7;
}

.asset-card.rarity-rare::before {
  background: #4b69ff;
}

.asset-card.rarity-epic::before {
  background: #8847ff;
}

.asset-card.rarity-legendary::before {
  background: #d32ce6;
}

.asset-card img {
  width: 100%;
  height: 96px;
  object-fit: contain;
  filter: drop-shadow(0 12px 10px rgba(0, 0, 0, 0.38));
}

.asset-card div {
  display: grid;
  gap: 2px;
  min-width: 0;
}

.asset-card span {
  color: #8794a1;
  font-size: 0.68rem;
}

.asset-card strong {
  overflow: hidden;
  color: #e6ebef;
  font-size: 0.78rem;
  line-height: 1.25;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.market-empty {
  display: grid;
  min-height: 320px;
  place-items: center;
  align-content: center;
  gap: 8px;
  color: #778593;
  background: rgba(17, 26, 35, 0.72);
}

.market-empty span {
  font-size: 0.85rem;
}

.market-empty.compact {
  min-height: 190px;
}

.steam-section {
  overflow: hidden;
  border: 1px solid #293745;
  background: #15202b;
}

.steam-section > header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  min-height: 90px;
  padding: 18px 22px;
  background: linear-gradient(90deg, #213548, #18232f);
}

.steam-section header span,
.steam-section header h2 {
  margin: 0;
}

.steam-section header span {
  color: #7890a4;
  font-size: 0.75rem;
  text-transform: uppercase;
}

.steam-section header h2 {
  font-size: 1.25rem;
}

.steam-section header button,
.transfer-search button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  min-height: 38px;
  padding: 0 14px;
  color: #fff;
  background: linear-gradient(180deg, #72a238, #4d7226);
}

.steam-rows {
  display: grid;
}

.steam-rows article {
  display: grid;
  grid-template-columns: 48px minmax(220px, 1.7fr) minmax(150px, 1fr) 110px 110px 80px;
  align-items: center;
  gap: 14px;
  min-height: 76px;
  padding: 10px 18px;
  border-bottom: 1px solid #293745;
  color: #aeb8c2;
}

.steam-rows article:hover {
  background: #1d2c3a;
}

.steam-rows article > button {
  min-height: 32px;
  background: #334455;
}

.row-art {
  display: grid;
  width: 44px;
  height: 44px;
  place-items: center;
  color: #66c0f4;
  background: #101923;
}

.row-main {
  display: grid;
  gap: 3px;
}

.row-main strong {
  color: #fff;
}

.row-main span,
.steam-rows time {
  color: #758493;
  font-size: 0.78rem;
}

.status-chip {
  color: #b5d98d;
}

.trade-rows article {
  grid-template-columns: 48px minmax(220px, 1.7fr) minmax(180px, 1fr) 100px 170px;
}

.transfer-rows article {
  grid-template-columns: 48px minmax(220px, 1.7fr) minmax(240px, 1fr) 180px;
}

.section-foot {
  margin: 0;
  padding: 12px 18px;
  color: #778593;
  font-size: 0.8rem;
  background: #111923;
}

.transfer-search {
  display: grid;
  grid-template-columns: minmax(220px, 320px) auto;
  gap: 8px;
}

.order-overlay {
  position: fixed;
  z-index: 50;
  inset: 0;
  display: grid;
  place-items: center;
  padding: 24px;
  background: rgba(4, 8, 12, 0.78);
  backdrop-filter: blur(5px);
}

.order-composer {
  width: min(760px, 100%);
  border: 1px solid #56616d;
  background: #202a35;
  box-shadow: 0 26px 80px rgba(0, 0, 0, 0.55);
}

.order-composer > header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 18px 20px;
  background: linear-gradient(90deg, #314654, #202b36);
}

.order-composer header span {
  color: #8e9aa6;
  font-size: 0.72rem;
  text-transform: uppercase;
}

.order-composer header h2 {
  margin: 2px 0 0;
  font-size: 1.2rem;
}

.order-composer header button {
  display: grid;
  width: 36px;
  height: 36px;
  place-items: center;
  background: #17212b;
}

.composer-body {
  display: grid;
  grid-template-columns: 280px minmax(0, 1fr);
}

.composer-item {
  display: grid;
  align-content: center;
  gap: 8px;
  min-height: 360px;
  padding: 24px;
  background: #171f29;
}

.composer-item img {
  width: 100%;
  height: 190px;
  object-fit: contain;
  filter: drop-shadow(0 18px 14px rgba(0, 0, 0, 0.42));
}

.composer-item span {
  color: #9b6cff;
  font-size: 0.78rem;
}

.composer-item strong {
  overflow-wrap: anywhere;
  color: #8190a0;
  font-size: 0.75rem;
}

.order-form {
  display: grid;
  align-content: start;
  gap: 16px;
  padding: 24px;
}

.segmented-control {
  display: grid;
  grid-template-columns: 1fr 1fr;
  background: #151d26;
}

.segmented-control button {
  min-height: 42px;
  color: #8e99a4;
  background: transparent;
}

.segmented-control button.active {
  color: #fff;
  background: #3a4a59;
  box-shadow: inset 0 -3px #75a43b;
}

.order-form label span {
  color: #9ca7b2;
  font-size: 0.8rem;
}

.price-summary {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 8px;
  padding: 13px;
  color: #8e9aa6;
  background: #17212b;
}

.price-summary strong {
  color: #fff;
}

.login-note {
  margin: 0;
  color: #e6b45c;
  font-size: 0.82rem;
}

.composer-actions {
  display: flex;
  justify-content: flex-end;
  gap: 9px;
}

.composer-actions button {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  min-height: 40px;
  padding: 0 15px;
}

.match-button {
  background: #344454;
}

.submit-button {
  color: #fff;
  background: linear-gradient(180deg, #78a83b, #52782a);
}

.spinning {
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

@media (max-width: 1180px) {
  .item-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .asset-grid {
    grid-template-columns: repeat(4, minmax(0, 1fr));
  }
}

@media (max-width: 900px) {
  .market-hero,
  .browse-layout,
  .composer-body {
    grid-template-columns: 1fr;
  }

  .account-tabs {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .account-tabs button {
    min-width: 0;
    min-height: 52px;
    padding: 8px 10px;
  }

  .account-tabs .refresh-button {
    grid-column: 1 / -1;
    margin-left: 0;
  }

  .filter-sidebar {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .filter-heading,
  .sidebar-search,
  .check-row,
  .market-security {
    grid-column: 1 / -1;
  }

  .category-list {
    grid-column: 1 / -1;
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .item-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .asset-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .steam-rows article,
  .trade-rows article,
  .transfer-rows article {
    grid-template-columns: 48px minmax(0, 1fr);
  }

  .steam-rows article > :not(.row-art, .row-main) {
    grid-column: 2;
  }

  .composer-item {
    min-height: 220px;
  }
}

@media (max-width: 600px) {
  .market-hero {
    padding: 20px;
  }

  .hero-copy h1 {
    font-size: 1.5rem;
  }

  .result-toolbar,
  .steam-section > header {
    align-items: stretch;
    flex-direction: column;
  }

  .filter-sidebar {
    grid-template-columns: 1fr;
  }

  .category-list,
  .item-grid,
  .asset-grid {
    grid-template-columns: 1fr;
  }

  .item-image {
    height: 210px;
  }

  .transfer-search {
    grid-template-columns: 1fr;
  }

  .order-overlay {
    align-items: end;
    padding: 0;
  }

  .order-composer {
    max-height: 92vh;
    overflow-y: auto;
  }

  .composer-actions {
    flex-direction: column;
  }

  .composer-actions button {
    justify-content: center;
  }
}
</style>
