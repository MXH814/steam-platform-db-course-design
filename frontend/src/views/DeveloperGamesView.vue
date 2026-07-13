<template>
  <section class="developer-games view-stack">
    <div class="section-heading">
      <p class="eyebrow">Developer Console</p>
      <h1>开发商游戏管理</h1>
    </div>

    <div class="manage-hero">
      <div>
        <span class="pill">当前开发商 {{ developerId || '-' }}</span>
        <h2>维护商店页面中的游戏资料</h2>
        <p>新游戏创建后默认下架，管理员审核并上架后才会出现在公开商店。</p>
      </div>
    </div>

    <div v-if="message" class="state-panel success">{{ message }}</div>
    <div v-if="error" class="state-panel error">{{ error }}</div>

    <section class="manage-layout">
      <form class="form-panel manage-form" @submit.prevent="submitGame">
        <header class="form-heading">
          <div>
            <p class="eyebrow">{{ editingGameId ? 'Edit Game' : 'New Game' }}</p>
            <h2>{{ editingGameId ? '修改游戏' : '新建游戏' }}</h2>
          </div>
          <button v-if="editingGameId" class="ghost-button" type="button" @click="resetForm">新建</button>
        </header>

        <label>
          <span>游戏名称</span>
          <input v-model.trim="form.gameName" required maxlength="80" />
        </label>

        <div class="field-grid">
          <label>
            <span>原价</span>
            <input v-model.number="form.basePrice" type="number" min="0" step="0.01" required />
          </label>
          <label>
            <span>折扣系数</span>
            <input v-model.number="form.discountRate" type="number" min="0" max="1" step="0.01" required />
          </label>
        </div>

        <div class="field-grid">
          <label>
            <span>发行日期</span>
            <input v-model="form.releaseDate" type="date" required />
          </label>
          <label>
            <span>口碑</span>
            <select v-model="form.reputation">
              <option value="">暂无口碑</option>
              <option v-for="item in reputationOptions" :key="item.value" :value="item.value">{{ item.label }}</option>
            </select>
          </label>
        </div>

        <div class="price-preview">
          <span>商店价格预览</span>
          <GamePriceBlock :base-price="safeBasePrice" :final-price="previewFinalPrice" :discount-rate="safeDiscountRate" />
        </div>

        <button class="primary-button" type="submit" :disabled="submitting || !developerId">
          {{ submitting ? '提交中...' : editingGameId ? '保存修改' : '创建游戏' }}
        </button>
      </form>

      <section class="data-panel manage-panel">
        <header class="panel-header">
          <div>
            <h2>我的游戏</h2>
            <span>{{ games.length }} 款</span>
          </div>
          <button class="ghost-button" type="button" :disabled="loading" @click="loadGames">刷新</button>
        </header>

        <PageState v-if="loading" kind="loading" title="正在加载开发商游戏" />
        <PageState v-else-if="!games.length" kind="empty" title="暂无游戏" message="创建第一款游戏后，它会先以已下架状态进入列表。" />
        <div v-else class="game-manage-list">
          <article v-for="game in games" :key="game.gameId" class="game-manage-row">
            <div class="game-main">
              <strong>{{ game.gameName }}</strong>
              <span>{{ game.gameId }} · {{ formatDate(game.releaseDate) }}</span>
            </div>
            <GamePriceBlock :base-price="game.basePrice" :final-price="game.finalPrice" :discount-rate="game.discountRate" compact />
            <StatusBadge :status="game.status" />
            <button class="secondary-button" type="button" @click="editGame(game)">编辑</button>
            <button
              class="ghost-button danger"
              type="button"
              :disabled="deletingGameId === game.gameId || game.status !== 'OFFLINE'"
              :title="game.status === 'OFFLINE' ? '删除游戏' : '已上架游戏需要先由管理员下架'"
              @click="deleteGame(game)"
            >
              删除
            </button>
          </article>
        </div>
      </section>
    </section>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { createDeveloperGame, deleteDeveloperGame, getGames, updateDeveloperGame } from '../api/games';
import { getApiError } from '../api/http';
import type { GameListItem, GameManageForm } from '../api/types';
import GamePriceBlock from '../components/GamePriceBlock.vue';
import PageState from '../components/PageState.vue';
import StatusBadge from '../components/StatusBadge.vue';
import { useAuthStore } from '../stores/auth';

const reputationOptions = [
  { value: 'OVERWHELMINGLY_POSITIVE', label: '好评如潮' },
  { value: 'VERY_POSITIVE', label: '特别好评' },
  { value: 'MOSTLY_POSITIVE', label: '多半好评' },
  { value: 'MIXED', label: '褒贬不一' },
  { value: 'NEGATIVE', label: '差评' }
];

const auth = useAuthStore();
const developerId = computed(() => auth.currentUser?.principalId || '');
const games = ref<GameListItem[]>([]);
const editingGameId = ref('');
const loading = ref(false);
const submitting = ref(false);
const deletingGameId = ref('');
const message = ref('');
const error = ref('');

const form = reactive<GameManageForm>({
  gameName: '',
  basePrice: 0,
  discountRate: 1,
  releaseDate: new Date().toISOString().slice(0, 10),
  reputation: ''
});

const safeBasePrice = computed(() => Math.max(Number(form.basePrice) || 0, 0));
const safeDiscountRate = computed(() => Math.min(Math.max(Number(form.discountRate) || 0, 0), 1));
const previewFinalPrice = computed(() => Number((safeBasePrice.value * safeDiscountRate.value).toFixed(2)));

onMounted(loadGames);

async function loadGames() {
  if (!developerId.value) {
    error.value = '当前账号不是有效开发商账号。';
    return;
  }

  loading.value = true;
  error.value = '';
  try {
    const [online, offline] = await Promise.all([
      getGames({ developerId: developerId.value, status: 'ONLINE', pageSize: 100 }),
      getGames({ developerId: developerId.value, status: 'OFFLINE', pageSize: 100 })
    ]);
    games.value = mergeGames([...online.items, ...offline.items]);
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    loading.value = false;
  }
}

async function submitGame() {
  message.value = '';
  error.value = '';
  submitting.value = true;

  try {
    const payload = {
      gameName: form.gameName,
      basePrice: safeBasePrice.value,
      discountRate: safeDiscountRate.value,
      releaseDate: `${form.releaseDate}T00:00:00`,
      reputation: form.reputation || null
    };

    if (editingGameId.value) {
      const updated = await updateDeveloperGame(editingGameId.value, payload);
      message.value = `游戏已更新：${updated.gameName}`;
    } else {
      const created = await createDeveloperGame({ ...payload, devId: developerId.value });
      message.value = `游戏已创建：${created.gameName}。默认下架，等待管理员上架。`;
      editingGameId.value = created.gameId;
    }

    await loadGames();
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    submitting.value = false;
  }
}

async function deleteGame(game: GameListItem) {
  if (game.status !== 'OFFLINE') {
    error.value = '已上架游戏不能由开发商直接删除，请先联系管理员下架。';
    return;
  }

  if (!window.confirm(`确定删除「${game.gameName}」吗？已有订单、评价、成就或饰品数据的游戏可能无法删除。`)) {
    return;
  }

  message.value = '';
  error.value = '';
  deletingGameId.value = game.gameId;

  try {
    await deleteDeveloperGame(game.gameId);
    message.value = `游戏已删除：${game.gameName}`;
    if (editingGameId.value === game.gameId) {
      resetForm();
    }
    await loadGames();
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    deletingGameId.value = '';
  }
}

function editGame(game: GameListItem) {
  editingGameId.value = game.gameId;
  form.gameName = game.gameName;
  form.basePrice = game.basePrice;
  form.discountRate = game.discountRate;
  form.releaseDate = game.releaseDate.slice(0, 10);
  form.reputation = game.reputation && reputationOptions.some((item) => item.value === game.reputation) ? game.reputation : '';
}

function resetForm() {
  editingGameId.value = '';
  form.gameName = '';
  form.basePrice = 0;
  form.discountRate = 1;
  form.releaseDate = new Date().toISOString().slice(0, 10);
  form.reputation = '';
}

function mergeGames(items: GameListItem[]) {
  return Array.from(new Map(items.map((item) => [item.gameId, item])).values()).sort((a, b) => a.gameName.localeCompare(b.gameName, 'zh-CN'));
}

function formatDate(value: string) {
  return value ? new Date(value).toLocaleDateString('zh-CN') : '-';
}
</script>

<style scoped>
.developer-games {
  gap: 1.1rem;
}

.manage-hero,
.manage-layout,
.game-manage-row,
.price-preview,
.field-grid,
.form-heading {
  display: grid;
  gap: 1rem;
}

.manage-hero {
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: end;
  border: 1px solid rgba(102, 192, 244, 0.2);
  border-radius: 8px;
  padding: 1.2rem;
  background:
    linear-gradient(135deg, rgba(26, 159, 255, 0.14), transparent 48%),
    rgba(20, 28, 39, 0.92);
}

.manage-hero h2,
.manage-hero p,
.form-heading p {
  margin: 0;
}

.manage-hero h2 {
  margin-top: 0.7rem;
  font-size: 1.45rem;
}

.manage-hero p,
.panel-header span,
.game-main span,
.price-preview span {
  color: var(--steam-muted);
}

.manage-layout {
  grid-template-columns: minmax(320px, 420px) minmax(0, 1fr);
  align-items: start;
}

.manage-form {
  width: 100%;
}

.form-heading {
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: start;
}

.field-grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.price-preview {
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: center;
  border: 1px solid rgba(151, 170, 195, 0.14);
  border-radius: 6px;
  padding: 0.8rem;
  background: rgba(8, 13, 19, 0.38);
}

.manage-panel :deep(.page-state) {
  margin: 1rem;
}

.panel-header > div {
  display: grid;
  gap: 0.2rem;
}

.game-manage-list {
  display: grid;
}

.game-manage-row {
  grid-template-columns: minmax(0, 1fr) auto auto auto auto;
  align-items: center;
  padding: 1rem 1.1rem;
  border-bottom: 1px solid rgba(151, 170, 195, 0.12);
}

.game-manage-row:hover {
  background: rgba(127, 198, 255, 0.06);
}

.game-main {
  display: grid;
  gap: 0.25rem;
  min-width: 0;
}

.game-main strong {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.danger {
  color: #ffb9b9;
}

.danger:hover {
  border-color: rgba(255, 135, 135, 0.52);
  color: #ffd6d6;
}

@media (max-width: 900px) {
  .manage-hero,
  .manage-layout,
  .game-manage-row,
  .field-grid,
  .price-preview,
  .form-heading {
    grid-template-columns: 1fr;
  }
}
</style>
