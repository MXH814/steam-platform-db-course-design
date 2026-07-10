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
type CategoryKey = 'pistol' | 'smg' | 'rifle' | 'sniper' | 'case' | 'sticker';

const tabs: Array<{ key: TabKey; label: string }> = [
  { key: 'market', label: '市场首页' },
  { key: 'orders', label: '我的上架物品' },
  { key: 'trades', label: '市场交易记录' },
  { key: 'transfers', label: '物品流通' }
];

const categories: Array<{ key: CategoryKey; label: string; imageUrl?: string }> = [
  { key: 'pistol', label: '手枪', imageUrl: '/assets/items/cs2/cs2-deagle-blaze.png' },
  { key: 'smg', label: '微型冲锋枪', imageUrl: '/assets/items/cs2/cs2-mp9-starlight.png' },
  { key: 'rifle', label: '步枪', imageUrl: '/assets/items/cs2/cs2-ak-redline.png' },
  { key: 'sniper', label: '狙击步枪', imageUrl: '/assets/items/cs2/cs2-awp-asiimov.png' },
  { key: 'case', label: '武器箱', imageUrl: '/assets/items/cs2/cs2-dreams-case.png' },
  { key: 'sticker', label: '印花', imageUrl: '/assets/items/cs2/cs2-sticker-crown.png' }
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
const selectedCategories = ref<CategoryKey[]>([]);
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
const activeOrders = computed(() => orders.value.filter((order) => order.status === 'MATCHING'));
const canBuySelectedNow = computed(() => Boolean(selectedListing.value?.lowestSellPrice && selectedListing.value.activeSellCount > 0));
const myTrades = computed(() => {
  const principalId = auth.currentUser?.principalId;
  if (!principalId) {
    return [];
  }

  return trades.value.filter((trade) => trade.buyerId === principalId || trade.sellerId === principalId);
});

const activeCategorySet = computed(() => new Set(selectedCategories.value));

const filteredListings = computed(() => {
  const query = searchQuery.value.trim().toLowerCase();
  const rows = listings.value.filter((item) => {
    const matchesQuery =
      !query ||
      item.itemName.toLowerCase().includes(query) ||
      item.templateId.toLowerCase().includes(query) ||
      (includeDescription.value && item.rarity.toLowerCase().includes(query));
    return matchesQuery && (!activeCategorySet.value.size || activeCategorySet.value.has(itemCategory(item)));
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

function money(value?: number | null) {
  return typeof value === 'number' ? `¥${value.toFixed(2)}` : '暂无挂单';
}

function categoryLabel(key: CategoryKey) {
  return categories.find((item) => item.key === key)?.label ?? key;
}

function toggleCategory(key: CategoryKey) {
  selectedCategories.value = activeCategorySet.value.has(key)
    ? selectedCategories.value.filter((item) => item !== key)
    : [...selectedCategories.value, key];
}

function removeCategory(key: CategoryKey) {
  selectedCategories.value = selectedCategories.value.filter((item) => item !== key);
}

function versionedImageUrl(value?: string | null) {
  if (!value) return '';
  return `${value}${value.includes('?') ? '&' : '?'}v=7d7c765-main`;
}

function templateImageUrl(templateId: string) {
  return listings.value.find((item) => item.templateId === templateId)?.imageUrl ?? null;
}

function tradeImageUrl(trade: MarketTrade) {
  return templateImageUrl(trade.templateId);
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
  if (text.includes('awp') || text.includes('ssg') || text.includes('scar') || text.includes('g3sg1')) return 'sniper';
  if (text.includes('mp') || text.includes('p90') || text.includes('mac') || text.includes('ump') || text.includes('bizon')) return 'smg';
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

function openOrder(item: MarketListing) {
  selectedListing.value = item;
  orderForm.orderType = 'BUY';
  orderForm.templateId = item.templateId;
  orderForm.itemId = null;
  orderForm.targetPrice = item.lowestSellPrice ?? item.highestBuyPrice ?? 1;
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
    errorMessage.value = '请先登录玩家账号后再购买。';
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

  await runAction(async () => {
    await createOrder({
      orderType: 'BUY',
      templateId: orderForm.templateId,
      itemId: null,
      targetPrice: orderForm.targetPrice
    });
    successMessage.value = '求购单已创建。';
    showOrderComposer.value = false;
    await refreshAll();
  });
}

async function buySelectedNow() {
  if (!isPlayer.value) {
    errorMessage.value = '请先登录玩家账号后再购买。';
    return;
  }
  if (!selectedListing.value || !selectedListing.value.lowestSellPrice) {
    errorMessage.value = '当前没有可立即购买的在售物品。';
    return;
  }

  await runAction(async () => {
    const order = await createOrder({
      orderType: 'BUY',
      templateId: selectedListing.value!.templateId,
      itemId: null,
      targetPrice: selectedListing.value!.lowestSellPrice!
    });
    try {
      const trade = await matchMarket(selectedListing.value!.templateId);
      successMessage.value = `已购买 ${trade.itemName}，成交价 ${money(trade.tradePrice)}。`;
    } catch (error) {
      try {
        await cancelOrder(order.marketOrderId);
      } catch {
        // If cancellation fails, keep the original matching error visible to the user.
      }
      throw error;
    }
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
        <span v-if="tab.key === 'orders'">({{ activeOrders.length }})</span>
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
        <h2 class="filter-title">筛选条件</h2>
        <div class="filter-kind-tabs">
          <button class="active" type="button">游戏内物品</button>
          <button type="button">Steam 物品</button>
        </div>

        <div class="filter-heading">
          <span class="cs2-badge">CS2</span>
          <div>
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
            :class="{ active: activeCategorySet.has(item.key) }"
            @click="toggleCategory(item.key)"
          >
            <span class="category-art">
              <img v-if="item.imageUrl" :src="versionedImageUrl(item.imageUrl)" :alt="item.label" />
              <span v-else>{{ item.label.slice(0, 1) }}</span>
            </span>
            <span class="category-copy">
              <strong>{{ item.label }}<template v-if="activeCategorySet.has(item.key)">:</template></strong>
              <small v-if="activeCategorySet.has(item.key)">
                全部
                <X :size="16" @click.stop="removeCategory(item.key)" />
              </small>
            </span>
          </button>
        </div>

        <div class="market-security">
          <ShieldAlert :size="20" />
          <p>所有交易均绑定当前登录玩家身份，并由平台完成钱包与库存校验。</p>
        </div>
      </aside>

      <div class="market-results">
        <div class="result-toolbar">
          <div class="result-summary">
            <p>找到 {{ filteredListings.length.toLocaleString('zh-CN') }} 个以下内容的搜索结果:</p>
            <div v-if="selectedCategories.length" class="result-chips">
              <button v-for="key in selectedCategories" :key="key" type="button" @click="removeCategory(key)">
                {{ categoryLabel(key) }}
                <X :size="16" />
              </button>
            </div>
          </div>
          <label>
            <span>排序方式:</span>
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
                <span>{{ rarityLabel(item.rarity) }} {{ categoryLabel(itemCategory(item)) }}</span>
                <h2>{{ item.itemName }}</h2>
              </div>
              <div class="item-image">
                <img v-if="item.imageUrl" :src="versionedImageUrl(item.imageUrl)" :alt="item.itemName" />
                <PackageSearch v-else :size="64" />
              </div>
              <div class="item-foot">
                <span>待售数量：{{ item.activeSellCount.toLocaleString('zh-CN') }}</span>
              </div>
            </button>
            <button class="price-button" type="button" @click="openOrder(item)">
              <span>起价：</span>
              <strong>{{ money(item.lowestSellPrice ?? item.highestBuyPrice) }}</strong>
            </button>
          </article>
        </div>
      </div>
    </section>

    <section v-else-if="activeTab === 'orders'" class="steam-section">
      <header>
        <div>
          <span>我的市场</span>
          <h2>我的上架物品与订购单</h2>
        </div>
        <button type="button" :disabled="!listings.length" @click="listings[0] && openOrder(listings[0])">
          <Tag :size="17" />
          查看市场物品
        </button>
      </header>
      <div v-if="!isPlayer" class="market-empty compact">
        <ShieldAlert :size="30" />
        <strong>请使用玩家账号登录</strong>
        <span>挂单只对已认证玩家开放。</span>
      </div>
      <div v-else-if="!activeOrders.length" class="market-empty compact">
        <PackageSearch :size="30" />
        <strong>您目前没有有效市场挂单</strong>
      </div>
      <div v-else class="steam-rows">
        <article v-for="order in activeOrders" :key="order.marketOrderId">
          <div class="row-art trade-art">
            <img
              v-if="templateImageUrl(order.templateId)"
              :src="versionedImageUrl(templateImageUrl(order.templateId))"
              :alt="order.itemName"
            />
            <Tag v-else :size="22" />
          </div>
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
      <p class="section-foot">等待撮合：{{ activeOrders.length }} 条</p>
    </section>

    <section v-else-if="activeTab === 'trades'" class="steam-section">
      <header>
        <div>
          <span>社区市场</span>
          <h2>我的成交记录</h2>
        </div>
      </header>
      <div v-if="!isPlayer" class="market-empty compact">
        <ShieldAlert :size="30" />
        <strong>请使用玩家账号登录</strong>
        <span>成交记录只显示当前登录玩家参与的交易。</span>
      </div>
      <div v-else-if="!myTrades.length" class="market-empty compact">
        <History :size="30" />
        <strong>暂无与您有关的成交记录</strong>
      </div>
      <div v-else class="steam-rows trade-rows">
        <article v-for="trade in myTrades" :key="trade.tradeId">
          <div class="row-art trade-art">
            <img
              v-if="tradeImageUrl(trade)"
              :src="versionedImageUrl(tradeImageUrl(trade))"
              :alt="trade.itemName"
            />
            <History v-else :size="22" />
          </div>
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

          <form class="order-form" @submit.prevent="canBuySelectedNow ? buySelectedNow() : submitOrder()">
            <div class="price-summary">
              <span>当前最低售价</span>
              <strong>{{ money(selectedListing.lowestSellPrice) }}</strong>
              <span>当前最高求购价</span>
              <strong>{{ money(selectedListing.highestBuyPrice) }}</strong>
            </div>
            <div v-if="canBuySelectedNow" class="steam-buy-summary">
              <span>将以当前最低售价立即购买一件在售物品。</span>
              <strong>{{ money(selectedListing.lowestSellPrice) }}</strong>
            </div>
            <label v-else>
              <span>暂无在售物品，设置您的求购价格</span>
              <input v-model.number="orderForm.targetPrice" type="number" min="0.01" step="0.01" />
            </label>
            <p v-if="!isPlayer" class="login-note">请先登录玩家账号后再提交交易。</p>
            <div class="composer-actions">
              <button class="submit-button" type="submit" :disabled="actionLoading || !isPlayer">
                <ShoppingCart :size="17" />
                {{ actionLoading ? '处理中...' : canBuySelectedNow ? '立即购买' : '创建求购单' }}
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
  align-content: start;
  gap: 14px;
  min-height: calc(100vh - 92px);
  color: #d6d7d8;
  background: #252b34;
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
  height: 34px;
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
  grid-template-columns: 330px minmax(0, 1fr);
  gap: 24px;
  align-items: start;
  padding: 32px 16px 0;
}

.filter-sidebar {
  display: grid;
  gap: 10px;
}

.filter-title {
  margin: 0 0 12px;
  color: #b5bdc7;
  font-size: 1rem;
  font-weight: 800;
}

.filter-kind-tabs {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0;
  padding: 4px;
  border-radius: 22px;
  background: #15191f;
}

.filter-kind-tabs button {
  min-height: 34px;
  border: 0;
  border-radius: 18px;
  color: #b8c0ca;
  background: transparent;
  cursor: pointer;
  font-weight: 800;
}

.filter-kind-tabs button.active {
  color: #f1f5f8;
  background: #4a5665;
}

.filter-heading {
  display: grid;
  grid-template-columns: 38px minmax(0, 1fr) auto;
  align-items: center;
  gap: 12px;
  min-height: 50px;
  padding: 0 12px;
  background: #3b4654;
}

.filter-heading div {
  display: grid;
  gap: 2px;
}

.filter-heading strong {
  color: #fff;
  font-size: 0.95rem;
}

.cs2-badge {
  display: grid;
  width: 28px;
  height: 28px;
  place-items: center;
  color: #fff;
  background: linear-gradient(135deg, #f7a31b, #4d8fd9);
  font-size: 0.66rem;
  font-weight: 900;
}

.sidebar-search {
  grid-template-columns: auto minmax(0, 1fr);
  min-height: 44px;
  padding: 0 12px;
  color: #8e9aa6;
  background: #3a4555;
}

.check-row {
  display: flex;
  grid-template-columns: none;
  align-items: center;
  gap: 12px;
  padding: 10px 18px;
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
  gap: 10px;
  margin-top: 4px;
}

.category-list button {
  display: grid;
  grid-template-columns: 92px minmax(0, 1fr);
  align-items: center;
  gap: 0;
  min-height: 74px;
  padding: 0;
  color: #d5d9dd;
  text-align: left;
  background:
    linear-gradient(90deg, rgba(78, 93, 110, 0.72), rgba(55, 64, 77, 0.96)),
    #303842;
}

.category-list button:hover,
.category-list button.active {
  color: #fff;
  background:
    linear-gradient(90deg, rgba(82, 102, 121, 0.88), rgba(58, 69, 84, 1)),
    #394553;
}

.category-list button.active {
  box-shadow: inset 0 0 0 1px #2b8ed8, inset 4px 0 #66c0f4;
}

.category-list button:focus {
  outline: none;
}

.category-list button:focus-visible {
  outline: 1px solid #66c0f4;
  outline-offset: -1px;
}

.category-copy {
  display: flex;
  min-width: 0;
  align-items: center;
  gap: 10px;
  padding: 0 18px 0 0;
}

.category-copy strong {
  overflow: hidden;
  font-size: 0.95rem;
  font-weight: 700;
  line-height: 1.2;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.category-copy small {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  margin-left: auto;
  color: #2da8ff;
  font-size: 0.84rem;
  font-style: normal;
  line-height: 1.2;
  white-space: nowrap;
}

.category-art {
  display: grid;
  height: 74px;
  place-items: center;
  overflow: hidden;
  background:
    radial-gradient(circle at 55% 48%, rgba(184, 95, 74, 0.32), transparent 58%),
    transparent;
}

.category-art img {
  width: 74px;
  height: 52px;
  object-fit: contain;
  filter: drop-shadow(0 9px 8px rgba(0, 0, 0, 0.38));
}

.market-security {
  display: none;
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
  align-items: flex-start;
  justify-content: space-between;
  gap: 20px;
  margin-bottom: 10px;
}

.result-toolbar p,
.result-toolbar span {
  margin: 0;
}

.result-summary {
  display: flex;
  min-height: 48px;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.result-toolbar p {
  color: #9aa4b1;
  font-size: 0.95rem;
}

.result-chips {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.result-chips button {
  appearance: none;
  display: inline-flex;
  min-height: 44px;
  align-items: center;
  gap: 8px;
  padding: 0 14px;
  border: 0;
  color: #dbe4ee;
  font-weight: 700;
  background: #3a4555;
  border-radius: 2px;
  box-shadow: none;
  text-shadow: none;
}

.result-chips button:hover {
  color: #fff;
  background: #495769;
}

.result-toolbar label {
  display: flex;
  grid-template-columns: none;
  align-items: center;
  gap: 12px;
  color: #a5afba;
  font-weight: 400;
}

.result-toolbar select {
  appearance: none;
  width: 118px;
  min-height: 48px;
  border: 0;
  border-radius: 2px;
  background: #3a4555;
  box-shadow: none;
  text-shadow: none;
}

.item-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 18px;
}

.market-item {
  --rarity-color: rgba(118, 146, 167, 0.58);
  --rarity-glow: rgba(118, 146, 167, 0.22);
  position: relative;
  display: grid;
  grid-template-rows: 1fr auto;
  min-width: 0;
  min-height: 360px;
  overflow: hidden;
  border: 1px solid var(--rarity-color);
  background: #212731;
}

.market-item::before {
  position: absolute;
  inset: 0 0 auto;
  z-index: 0;
  height: 30px;
  pointer-events: none;
  content: "";
  background: linear-gradient(180deg, var(--rarity-glow), transparent);
}

.market-item.rarity-uncommon {
  --rarity-color: rgba(94, 152, 217, 0.58);
  --rarity-glow: rgba(94, 152, 217, 0.26);
}

.market-item.rarity-rare {
  --rarity-color: rgba(75, 105, 255, 0.58);
  --rarity-glow: rgba(75, 105, 255, 0.28);
}

.market-item.rarity-epic {
  --rarity-color: rgba(136, 71, 255, 0.58);
  --rarity-glow: rgba(136, 71, 255, 0.3);
}

.market-item.rarity-legendary {
  --rarity-color: rgba(211, 44, 230, 0.58);
  --rarity-glow: rgba(211, 44, 230, 0.3);
}

.item-main {
  appearance: none;
  position: relative;
  z-index: 1;
  display: grid;
  grid-template-rows: auto minmax(184px, 1fr) auto;
  width: 100%;
  min-height: 292px;
  padding: 16px 14px 0;
  border: 0;
  color: inherit;
  cursor: pointer;
  text-align: left;
  background: transparent;
  box-shadow: none;
  text-shadow: none;
}

.item-meta {
  min-height: 54px;
}

.item-meta span {
  color: #a1aab5;
  font-size: 0.82rem;
}

.item-meta h2 {
  margin: 1px 0 0;
  overflow: hidden;
  color: #f2f2f2;
  font-size: 1.02rem;
  line-height: 1.28;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.item-image {
  display: grid;
  height: 184px;
  place-items: center;
  padding: 8px 6px 0;
  color: #556474;
}

.item-image img {
  width: 100%;
  height: 100%;
  object-fit: contain;
  filter: drop-shadow(0 20px 14px rgba(0, 0, 0, 0.45));
  transition: transform 0.18s ease;
}

.market-item:hover .item-image img {
  transform: scale(1.045);
}

.item-foot {
  display: block;
  min-height: 42px;
  padding-top: 8px;
  color: #9ca5af;
  font-size: 0.84rem;
}

.item-foot span {
  white-space: nowrap;
}

.price-button {
  appearance: none;
  position: relative;
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 7px;
  width: calc(100% - 28px);
  min-height: 44px;
  margin: 0 14px 14px;
  border: 0;
  color: #d7dce1;
  background: #3d4858;
  box-shadow: none;
  text-shadow: none;
}

.price-button span {
  color: #9aa4ad;
  font-size: 0.84rem;
}

.price-button:hover {
  color: #fff;
  background: #567b2a;
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

.trade-art img {
  width: 40px;
  height: 40px;
  object-fit: contain;
  filter: drop-shadow(0 7px 6px rgba(0, 0, 0, 0.35));
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

.steam-buy-summary {
  display: grid;
  gap: 7px;
  padding: 14px;
  color: #b9c8d6;
  background: linear-gradient(180deg, #243443, #1a2631);
}

.steam-buy-summary strong {
  color: #fff;
  font-size: 1.55rem;
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
  .item-grid {
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
