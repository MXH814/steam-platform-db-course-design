<script setup lang="ts">
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

const tabs: Array<{ key: TabKey; label: string }> = [
  { key: 'market', label: '市场' },
  { key: 'orders', label: '挂单' },
  { key: 'trades', label: '成交' },
  { key: 'transfers', label: '流转' }
];

const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const activeTab = computed(() => (route.meta.tab as TabKey) || 'market');
const isPlayer = computed(() => auth.currentUser?.role.toUpperCase() === 'PLAYER');
const currentPlayerLabel = computed(() => (auth.currentUser ? `${auth.currentUser.account} / ${auth.currentUser.principalId}` : '未登录'));
const loading = ref(false);
const actionLoading = ref(false);
const errorMessage = ref('');
const successMessage = ref('');

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

function money(value?: number | null) {
  return typeof value === 'number' ? `¥${value.toFixed(2)}` : '-';
}

function shortId(value: string) {
  return value.length > 12 ? `${value.slice(0, 8)}...` : value;
}

function formatTime(value: string) {
  return new Date(value).toLocaleString('zh-CN', { hour12: false });
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
    errorMessage.value = error instanceof Error ? error.message : '刷新失败';
  } finally {
    loading.value = false;
  }
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

async function submitOrder() {
  if (!isPlayer.value) {
    errorMessage.value = '请先登录玩家账号。';
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
    errorMessage.value = '卖单必须填写饰品编号。';
    return;
  }

  await runAction(async () => {
    const payload = {
      ...orderForm,
      itemId: orderForm.orderType === 'SELL' ? orderForm.itemId?.trim() : null
    };
    await createOrder(payload);
    successMessage.value = '挂单已创建。';
    await refreshAll();
  });
}

async function cancelSelectedOrder(order: MarketOrder) {
  if (!isPlayer.value) {
    errorMessage.value = '请先登录玩家账号。';
    return;
  }

  await runAction(async () => {
    await cancelOrder(order.marketOrderId);
    successMessage.value = '挂单已取消。';
    await refreshAll();
  });
}

async function submitMatch() {
  if (!isPlayer.value) {
    errorMessage.value = '请先登录玩家账号。';
    return;
  }

  await runAction(async () => {
    const trade = await matchMarket(matchTemplateId.value.trim());
    successMessage.value = `成交 ${shortId(trade.tradeId)}，饰品 ${trade.itemId} 已换手。`;
    await refreshAll();
    if (transferItemId.value === trade.itemId) {
      await loadTransfers();
    }
  });
}

async function loadTransfers() {
  if (!transferItemId.value.trim()) {
    errorMessage.value = '饰品编号不能为空。';
    return;
  }

  await runAction(async () => {
    transfers.value = await getTransfers(transferItemId.value.trim());
    successMessage.value = '流转记录已更新。';
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
  await loadTransfers();
});
</script>

<template>
  <main class="market-shell">
    <section class="market-workspace">
      <header class="market-top">
        <div>
          <p class="eyebrow">Group D / Market</p>
          <h1>饰品市场交易</h1>
        </div>
        <div class="market-toolbar">
          <span>{{ currentPlayerLabel }}</span>
          <button class="ghost-button" type="button" :disabled="loading" @click="refreshAll">{{ loading ? '刷新中' : '刷新' }}</button>
        </div>
      </header>

      <nav class="market-tabs">
        <button v-for="tab in tabs" :key="tab.key" type="button" :class="{ active: activeTab === tab.key }" @click="openTab(tab.key)">
          {{ tab.label }}
        </button>
      </nav>

      <p v-if="errorMessage" class="message error">{{ errorMessage }}</p>
      <p v-if="successMessage" class="message success">{{ successMessage }}</p>

      <section v-if="activeTab === 'market'" class="market-grid">
        <div class="market-panel">
          <h2>市场行情</h2>
          <div v-if="loading" class="market-state">加载中...</div>
          <div v-else-if="!listings.length" class="market-state">暂无饰品模板</div>
          <div v-else class="listing-grid">
            <article v-for="item in listings" :key="item.templateId" class="item-card">
              <div class="item-art">{{ item.rarity }}</div>
              <div>
                <h3>{{ item.itemName }}</h3>
                <p>{{ item.templateId }}</p>
                <span>买价 {{ money(item.highestBuyPrice) }}</span>
                <span>卖价 {{ money(item.lowestSellPrice) }}</span>
              </div>
            </article>
          </div>
        </div>

        <aside class="market-panel side-panel">
          <h2>创建挂单</h2>
          <label>
            类型
            <select v-model="orderForm.orderType">
              <option value="BUY">买单</option>
              <option value="SELL">卖单</option>
            </select>
          </label>
          <label>
            模板
            <input v-model="orderForm.templateId" />
          </label>
          <label v-if="orderForm.orderType === 'SELL'">
            饰品
            <input v-model="orderForm.itemId" />
          </label>
          <label>
            价格
            <input v-model.number="orderForm.targetPrice" type="number" min="0.01" step="0.01" />
          </label>
          <button class="primary-button" type="button" :disabled="actionLoading || !isPlayer" @click="submitOrder">提交</button>

          <div class="divider"></div>

          <h2>撮合</h2>
          <label>
            模板
            <input v-model="matchTemplateId" />
          </label>
          <button class="secondary-button" type="button" :disabled="actionLoading || !isPlayer" @click="submitMatch">撮合</button>
        </aside>
      </section>

      <section v-if="activeTab === 'orders'" class="market-panel">
        <h2>我的挂单</h2>
        <div v-if="!orders.length" class="market-state">暂无挂单</div>
        <div v-else class="market-table">
          <div class="table-row table-head">
            <span>编号</span><span>类型</span><span>饰品</span><span>价格</span><span>状态</span><span>操作</span>
          </div>
          <div v-for="order in orders" :key="order.marketOrderId" class="table-row">
            <span>{{ shortId(order.marketOrderId) }}</span>
            <span>{{ order.orderType }}</span>
            <span>{{ order.itemName }}</span>
            <span>{{ money(order.targetPrice) }}</span>
            <span>{{ order.status }}</span>
            <button class="ghost-button" type="button" :disabled="order.status !== 'MATCHING' || actionLoading || !isPlayer" @click="cancelSelectedOrder(order)">取消</button>
          </div>
        </div>
        <p class="micro">撮合中 {{ matchingOrders.length }} 条</p>
      </section>

      <section v-if="activeTab === 'trades'" class="market-panel">
        <h2>成交记录</h2>
        <div v-if="!trades.length" class="market-state">暂无成交</div>
        <div v-else class="trade-list">
          <article v-for="trade in trades" :key="trade.tradeId">
            <strong>{{ trade.itemName }}</strong>
            <span>{{ shortId(trade.tradeId) }} / {{ trade.itemId }}</span>
            <span>{{ trade.sellerId }} → {{ trade.buyerId }}</span>
            <strong>{{ money(trade.tradePrice) }}</strong>
            <time>{{ formatTime(trade.tradeTime) }}</time>
          </article>
        </div>
      </section>

      <section v-if="activeTab === 'transfers'" class="market-panel">
        <h2>饰品流转</h2>
        <div class="lookup">
          <input v-model="transferItemId" />
          <button class="primary-button" type="button" :disabled="actionLoading" @click="loadTransfers">查询</button>
        </div>
        <div v-if="!transfers.length" class="market-state">暂无流转记录</div>
        <div v-else class="timeline">
          <article v-for="transfer in transfers" :key="transfer.transferId">
            <span>{{ transfer.transferType }}</span>
            <strong>{{ transfer.fromUserId ?? 'SYSTEM' }} → {{ transfer.toUserId }}</strong>
            <p>{{ transfer.itemName }} / {{ formatTime(transfer.transferTime) }}</p>
          </article>
        </div>
      </section>
    </section>
  </main>
</template>

<style scoped>
.market-shell,
.market-workspace,
.market-panel,
.side-panel {
  display: grid;
  gap: 1rem;
}

.market-top,
.market-toolbar,
.market-tabs,
.lookup {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.market-top {
  justify-content: space-between;
}

.market-top h1,
.market-top p,
.market-panel h2,
.item-card h3,
.item-card p,
.micro {
  margin: 0;
}

.market-toolbar {
  justify-content: flex-end;
  flex-wrap: wrap;
}

.market-toolbar span {
  color: var(--steam-muted);
}

.market-tabs button {
  border: 1px solid var(--steam-border);
  border-radius: 4px;
  padding: 0.48rem 0.8rem;
  color: var(--steam-text);
  background: rgba(22, 32, 45, 0.9);
  cursor: pointer;
}

.market-tabs button.active {
  color: #07111c;
  background: var(--steam-blue);
}

.market-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 320px;
  gap: 1rem;
}

.market-panel {
  border: 1px solid var(--steam-border);
  border-radius: 6px;
  padding: 1rem;
  background: rgba(22, 32, 45, 0.86);
}

.listing-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.75rem;
}

.item-card {
  display: grid;
  grid-template-columns: 96px minmax(0, 1fr);
  gap: 0.75rem;
  border-radius: 4px;
  padding: 0.75rem;
  background: rgba(8, 13, 19, 0.36);
}

.item-art {
  display: grid;
  min-height: 76px;
  place-items: center;
  border-radius: 4px;
  color: var(--steam-blue);
  background: linear-gradient(135deg, #17202c, #26435a);
  font-size: 0.78rem;
  font-weight: 900;
}

.item-card p,
.item-card span,
.market-state,
.micro {
  color: var(--steam-muted);
}

.item-card span {
  display: inline-block;
  margin-right: 0.55rem;
  font-variant-numeric: tabular-nums;
}

.divider {
  height: 1px;
  background: var(--steam-border);
}

.market-table,
.trade-list,
.timeline {
  display: grid;
  gap: 0.55rem;
}

.table-row,
.trade-list article,
.timeline article {
  display: grid;
  grid-template-columns: repeat(6, minmax(0, 1fr));
  gap: 0.5rem;
  align-items: center;
  border-radius: 4px;
  padding: 0.6rem;
  background: rgba(8, 13, 19, 0.32);
}

.table-head {
  color: var(--steam-muted);
  font-weight: 800;
}

.trade-list article {
  grid-template-columns: 1.2fr 1fr 1.4fr 0.8fr 1fr;
}

.timeline article {
  grid-template-columns: 0.8fr 1.4fr 1.6fr;
}

@media (max-width: 920px) {
  .market-top,
  .market-grid,
  .listing-grid,
  .table-row,
  .trade-list article,
  .timeline article {
    grid-template-columns: 1fr;
  }

  .market-top {
    align-items: start;
    flex-direction: column;
  }

  .market-tabs,
  .lookup {
    flex-wrap: wrap;
  }
}
</style>
