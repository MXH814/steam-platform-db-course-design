<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  ArrowRightLeft,
  BadgeDollarSign,
  Boxes,
  CircleX,
  History,
  LoaderCircle,
  RefreshCw,
  ScrollText,
  ShoppingCart,
  Tag
} from '@lucide/vue'
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
} from '../api/marketApi'
import { useAuthStore } from '../stores/auth'

type TabKey = 'market' | 'orders' | 'trades' | 'transfers'

const tabs: Array<{ key: TabKey; label: string }> = [
  { key: 'market', label: '市场' },
  { key: 'orders', label: '挂单' },
  { key: 'trades', label: '成交' },
  { key: 'transfers', label: '流转' }
]

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const activeTab = computed(() => (route.meta.tab as TabKey) || 'market')
const isPlayer = computed(() => auth.currentUser?.role.toUpperCase() === 'PLAYER')
const currentPlayerLabel = computed(() => {
  if (!auth.currentUser) {
    return '未登录'
  }

  return `${auth.currentUser.account} / ${auth.currentUser.principalId}`
})
const loading = ref(false)
const actionLoading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const listings = ref<MarketListing[]>([])
const orders = ref<MarketOrder[]>([])
const trades = ref<MarketTrade[]>([])
const transfers = ref<ItemTransfer[]>([])

const orderForm = reactive<CreateMarketOrderPayload>({
  orderType: 'BUY',
  templateId: 'ITPL_CS2_AK_REDLINE',
  itemId: 'ITEM_CS2_002',
  targetPrice: 50
})

const transferItemId = ref('ITEM_CS2_001')
const matchTemplateId = ref('ITPL_CS2_AK_REDLINE')

const matchingOrders = computed(() => orders.value.filter((order) => order.status === 'MATCHING'))

function money(value?: number | null) {
  return typeof value === 'number' ? `¥${value.toFixed(2)}` : '-'
}

function shortId(value: string) {
  return value.length > 12 ? `${value.slice(0, 8)}...` : value
}

function formatTime(value: string) {
  return new Date(value).toLocaleString('zh-CN', { hour12: false })
}

async function refreshAll() {
  loading.value = true
  errorMessage.value = ''
  try {
    const [marketRows, tradeRows] = await Promise.all([
      getMarket('GAME_CS2'),
      getTrades()
    ])
    listings.value = marketRows
    trades.value = tradeRows
    orders.value = isPlayer.value ? await getMyOrders() : []
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '刷新失败'
  } finally {
    loading.value = false
  }
}

function openTab(tab: TabKey) {
  const paths: Record<TabKey, string> = {
    market: '/market',
    orders: '/market/orders',
    trades: '/market/trades',
    transfers: '/market/transfers'
  }
  router.push(paths[tab])
}

async function submitOrder() {
  if (!isPlayer.value) {
    errorMessage.value = '请先登录玩家账号。'
    return
  }

  if (!orderForm.templateId.trim()) {
    errorMessage.value = '饰品模板不能为空。'
    return
  }
  if (orderForm.targetPrice <= 0) {
    errorMessage.value = '价格必须大于 0。'
    return
  }
  if (orderForm.orderType === 'SELL' && !orderForm.itemId?.trim()) {
    errorMessage.value = '卖单必须填写饰品编号。'
    return
  }

  await runAction(async () => {
    const payload = {
      ...orderForm,
      itemId: orderForm.orderType === 'SELL' ? orderForm.itemId?.trim() : null
    }
    await createOrder(payload)
    successMessage.value = '挂单已创建。'
    await refreshAll()
  })
}

async function cancelSelectedOrder(order: MarketOrder) {
  if (!isPlayer.value) {
    errorMessage.value = '请先登录玩家账号。'
    return
  }

  await runAction(async () => {
    await cancelOrder(order.marketOrderId)
    successMessage.value = '挂单已取消。'
    await refreshAll()
  })
}

async function submitMatch() {
  if (!isPlayer.value) {
    errorMessage.value = '请先登录玩家账号。'
    return
  }

  await runAction(async () => {
    const trade = await matchMarket(matchTemplateId.value.trim())
    successMessage.value = `成交 ${shortId(trade.tradeId)}，饰品 ${trade.itemId} 已换手。`
    await refreshAll()
    if (transferItemId.value === trade.itemId) {
      await loadTransfers()
    }
  })
}

async function loadTransfers() {
  if (!transferItemId.value.trim()) {
    errorMessage.value = '饰品编号不能为空。'
    return
  }

  await runAction(async () => {
    transfers.value = await getTransfers(transferItemId.value.trim())
    successMessage.value = '流转记录已更新。'
  })
}

async function runAction(action: () => Promise<void>) {
  actionLoading.value = true
  errorMessage.value = ''
  successMessage.value = ''
  try {
    await action()
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '操作失败'
  } finally {
    actionLoading.value = false
  }
}

onMounted(async () => {
  await refreshAll()
  await loadTransfers()
})
</script>

<template>
  <main class="shell">
    <section class="workspace">
      <header class="topbar">
        <div>
          <p class="eyebrow">Group D / Market</p>
          <h1>饰品市场交易</h1>
        </div>
        <div class="toolbar">
          <div class="segmented" aria-label="当前玩家">
            <button class="active" type="button" disabled>{{ currentPlayerLabel }}</button>
          </div>
          <button class="icon-button" title="刷新" :disabled="loading" @click="refreshAll">
            <LoaderCircle v-if="loading" class="spin" :size="18" />
            <RefreshCw v-else :size="18" />
          </button>
        </div>
      </header>

      <nav class="tabs">
        <button v-for="tab in tabs" :key="tab.key" :class="{ active: activeTab === tab.key }" @click="openTab(tab.key)">
          {{ tab.label }}
        </button>
      </nav>

      <p v-if="errorMessage" class="notice error">{{ errorMessage }}</p>
      <p v-if="successMessage" class="notice success">{{ successMessage }}</p>

      <section v-if="activeTab === 'market'" class="content-grid">
        <div class="panel">
          <div class="panel-title">
            <Boxes :size="18" />
            <h2>市场行情</h2>
          </div>
          <div v-if="loading" class="state">加载中...</div>
          <div v-else-if="!listings.length" class="state">暂无饰品模板</div>
          <div v-else class="market-grid">
            <article v-for="item in listings" :key="item.templateId" class="item-card">
              <div class="item-art">
                <span>{{ item.rarity }}</span>
              </div>
              <div class="item-body">
                <h3>{{ item.itemName }}</h3>
                <p>{{ item.templateId }}</p>
                <div class="price-row">
                  <span>买价 {{ money(item.highestBuyPrice) }}</span>
                  <span>卖价 {{ money(item.lowestSellPrice) }}</span>
                </div>
                <div class="depth-row">
                  <span>买单 {{ item.activeBuyCount }}</span>
                  <span>卖单 {{ item.activeSellCount }}</span>
                </div>
              </div>
            </article>
          </div>
        </div>

        <aside class="panel side-panel">
          <div class="panel-title">
            <BadgeDollarSign :size="18" />
            <h2>创建挂单</h2>
          </div>
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
          <button class="primary" :disabled="actionLoading || !isPlayer" @click="submitOrder">
            <ShoppingCart v-if="orderForm.orderType === 'BUY'" :size="18" />
            <Tag v-else :size="18" />
            提交
          </button>

          <div class="divider"></div>

          <div class="panel-title compact">
            <ArrowRightLeft :size="18" />
            <h2>撮合</h2>
          </div>
          <label>
            模板
            <input v-model="matchTemplateId" />
          </label>
          <button class="primary green" :disabled="actionLoading || !isPlayer" @click="submitMatch">
            <ArrowRightLeft :size="18" />
            撮合
          </button>
        </aside>
      </section>

      <section v-if="activeTab === 'orders'" class="panel">
        <div class="panel-title">
          <ScrollText :size="18" />
          <h2>我的挂单</h2>
        </div>
        <div v-if="!orders.length" class="state">暂无挂单</div>
        <div v-else class="table">
          <div class="table-row table-head">
            <span>编号</span>
            <span>类型</span>
            <span>饰品</span>
            <span>价格</span>
            <span>状态</span>
            <span>操作</span>
          </div>
          <div v-for="order in orders" :key="order.marketOrderId" class="table-row">
            <span>{{ shortId(order.marketOrderId) }}</span>
            <span>{{ order.orderType }}</span>
            <span>{{ order.itemName }}</span>
            <span>{{ money(order.targetPrice) }}</span>
            <span><i :class="['status-dot', order.status.toLowerCase()]"></i>{{ order.status }}</span>
            <button class="icon-button danger" title="取消" :disabled="order.status !== 'MATCHING' || actionLoading || !isPlayer" @click="cancelSelectedOrder(order)">
              <CircleX :size="17" />
            </button>
          </div>
        </div>
        <p class="micro">撮合中 {{ matchingOrders.length }} 条</p>
      </section>

      <section v-if="activeTab === 'trades'" class="panel">
        <div class="panel-title">
          <History :size="18" />
          <h2>成交记录</h2>
        </div>
        <div v-if="!trades.length" class="state">暂无成交</div>
        <div v-else class="trade-list">
          <article v-for="trade in trades" :key="trade.tradeId" class="trade-item">
            <div>
              <strong>{{ trade.itemName }}</strong>
              <p>{{ shortId(trade.tradeId) }} / {{ trade.itemId }}</p>
            </div>
            <div>
              <span>{{ trade.sellerId }}</span>
              <ArrowRightLeft :size="16" />
              <span>{{ trade.buyerId }}</span>
            </div>
            <div class="amount">
              <strong>{{ money(trade.tradePrice) }}</strong>
              <p>fee {{ money(trade.platformFee) }}</p>
            </div>
            <time>{{ formatTime(trade.tradeTime) }}</time>
          </article>
        </div>
      </section>

      <section v-if="activeTab === 'transfers'" class="panel">
        <div class="panel-title">
          <ArrowRightLeft :size="18" />
          <h2>饰品流转</h2>
        </div>
        <div class="lookup">
          <input v-model="transferItemId" />
          <button class="primary" :disabled="actionLoading" @click="loadTransfers">
            <RefreshCw :size="18" />
            查询
          </button>
        </div>
        <div v-if="!transfers.length" class="state">暂无流转记录</div>
        <div v-else class="timeline">
          <article v-for="transfer in transfers" :key="transfer.transferId">
            <span>{{ transfer.transferType }}</span>
            <strong>{{ transfer.fromUserId ?? 'SYSTEM' }} -&gt; {{ transfer.toUserId }}</strong>
            <p>{{ transfer.itemName }} / {{ formatTime(transfer.transferTime) }}</p>
          </article>
        </div>
      </section>
    </section>
  </main>
</template>
