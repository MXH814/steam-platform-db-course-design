<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import {
  ArrowLeftRight,
  Boxes,
  ChevronLeft,
  ChevronRight,
  ExternalLink,
  LoaderCircle,
  PackagePlus,
  RefreshCw,
  Search,
  SlidersHorizontal,
  Tag
} from '@lucide/vue';
import {
  dropInventoryItem,
  listInventory,
  listItemTemplates,
  listItemTransfers,
  type InventoryGameId,
  type InventoryItem,
  type ItemTemplate,
  type ItemTransfer
} from '../api/inventoryApi';
import { useAuthStore } from '../stores/auth';

const games: Array<{ id: InventoryGameId; label: string; shortName: string; accent: string }> = [
  { id: 'GAME_CS2', label: 'Counter-Strike 2', shortName: 'CS2', accent: '#9ec9ff' },
  { id: 'GAME_DST', label: "Don't Starve Together / 饥荒联机版", shortName: 'DST', accent: '#d3a45f' }
];

const pageSize = 25;

const router = useRouter();
const auth = useAuthStore();
const activeGameId = ref<InventoryGameId>('GAME_CS2');
const searchTerm = ref('');
const templates = ref<ItemTemplate[]>([]);
const inventory = ref<InventoryItem[]>([]);
const selectedItemId = ref('');
const transfers = ref<ItemTransfer[]>([]);
const currentPage = ref(1);
const loading = ref(false);
const actionLoading = ref(false);
const transfersLoading = ref(false);
const errorMessage = ref('');
const successMessage = ref('');
const droppedItem = ref<InventoryItem | null>(null);
const missingImageKeys = ref<Set<string>>(new Set());

const activeGame = computed(() => games.find((game) => game.id === activeGameId.value) ?? games[0]);
const activeTemplates = computed(() => templates.value.filter((template) => template.gameId === activeGameId.value));
const activeItems = computed(() => inventory.value.filter((item) => item.gameId === activeGameId.value));

const filteredItems = computed(() => {
  const keyword = searchTerm.value.trim().toLowerCase();
  if (!keyword) {
    return activeItems.value;
  }

  return activeItems.value.filter((item) =>
    [item.itemName, item.itemId, item.templateId, item.rarity, item.status]
      .join(' ')
      .toLowerCase()
      .includes(keyword)
  );
});

const selectedItem = computed(() =>
  inventory.value.find((item) => item.itemId === selectedItemId.value) ?? filteredItems.value[0] ?? null
);

const totalPages = computed(() => Math.max(1, Math.ceil(filteredItems.value.length / pageSize)));
const pagedItems = computed(() => {
  const start = (currentPage.value - 1) * pageSize;
  return filteredItems.value.slice(start, start + pageSize);
});

const gameCounts = computed(() =>
  games.reduce<Record<InventoryGameId, number>>((counts, game) => {
    counts[game.id] = inventory.value.filter((item) => item.gameId === game.id).length;
    return counts;
  }, { GAME_CS2: 0, GAME_DST: 0 })
);

watch([activeGameId, searchTerm], () => {
  currentPage.value = 1;
  selectFirstVisibleItem();
});

watch(selectedItem, async (item) => {
  transfers.value = [];
  if (item) {
    await loadTransfers(item.itemId);
  }
});

onMounted(async () => {
  await refreshInventory();
});

async function refreshInventory() {
  loading.value = true;
  errorMessage.value = '';
  successMessage.value = '';

  try {
    const [templateRows, inventoryRows] = await Promise.all([
      listItemTemplates(),
      listInventory()
    ]);
    templates.value = templateRows;
    inventory.value = inventoryRows;
    selectFirstVisibleItem();
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '库存加载失败';
  } finally {
    loading.value = false;
  }
}

async function simulateDrop() {
  actionLoading.value = true;
  errorMessage.value = '';
  successMessage.value = '';
  droppedItem.value = null;

  try {
    const item = await dropInventoryItem(activeGameId.value);
    droppedItem.value = item;
    successMessage.value = `已获得 ${item.itemName}`;
    await refreshInventory();
    activeGameId.value = item.gameId as InventoryGameId;
    selectedItemId.value = item.itemId;
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '模拟掉落失败';
  } finally {
    actionLoading.value = false;
  }
}

async function loadTransfers(itemId: string) {
  transfersLoading.value = true;

  try {
    transfers.value = await listItemTransfers(itemId);
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '流转记录加载失败';
  } finally {
    transfersLoading.value = false;
  }
}

function selectFirstVisibleItem() {
  const visible = filteredItems.value;
  if (!visible.length) {
    selectedItemId.value = '';
    return;
  }

  if (!visible.some((item) => item.itemId === selectedItemId.value)) {
    selectedItemId.value = visible[0].itemId;
  }
}

function selectItem(item: InventoryItem) {
  selectedItemId.value = item.itemId;
}

function previousPage() {
  currentPage.value = Math.max(1, currentPage.value - 1);
  selectFirstVisibleItem();
}

function nextPage() {
  currentPage.value = Math.min(totalPages.value, currentPage.value + 1);
  selectFirstVisibleItem();
}

function openSellPage() {
  if (!selectedItem.value) {
    return;
  }

  router.push({
    path: '/market/orders',
    query: {
      itemId: selectedItem.value.itemId,
      templateId: selectedItem.value.templateId
    }
  });
}

function openMarket() {
  router.push({
    path: '/market',
    query: selectedItem.value ? { templateId: selectedItem.value.templateId } : undefined
  });
}

function formatTime(value: string) {
  return new Date(value).toLocaleString('zh-CN', { hour12: false });
}

function formatWear(value?: number | null) {
  return typeof value === 'number' ? value.toFixed(8) : '无';
}

function imageKey(item: Pick<InventoryItem, 'itemId' | 'templateId'>) {
  return item.itemId || item.templateId;
}

function shouldShowImage(item: Pick<InventoryItem, 'itemId' | 'templateId' | 'imageUrl'>) {
  return Boolean(item.imageUrl && !missingImageKeys.value.has(imageKey(item)));
}

function markImageMissing(item: Pick<InventoryItem, 'itemId' | 'templateId'>) {
  const next = new Set(missingImageKeys.value);
  next.add(imageKey(item));
  missingImageKeys.value = next;
}

function shortId(value: string) {
  return value.length > 16 ? `${value.slice(0, 10)}...` : value;
}

function rarityClass(rarity: string) {
  const normalized = rarity.toUpperCase();
  if (normalized.includes('COVERT') || normalized.includes('隐秘')) return 'covert';
  if (normalized.includes('CLASSIFIED') || normalized.includes('保密')) return 'classified';
  if (normalized.includes('RESTRICTED') || normalized.includes('受限')) return 'restricted';
  if (normalized.includes('MIL') || normalized.includes('军规')) return 'milspec';
  if (normalized.includes('INDUSTRIAL') || normalized.includes('工业')) return 'industrial';
  if (normalized.includes('SPECIAL') || normalized.includes('金')) return 'special';
  return 'common';
}

function transferParty(value?: string | null) {
  return value || 'SYSTEM';
}
</script>

<template>
  <main class="inventory-page">
    <section class="inventory-shell">
      <header class="inventory-actions">
        <div>
          <p class="inventory-eyebrow">Group D / Inventory</p>
          <h1>{{ auth.currentUser?.account ?? '玩家' }} 的库存</h1>
        </div>
        <div class="inventory-action-row">
          <button class="steam-blue" type="button" :disabled="loading" @click="refreshInventory">
            <RefreshCw :size="17" />
            刷新
          </button>
          <button class="steam-blue primary" type="button" :disabled="actionLoading || !auth.isAuthenticated" @click="simulateDrop">
            <LoaderCircle v-if="actionLoading" class="spin" :size="17" />
            <PackagePlus v-else :size="17" />
            模拟掉落
          </button>
        </div>
      </header>

      <nav class="game-tabs" aria-label="库存游戏分类">
        <button
          v-for="game in games"
          :key="game.id"
          type="button"
          :class="{ active: activeGameId === game.id }"
          :style="{ '--game-accent': game.accent }"
          @click="activeGameId = game.id"
        >
          <span class="game-icon">{{ game.shortName }}</span>
          <strong>{{ game.label }}</strong>
          <span>({{ gameCounts[game.id] }})</span>
        </button>
      </nav>

      <section class="inventory-board">
        <div class="game-banner">
          <div>
            <span>{{ activeGame.shortName }}</span>
            <h2>{{ activeGame.label }}</h2>
          </div>
          <p>{{ activeTemplates.length }} 个饰品模板，{{ activeItems.length }} 件库存饰品</p>
        </div>

        <div class="filter-row">
          <label class="search-box">
            <Search :size="17" />
            <input v-model="searchTerm" placeholder="在库存物品中搜索" />
          </label>
          <button class="filter-button" type="button">
            <SlidersHorizontal :size="16" />
            显示高级筛选条件...
          </button>
        </div>

        <p v-if="errorMessage" class="steam-message error">{{ errorMessage }}</p>
        <p v-if="successMessage" class="steam-message success">{{ successMessage }}</p>

        <section class="inventory-content">
          <div class="item-grid-panel">
            <div v-if="loading" class="inventory-state">
              <LoaderCircle class="spin" :size="22" />
              正在加载库存...
            </div>
            <div v-else-if="!filteredItems.length" class="inventory-state">
              <Boxes :size="24" />
              <strong>当前分类暂无饰品</strong>
              <span>可以点击“模拟掉落”生成一件该游戏饰品。</span>
            </div>
            <div v-else class="item-grid">
              <button
                v-for="item in pagedItems"
                :key="item.itemId"
                type="button"
                class="inventory-tile"
                :class="[rarityClass(item.rarity), { selected: selectedItem?.itemId === item.itemId }]"
                @click="selectItem(item)"
              >
                <img v-if="shouldShowImage(item)" :src="item.imageUrl ?? ''" :alt="item.itemName" @error="markImageMissing(item)" />
                <span v-else class="item-fallback">{{ item.itemName.slice(0, 2).toUpperCase() }}</span>
              </button>
            </div>

            <footer class="pager">
              <button type="button" :disabled="currentPage === 1" @click="previousPage">
                <ChevronLeft :size="17" />
              </button>
              <span>第 {{ currentPage }} 页，共 {{ totalPages }} 页</span>
              <button type="button" :disabled="currentPage === totalPages" @click="nextPage">
                <ChevronRight :size="17" />
              </button>
            </footer>
          </div>

          <aside class="item-detail" :class="{ empty: !selectedItem }">
            <template v-if="selectedItem">
              <div class="detail-art" :class="rarityClass(selectedItem.rarity)">
                <img
                  v-if="shouldShowImage(selectedItem)"
                  :src="selectedItem.imageUrl ?? ''"
                  :alt="selectedItem.itemName"
                  @error="markImageMissing(selectedItem)"
                />
                <span v-else>{{ selectedItem.itemName.slice(0, 2).toUpperCase() }}</span>
              </div>

              <div class="detail-heading">
                <h2>{{ selectedItem.itemName }}</h2>
                <p>{{ activeGame.label }}</p>
                <p>{{ selectedItem.rarity }} 饰品</p>
              </div>

              <div v-if="droppedItem?.itemId === selectedItem.itemId" class="drop-callout">
                这是刚刚模拟掉落获得的新饰品。
              </div>

              <dl class="detail-list">
                <div>
                  <dt>饰品实例</dt>
                  <dd>{{ selectedItem.itemId }}</dd>
                </div>
                <div>
                  <dt>图案模板</dt>
                  <dd>{{ selectedItem.templateId }}</dd>
                </div>
                <div>
                  <dt>磨损率</dt>
                  <dd>{{ formatWear(selectedItem.wearRating) }}</dd>
                </div>
                <div>
                  <dt>状态</dt>
                  <dd>{{ selectedItem.status }}</dd>
                </div>
                <div>
                  <dt>获得时间</dt>
                  <dd>{{ formatTime(selectedItem.acquireTime) }}</dd>
                </div>
              </dl>

              <p class="detail-description">
                这是一件由饰品模板生成的玩家库存实例。模板决定名称、游戏和稀有度；实例记录磨损、状态和归属，可进入市场挂单或查看流转账本。
              </p>

              <div class="detail-actions">
                <button class="market-button" type="button" @click="openMarket">
                  <ExternalLink :size="16" />
                  在社区市场中查看
                </button>
                <button class="sell-button" type="button" :disabled="selectedItem.status !== 'NORMAL'" @click="openSellPage">
                  <Tag :size="16" />
                  出售
                </button>
              </div>

              <section class="transfer-panel">
                <header>
                  <h3>饰品流转记录</h3>
                  <button type="button" :disabled="transfersLoading" @click="loadTransfers(selectedItem.itemId)">
                    <ArrowLeftRight :size="15" />
                    更新
                  </button>
                </header>

                <div v-if="transfersLoading" class="mini-state">正在加载...</div>
                <div v-else-if="!transfers.length" class="mini-state">暂无流转记录</div>
                <ol v-else class="transfer-list">
                  <li v-for="transfer in transfers" :key="transfer.transferId">
                    <span>{{ transfer.transferType }}</span>
                    <strong>{{ transferParty(transfer.fromUserId) }} -&gt; {{ transferParty(transfer.toUserId) }}</strong>
                    <time>{{ formatTime(transfer.transferTime) }}</time>
                    <small>{{ shortId(transfer.transferId) }}</small>
                  </li>
                </ol>
              </section>
            </template>

            <div v-else class="inventory-state">
              <Boxes :size="24" />
              <strong>请选择一件饰品</strong>
              <span>选中左侧库存格子后会显示实例详情。</span>
            </div>
          </aside>
        </section>
      </section>
    </section>
  </main>
</template>

<style scoped>
.inventory-page {
  width: min(100%, 1320px);
  margin: -12px auto 0;
  color: #c7d5e0;
}

.inventory-shell {
  display: grid;
  gap: 14px;
  min-width: 0;
}

.inventory-actions {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: 18px;
  padding: 12px 0 6px;
}

.inventory-eyebrow,
.inventory-actions h1,
.game-banner h2,
.game-banner p,
.detail-heading h2,
.detail-heading p,
.detail-description {
  margin: 0;
}

.inventory-eyebrow {
  color: #66c0f4;
  font-size: 0.75rem;
  font-weight: 800;
  letter-spacing: 0.1em;
  text-transform: uppercase;
}

.inventory-actions h1 {
  margin-top: 4px;
  color: #ffffff;
  font-size: 1.8rem;
  line-height: 1.1;
}

.inventory-action-row {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  justify-content: flex-end;
}

.steam-blue,
.filter-button,
.market-button,
.sell-button,
.transfer-panel button,
.pager button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 7px;
  border: 0;
  border-radius: 2px;
  min-height: 34px;
  padding: 0 14px;
  color: #dfe9f5;
  background: linear-gradient(#376b8d, #24516d);
  cursor: pointer;
  font-weight: 700;
}

.steam-blue:hover,
.market-button:hover,
.filter-button:hover {
  color: #ffffff;
  background: linear-gradient(#4b86ad, #2f6385);
}

.steam-blue.primary {
  color: #e7f7ff;
  background: linear-gradient(#1b91d1, #176b9d);
}

.game-tabs {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  background: #0f1923;
  border: 1px solid rgba(255, 255, 255, 0.05);
}

.game-tabs button {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
  border: 0;
  border-right: 1px solid #4b5562;
  padding: 14px 16px;
  color: #c7d5e0;
  background: #333a42;
  cursor: pointer;
}

.game-tabs button:last-child {
  border-right: 0;
}

.game-tabs button:hover,
.game-tabs button.active {
  background: #191f25;
}

.game-tabs button.active {
  box-shadow: inset 0 -3px 0 var(--game-accent);
}

.game-tabs strong {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.game-icon {
  display: grid;
  place-items: center;
  width: 34px;
  height: 34px;
  flex: 0 0 auto;
  color: #ffffff;
  background: linear-gradient(135deg, var(--game-accent), #1b2838);
  font-size: 0.78rem;
  font-weight: 900;
}

.inventory-board {
  padding: 10px 12px 12px;
  background: #151a1f;
  border: 1px solid rgba(255, 255, 255, 0.04);
  box-shadow: 0 18px 80px rgba(0, 0, 0, 0.38);
}

.game-banner {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: 20px;
  min-height: 132px;
  padding: 24px 24px 20px;
  background:
    linear-gradient(90deg, rgba(255, 255, 255, 0.04), transparent 58%),
    #171c21;
}

.game-banner span {
  color: #ffffff;
  font-size: 3.2rem;
  font-weight: 900;
  line-height: 0.9;
  letter-spacing: 0;
}

.game-banner h2 {
  margin-top: 10px;
  color: #f0f4f8;
  font-size: 1.35rem;
}

.game-banner p {
  color: #8997a5;
}

.filter-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: center;
  gap: 12px;
  padding: 12px 0;
}

.search-box {
  display: flex;
  align-items: center;
  gap: 9px;
  min-width: 0;
  padding: 0 12px;
  color: #687685;
  background: #0b1015;
  border: 1px solid #252f38;
}

.search-box input {
  border: 0;
  border-radius: 0;
  padding: 8px 0;
  color: #c7d5e0;
  background: transparent;
}

.filter-button {
  color: #e7edf4;
  background: #05080b;
}

.steam-message {
  margin: 0 0 10px;
  padding: 8px 10px;
  border-left: 3px solid currentColor;
  background: #101820;
}

.steam-message.error {
  color: #f7a8a8;
}

.steam-message.success {
  color: #a4d871;
}

.inventory-content {
  display: grid;
  grid-template-columns: minmax(360px, 1.42fr) minmax(360px, 1fr);
  gap: 12px;
  align-items: start;
}

.item-grid-panel,
.item-detail {
  min-height: 632px;
  background: #101417;
  border: 1px solid #232a31;
}

.item-grid {
  display: grid;
  grid-template-columns: repeat(5, minmax(78px, 1fr));
  gap: 6px;
  padding: 8px;
}

.inventory-tile {
  aspect-ratio: 1;
  border: 2px solid #707b86;
  padding: 0;
  background:
    radial-gradient(circle at 50% 40%, rgba(255, 255, 255, 0.1), transparent 58%),
    #343a40;
  cursor: pointer;
  overflow: hidden;
}

.inventory-tile:hover,
.inventory-tile.selected {
  border-color: #d9e9f9;
  box-shadow: 0 0 0 1px #d9e9f9;
}

.inventory-tile img,
.detail-art img {
  display: block;
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.item-fallback {
  display: grid;
  place-items: center;
  width: 100%;
  height: 100%;
  color: rgba(255, 255, 255, 0.72);
  font-size: 1.35rem;
  font-weight: 900;
}

.inventory-tile.common,
.detail-art.common {
  border-color: #8b929a;
}

.inventory-tile.industrial,
.detail-art.industrial {
  border-color: #5e98d9;
  background-color: #213242;
}

.inventory-tile.milspec,
.detail-art.milspec {
  border-color: #4b69ff;
  background-color: #1b2748;
}

.inventory-tile.restricted,
.detail-art.restricted {
  border-color: #8847ff;
  background-color: #2b1c43;
}

.inventory-tile.classified,
.detail-art.classified {
  border-color: #d32ce6;
  background-color: #3b173d;
}

.inventory-tile.covert,
.detail-art.covert {
  border-color: #eb4b4b;
  background-color: #401d22;
}

.inventory-tile.special,
.detail-art.special {
  border-color: #ffd700;
  background-color: #403717;
}

.pager {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 10px;
  padding: 10px 0 0;
  color: #8f98a0;
}

.pager button {
  min-height: 26px;
  padding: 0 10px;
  background: #303842;
}

.item-detail {
  padding: 10px;
  border-color: #9db4c7;
  background:
    linear-gradient(180deg, rgba(255, 255, 255, 0.05), transparent 42%),
    #171a1d;
}

.item-detail.empty {
  display: grid;
  place-items: center;
}

.detail-art {
  display: grid;
  place-items: center;
  height: 260px;
  border: 0;
  background:
    radial-gradient(circle at 50% 42%, rgba(255, 255, 255, 0.12), transparent 58%),
    #202326;
}

.detail-art span {
  color: rgba(255, 255, 255, 0.62);
  font-size: 4rem;
  font-weight: 900;
}

.detail-heading {
  margin-top: 12px;
  padding-top: 14px;
  border-top: 1px solid #4a525a;
}

.detail-heading h2 {
  color: #dbe8f6;
  font-size: 1.7rem;
  line-height: 1.1;
}

.detail-heading p {
  color: #9aa7b3;
}

.drop-callout {
  margin-top: 12px;
  padding: 8px 10px;
  color: #dff6a6;
  background: rgba(123, 164, 41, 0.22);
}

.detail-list {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px 18px;
  margin: 16px 0 0;
}

.detail-list div {
  min-width: 0;
}

.detail-list dt {
  color: #6f7e8d;
  font-size: 0.75rem;
  font-weight: 800;
}

.detail-list dd {
  margin: 0;
  color: #c7d5e0;
  overflow-wrap: anywhere;
}

.detail-description {
  margin-top: 14px;
  color: #b8c5d1;
  line-height: 1.55;
}

.detail-actions {
  display: grid;
  gap: 9px;
  margin-top: 18px;
}

.market-button {
  width: 100%;
  background: #495463;
}

.sell-button {
  width: fit-content;
  color: #203000;
  background: #8bc53f;
}

.sell-button:hover {
  background: #a2d958;
}

.transfer-panel {
  margin-top: 18px;
  padding-top: 14px;
  border-top: 1px solid #2d363e;
}

.transfer-panel header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.transfer-panel h3 {
  margin: 0;
  color: #dbe8f6;
  font-size: 1rem;
}

.transfer-panel button {
  min-height: 28px;
  padding: 0 10px;
  color: #c7d5e0;
  background: #2d3742;
}

.transfer-list {
  display: grid;
  gap: 8px;
  margin: 12px 0 0;
  padding: 0;
  list-style: none;
}

.transfer-list li {
  display: grid;
  gap: 2px;
  padding: 8px 10px;
  background: #101820;
}

.transfer-list span {
  width: fit-content;
  padding: 1px 7px;
  color: #9ec9ff;
  background: rgba(102, 192, 244, 0.13);
  font-size: 0.78rem;
  font-weight: 800;
}

.transfer-list strong,
.transfer-list time,
.transfer-list small {
  color: #c7d5e0;
}

.transfer-list time,
.transfer-list small,
.mini-state {
  color: #7f8d9c;
  font-size: 0.86rem;
}

.inventory-state {
  display: grid;
  place-items: center;
  align-content: center;
  gap: 8px;
  min-height: 420px;
  padding: 28px;
  color: #8f98a0;
  text-align: center;
}

.inventory-state strong {
  color: #c7d5e0;
}

.mini-state {
  padding: 12px 0;
}

.spin {
  animation: spin 0.9s linear infinite;
}

button:disabled {
  cursor: not-allowed;
  opacity: 0.56;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

@media (max-width: 1120px) {
  .inventory-page {
    width: 100%;
  }

  .inventory-content {
    grid-template-columns: 1fr;
  }

  .item-detail {
    min-height: 0;
  }
}

@media (max-width: 720px) {
  .inventory-actions,
  .game-banner {
    align-items: stretch;
    flex-direction: column;
  }

  .game-tabs,
  .filter-row {
    grid-template-columns: 1fr;
  }

  .item-grid {
    grid-template-columns: repeat(3, minmax(74px, 1fr));
  }

  .detail-list {
    grid-template-columns: 1fr;
  }
}
</style>
