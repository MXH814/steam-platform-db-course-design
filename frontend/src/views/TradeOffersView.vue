<template>
  <section class="trade-page">
    <header class="trade-header">
      <div><p>社区交换</p><h1>交易报价</h1><span>物品交换由 Oracle 事务锁定，并写入资产转移账本。</span></div>
      <button type="button" @click="composerOpen = !composerOpen"><Plus v-if="!composerOpen" :size="17" /><X v-else :size="17" />{{ composerOpen ? '关闭创建器' : '新建交易报价' }}</button>
    </header>

    <section v-if="composerOpen" class="trade-composer">
      <header><strong>创建新报价</strong><span>双方至少选择 1 件、最多选择 8 件物品</span></header>
      <label class="friend-select"><Users :size="17" /><select v-model="recipientId" @change="loadRecipientInventory"><option value="">选择交易好友</option><option v-for="friend in acceptedFriends" :key="friend.userId" :value="friend.userId">{{ friend.nickname }}</option></select></label>
      <div class="trade-sides">
        <section>
          <header><div><span class="side-avatar mine">{{ ownInitial }}</span><strong>你的物品</strong></div><b>{{ offeredIds.size }}/8</b></header>
          <div class="item-picker">
            <button v-for="item in ownTradeable" :key="item.itemId" type="button" :class="['trade-item', rarityClass(item.rarity), { selected: offeredIds.has(item.itemId) }]" @click="toggleItem(offeredIds, item.itemId)">
              <img :src="item.imageUrl || fallbackItemImage" :alt="item.itemName" /><span><strong>{{ item.itemName }}</strong><small>{{ item.gameId === 'GAME_CS2' ? 'Counter-Strike 2' : '饥荒联机版' }}</small></span><Check v-if="offeredIds.has(item.itemId)" :size="17" />
            </button>
            <p v-if="!ownTradeable.length">你目前没有可交易物品。</p>
          </div>
        </section>
        <ArrowLeftRight class="trade-arrow" :size="28" />
        <section>
          <header><div><span class="side-avatar friend">{{ recipientName.slice(0, 1) || '?' }}</span><strong>{{ recipientName || '好友的物品' }}</strong></div><b>{{ requestedIds.size }}/8</b></header>
          <div class="item-picker">
            <button v-for="item in recipientItems" :key="item.itemId" type="button" :class="['trade-item', rarityClass(item.rarity), { selected: requestedIds.has(item.itemId) }]" @click="toggleItem(requestedIds, item.itemId)">
              <img :src="item.imageUrl || fallbackItemImage" :alt="item.itemName" /><span><strong>{{ item.itemName }}</strong><small>{{ item.gameId === 'GAME_CS2' ? 'Counter-Strike 2' : '饥荒联机版' }}</small></span><Check v-if="requestedIds.has(item.itemId)" :size="17" />
            </button>
            <p v-if="recipientLoading">正在读取好友可交易库存...</p><p v-else-if="recipientId && !recipientItems.length">该好友目前没有可交易物品。</p><p v-else-if="!recipientId">先选择一位已接受的好友。</p>
          </div>
        </section>
      </div>
      <label class="trade-message">附加留言<textarea v-model.trim="message" maxlength="500" rows="3" placeholder="说明这次交换的目的（可选）" /></label>
      <footer><span :class="{ error: composerMessage && !composerSuccess }">{{ composerMessage }}</span><button type="button" :disabled="submitting || !canSubmit" @click="submitOffer"><Send :size="17" />{{ submitting ? '发送中' : '发送报价' }}</button></footer>
    </section>

    <nav class="trade-tabs" aria-label="交易报价筛选">
      <button v-for="tab in tabs" :key="tab.value" type="button" :class="{ active: activeStatus === tab.value }" @click="activeStatus = tab.value; loadOffers()">{{ tab.label }}<span>{{ countFor(tab.value) }}</span></button>
    </nav>

    <div v-if="loading" class="trade-state">正在同步交易报价...</div>
    <div v-else-if="loadError" class="trade-state error">{{ loadError }}<button type="button" @click="loadOffers">重试</button></div>
    <div v-else-if="!offers.length" class="trade-state"><PackageOpen :size="34" /><strong>这里还没有交易报价</strong><span>向好友发送一个物品交换报价后，它会出现在这里。</span></div>
    <div v-else class="offer-list">
      <article v-for="offer in offers" :key="offer.offerId" class="offer-card">
        <header>
          <div><span :class="['offer-status', offer.status.toLowerCase()]">{{ statusLabel(offer.status) }}</span><strong>{{ offer.senderNickname }} <ArrowRight :size="14" /> {{ offer.recipientNickname }}</strong></div>
          <time>{{ formatDate(offer.createdAt) }}</time>
        </header>
        <p v-if="offer.message" class="offer-message">“{{ offer.message }}”</p>
        <div class="offer-sides">
          <section><small>{{ offer.senderNickname }} 提供</small><div><span v-for="item in offer.offeredItems" :key="item.itemId" :title="item.itemName"><img :src="item.imageUrl || fallbackItemImage" :alt="item.itemName" /><b>{{ item.itemName }}</b></span></div></section>
          <ArrowLeftRight :size="23" />
          <section><small>{{ offer.recipientNickname }} 提供</small><div><span v-for="item in offer.requestedItems" :key="item.itemId" :title="item.itemName"><img :src="item.imageUrl || fallbackItemImage" :alt="item.itemName" /><b>{{ item.itemName }}</b></span></div></section>
        </div>
        <footer>
          <span>报价号 {{ offer.offerId }} · 版本 {{ offer.version }}</span>
          <div><button v-if="offer.canDecline" type="button" class="secondary" :disabled="actingId === offer.offerId" @click="act(offer, 'DECLINE')">拒绝</button><button v-if="offer.canCancel" type="button" class="secondary" :disabled="actingId === offer.offerId" @click="act(offer, 'CANCEL')">撤销</button><button v-if="offer.canAccept" type="button" :disabled="actingId === offer.offerId" @click="act(offer, 'ACCEPT')"><CheckCircle2 :size="16" />接受报价</button></div>
        </footer>
      </article>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { ArrowLeftRight, ArrowRight, Check, CheckCircle2, PackageOpen, Plus, Send, Users, X } from '@lucide/vue';
import { useRoute } from 'vue-router';
import { actOnTradeOffer, createTradeOffer, listTradeableInventory, listTradeOffers } from '../api/engagementApi';
import { listInventory, type InventoryItem } from '../api/inventoryApi';
import { listFriends } from '../api/socialApi';
import type { FriendListItem, TradeOfferView, TradeableInventoryItemView } from '../api/types';
import { useAuthStore } from '../stores/auth';

const route = useRoute();
const auth = useAuthStore();
const loading = ref(true);
const loadError = ref('');
const offers = ref<TradeOfferView[]>([]);
const allOffers = ref<TradeOfferView[]>([]);
const friends = ref<FriendListItem[]>([]);
const ownItems = ref<InventoryItem[]>([]);
const recipientItems = ref<TradeableInventoryItemView[]>([]);
const recipientId = ref(typeof route.query.recipient === 'string' ? route.query.recipient : '');
const offeredIds = ref(new Set<string>());
const requestedIds = ref(new Set<string>());
const activeStatus = ref('');
const composerOpen = ref(Boolean(recipientId.value));
const recipientLoading = ref(false);
const message = ref('');
const submitting = ref(false);
const actingId = ref('');
const composerMessage = ref('');
const composerSuccess = ref(false);
const fallbackItemImage = '/assets/games/cs2-header.jpg';
const tabs = [{ value: '', label: '全部' }, { value: 'PENDING', label: '待处理' }, { value: 'ACCEPTED', label: '已完成' }, { value: 'DECLINED', label: '已拒绝' }, { value: 'CANCELED', label: '已撤销' }];
const acceptedFriends = computed(() => friends.value.filter((friend) => friend.relationStatus === 'ACCEPTED'));
const recipientName = computed(() => acceptedFriends.value.find((friend) => friend.userId === recipientId.value)?.nickname || '');
const ownTradeable = computed(() => ownItems.value.filter((item) => item.status === 'NORMAL'));
const ownInitial = computed(() => auth.currentUser?.account?.slice(0, 1).toUpperCase() || '?');
const canSubmit = computed(() => Boolean(recipientId.value && offeredIds.value.size && requestedIds.value.size));

onMounted(async () => {
  await Promise.all([loadOffers(), loadSetup()]);
  if (recipientId.value) await loadRecipientInventory();
});

async function loadSetup() {
  try { [friends.value, ownItems.value] = await Promise.all([listFriends(), listInventory()]); }
  catch { composerMessage.value = '好友或库存读取失败，请稍后重试。'; }
}

async function loadOffers() {
  loading.value = true;
  loadError.value = '';
  try {
    offers.value = await listTradeOffers(activeStatus.value || undefined);
    allOffers.value = activeStatus.value ? await listTradeOffers() : offers.value;
  } catch (reason) {
    loadError.value = reason instanceof Error ? reason.message : '交易报价读取失败。';
  } finally { loading.value = false; }
}

async function loadRecipientInventory() {
  recipientItems.value = [];
  requestedIds.value = new Set();
  if (!recipientId.value) return;
  recipientLoading.value = true;
  try { recipientItems.value = await listTradeableInventory(recipientId.value); }
  catch (reason) { composerMessage.value = reason instanceof Error ? reason.message : '好友库存读取失败。'; }
  finally { recipientLoading.value = false; }
}

function toggleItem(target: Set<string>, itemId: string) {
  const copy = new Set(target);
  if (copy.has(itemId)) copy.delete(itemId);
  else if (copy.size < 8) copy.add(itemId);
  if (target === offeredIds.value) offeredIds.value = copy;
  else requestedIds.value = copy;
}

async function submitOffer() {
  if (!canSubmit.value || submitting.value) return;
  submitting.value = true;
  composerMessage.value = '';
  composerSuccess.value = false;
  try {
    await createTradeOffer({ recipientId: recipientId.value, offeredItemIds: [...offeredIds.value], requestedItemIds: [...requestedIds.value], message: message.value || null });
    composerSuccess.value = true;
    composerMessage.value = '交易报价已发送，双方物品已锁定。';
    offeredIds.value = new Set(); requestedIds.value = new Set(); message.value = '';
    await Promise.all([loadOffers(), loadSetup(), loadRecipientInventory()]);
  } catch (reason) { composerMessage.value = reason instanceof Error ? reason.message : '报价发送失败。'; }
  finally { submitting.value = false; }
}

async function act(offer: TradeOfferView, action: 'ACCEPT' | 'DECLINE' | 'CANCEL') {
  if (actingId.value) return;
  actingId.value = offer.offerId;
  try { await actOnTradeOffer(offer.offerId, action); await Promise.all([loadOffers(), loadSetup()]); }
  catch (reason) { loadError.value = reason instanceof Error ? reason.message : '报价操作失败。'; }
  finally { actingId.value = ''; }
}

function countFor(status: string) { return status ? allOffers.value.filter((offer) => offer.status === status).length : allOffers.value.length; }
function statusLabel(status: string) { return ({ PENDING: '待处理', ACCEPTED: '已完成', DECLINED: '已拒绝', CANCELED: '已撤销', EXPIRED: '已过期' } as Record<string, string>)[status] || status; }
function formatDate(value: string) { return new Date(value).toLocaleString('zh-CN', { month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' }); }
function rarityClass(value: string) { return value.toLowerCase(); }
</script>

<style scoped>
.trade-page { width: min(1120px, calc(100% - 32px)); margin: 24px auto 74px; color: #c7d5e0; }
.trade-header { display: flex; align-items: end; justify-content: space-between; gap: 20px; min-height: 150px; padding: 24px 26px; background: #162638 url('/assets/games/cs2-library-hero.jpg') center/cover; box-shadow: inset 0 0 0 1000px rgba(10, 20, 31, .74); border-bottom: 2px solid #66c0f4; }
.trade-header p { margin: 0; color: #66c0f4; font-size: .76rem; text-transform: uppercase; }
.trade-header h1 { margin: 3px 0; color: #fff; font-size: 2rem; font-weight: 400; }
.trade-header span { color: #a7b6c5; }
.trade-header button, .trade-composer footer button, .offer-card footer button { display: inline-flex; align-items: center; gap: 7px; border: 0; padding: 9px 14px; color: #fff; background: #417a9b; cursor: pointer; }
.trade-header button:hover, .trade-composer footer button:hover, .offer-card footer button:hover { background: #67a7cd; }
.trade-composer { margin-top: 16px; border: 1px solid #39526a; padding: 18px; background: #17283a; }
.trade-composer > header, .trade-sides > section > header { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin-bottom: 14px; }
.trade-composer > header strong { color: #fff; font-size: 1.08rem; font-weight: 400; }
.trade-composer > header span { color: #8f98a0; font-size: .78rem; }
.friend-select { display: flex; align-items: center; gap: 9px; width: min(420px, 100%); margin-bottom: 16px; padding: 7px 10px; color: #66c0f4; background: #0d1823; }
.friend-select select { width: 100%; border: 0; color: #fff; background: transparent; outline: 0; }
.friend-select option { color: #fff; background: #18293a; }
.trade-sides { display: grid; grid-template-columns: minmax(0, 1fr) 40px minmax(0, 1fr); gap: 10px; align-items: center; }
.trade-sides > section { min-width: 0; border: 1px solid #293d50; padding: 12px; background: #101c29; }
.trade-sides > section > header div { display: flex; align-items: center; gap: 8px; }
.trade-sides > section > header strong { color: #fff; font-weight: 400; }
.trade-sides > section > header b { color: #66c0f4; font-weight: 400; }
.side-avatar { display: grid; width: 32px; height: 32px; place-items: center; color: #fff; background: #315873; }
.side-avatar.friend { background: #5b6735; }
.trade-arrow { color: #7c91a4; }
.item-picker { display: grid; max-height: 330px; gap: 6px; overflow-y: auto; }
.item-picker > p { padding: 24px 8px; color: #758494; text-align: center; }
.trade-item { display: grid; grid-template-columns: 58px minmax(0, 1fr) 20px; align-items: center; gap: 10px; min-height: 64px; border: 1px solid #283b4d; padding: 4px; color: #c7d5e0; background: #18293a; text-align: left; cursor: pointer; }
.trade-item:hover, .trade-item.selected { border-color: #66c0f4; background: #213c53; }
.trade-item.rare { border-left: 3px solid #4b69ff; }
.trade-item.epic, .trade-item.legendary { border-left: 3px solid #d32ce6; }
.trade-item img { width: 58px; height: 54px; object-fit: contain; background: #0b141d; }
.trade-item strong, .trade-item small { display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.trade-item strong { color: #fff; font-weight: 400; }
.trade-item small { color: #7f91a4; font-size: .72rem; }
.trade-message { display: grid; gap: 5px; margin-top: 14px; color: #8f98a0; font-size: .78rem; }
.trade-message textarea { border: 1px solid #293d50; padding: 9px; color: #fff; background: #0d1823; resize: vertical; }
.trade-composer > footer { display: flex; align-items: center; justify-content: flex-end; gap: 14px; margin-top: 12px; color: #8bc34a; font-size: .8rem; }
.trade-composer > footer span.error { color: #e27a68; }
.trade-composer button:disabled, .offer-card button:disabled { opacity: .48; cursor: not-allowed; }
.trade-tabs { display: flex; gap: 2px; margin-top: 20px; border-bottom: 1px solid #35495e; }
.trade-tabs button { border: 0; border-bottom: 3px solid transparent; padding: 10px 14px; color: #8f98a0; background: transparent; cursor: pointer; }
.trade-tabs button.active, .trade-tabs button:hover { border-bottom-color: #66c0f4; color: #fff; }
.trade-tabs span { margin-left: 6px; color: #66c0f4; }
.trade-state { display: grid; min-height: 280px; place-content: center; justify-items: center; gap: 8px; color: #8f98a0; text-align: center; }
.trade-state strong { color: #fff; font-weight: 400; }
.trade-state button { border: 1px solid #52728f; padding: 6px 12px; color: #c7d5e0; background: #20364a; cursor: pointer; }
.offer-list { display: grid; gap: 12px; margin-top: 15px; }
.offer-card { border: 1px solid #30465a; background: #17283a; }
.offer-card > header, .offer-card > footer { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 11px 14px; background: #101d2a; }
.offer-card > header > div { display: flex; align-items: center; gap: 10px; }
.offer-card > header strong { display: inline-flex; align-items: center; gap: 5px; color: #fff; font-weight: 400; }
.offer-card time { color: #758494; font-size: .76rem; }
.offer-status { padding: 3px 7px; color: #d9e8f2; background: #3d5369; font-size: .7rem; }
.offer-status.accepted { color: #d8f4bd; background: #3f672a; }
.offer-status.declined, .offer-status.canceled { color: #d1d5d8; background: #555b61; }
.offer-status.pending { color: #c6ecff; background: #28698e; }
.offer-message { margin: 0; padding: 10px 14px 0; color: #9eb0c1; font-style: italic; }
.offer-sides { display: grid; grid-template-columns: minmax(0, 1fr) 34px minmax(0, 1fr); gap: 12px; align-items: center; padding: 14px; }
.offer-sides > svg { color: #61788d; }
.offer-sides section { min-width: 0; }
.offer-sides small { display: block; margin-bottom: 6px; color: #8f98a0; }
.offer-sides section > div { display: flex; min-height: 84px; gap: 7px; overflow-x: auto; }
.offer-sides section span { position: relative; width: 88px; flex: 0 0 88px; border: 1px solid #344b60; background: #0d1721; }
.offer-sides img { width: 100%; height: 62px; object-fit: contain; }
.offer-sides b { display: block; overflow: hidden; padding: 3px 5px; color: #c7d5e0; font-size: .67rem; font-weight: 400; text-overflow: ellipsis; white-space: nowrap; }
.offer-card > footer > span { color: #687b8e; font-size: .72rem; }
.offer-card > footer > div { display: flex; gap: 7px; }
.offer-card footer button.secondary { color: #c7d5e0; background: #3b4651; }
@media (max-width: 760px) {
  .trade-page { width: 100%; margin-top: 0; }
  .trade-header { align-items: stretch; flex-direction: column; padding: 18px; }
  .trade-composer { border-right: 0; border-left: 0; }
  .trade-sides, .offer-sides { grid-template-columns: 1fr; }
  .trade-arrow, .offer-sides > svg { justify-self: center; transform: rotate(90deg); }
  .trade-tabs { overflow-x: auto; }
  .trade-tabs button { flex: 0 0 auto; }
  .offer-card > header, .offer-card > footer { align-items: flex-start; flex-direction: column; }
}
</style>
