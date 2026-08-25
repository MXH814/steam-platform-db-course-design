<template>
  <section class="profile-page" :class="profile?.themeKey.toLowerCase().replace(/_/g, '-')">
    <div v-if="loading" class="profile-state">正在读取社区资料...</div>
    <div v-else-if="error" class="profile-state error"><ShieldAlert :size="28" /><strong>无法显示此资料</strong><span>{{ error }}</span></div>
    <template v-else-if="profile">
      <header class="profile-hero" :style="heroStyle">
        <div class="hero-shade" />
        <div class="profile-identity">
          <div class="profile-avatar" :class="avatarClass(profile.avatarKey)">{{ profile.nickname.slice(0, 1).toUpperCase() }}</div>
          <div>
            <p>社区个人资料</p>
            <h1>{{ profile.nickname }}</h1>
            <span>{{ profile.headline || '这个玩家还没有填写个人签名。' }}</span>
          </div>
        </div>
        <div class="profile-actions">
          <button v-if="profile.isOwnProfile" type="button" @click="editing = !editing"><Pencil :size="16" />{{ editing ? '取消编辑' : '编辑个人资料' }}</button>
          <button v-else-if="auth.isAuthenticated" type="button" @click="goTrade"><ArrowLeftRight :size="16" />发起交易</button>
        </div>
      </header>

      <div class="profile-columns">
        <main>
          <form v-if="editing && profile.isOwnProfile" class="profile-editor" @submit.prevent="saveProfile">
            <header><div><SlidersHorizontal :size="18" /><strong>自定义个人资料</strong></div><span>更改会保存到 Oracle</span></header>
            <label>个人签名<input v-model.trim="draft.headline" maxlength="120" placeholder="用一句话介绍自己" /></label>
            <label>个人简介<textarea v-model.trim="draft.bio" maxlength="2000" rows="5" placeholder="喜欢的游戏、常用位置或社区介绍" /></label>

            <fieldset>
              <legend>头像</legend>
              <button v-for="option in avatarOptions" :key="option.key" type="button" :class="['avatar-choice', avatarClass(option.key), { active: draft.avatarKey === option.key }]" :title="option.label" @click="draft.avatarKey = option.key">{{ profile.nickname.slice(0, 1).toUpperCase() }}</button>
            </fieldset>
            <fieldset>
              <legend>资料背景</legend>
              <button v-for="option in backgroundOptions" :key="option.key" type="button" class="background-choice" :class="{ active: draft.backgroundKey === option.key }" @click="draft.backgroundKey = option.key">
                <img :src="option.image" alt="" /><span>{{ option.label }}</span><Check v-if="draft.backgroundKey === option.key" :size="16" />
              </button>
            </fieldset>
            <fieldset>
              <legend>主题颜色</legend>
              <button v-for="option in themeOptions" :key="option.key" type="button" class="theme-choice" :class="[{ active: draft.themeKey === option.key }, option.className]" @click="draft.themeKey = option.key"><i />{{ option.label }}</button>
            </fieldset>

            <div class="editor-row">
              <label>展示游戏<select v-model="draft.showcaseGameId"><option :value="null">不展示</option><option value="GAME_CS2">Counter-Strike 2</option><option value="GAME_DST">饥荒联机版</option></select></label>
              <label>资料可见性<select v-model="draft.profileVisibility"><option value="PUBLIC">公开</option><option value="FRIENDS">仅好友</option><option value="PRIVATE">私密</option></select></label>
            </div>
            <div class="editor-footer"><span>{{ saveMessage }}</span><button type="submit" :disabled="saving"><Save :size="16" />{{ saving ? '正在保存' : '保存更改' }}</button></div>
          </form>

          <section v-else class="profile-panel about-panel">
            <header><strong>关于 {{ profile.nickname }}</strong><span>最近更新 {{ formatDate(profile.updatedAt) }}</span></header>
            <p>{{ profile.bio || '尚未填写个人简介。' }}</p>
          </section>

          <section v-if="profile.showcaseGameId" class="profile-panel game-showcase">
            <div><small>精选展柜</small><h2>{{ profile.showcaseGameName }}</h2><p>{{ profile.showcaseGameId === 'GAME_CS2' ? '竞技、库存与饰品市场档案' : '生存、成就与创意工坊档案' }}</p><RouterLink :to="`/games/${profile.showcaseGameId}`">查看商店页面 <ChevronRight :size="16" /></RouterLink></div>
            <img :src="profile.showcaseGameId === 'GAME_CS2' ? '/assets/games/cs2-header.jpg' : '/assets/games/dst-header.jpg'" :alt="profile.showcaseGameName || ''" />
          </section>

          <section class="profile-panel badge-showcase">
            <header><strong>徽章展柜</strong><span>{{ profile.totalXp }} XP</span></header>
            <div class="badge-grid">
              <button v-for="badge in profile.badges" :key="badge.badgeId" type="button" :class="['badge-card', badge.rarity.toLowerCase(), { featured: badge.isFeatured }]" :disabled="!profile.isOwnProfile || featuring" @click="featureBadge(badge.badgeId)">
                <span class="badge-icon"><component :is="badgeIcon(badge.iconKey)" :size="28" /></span>
                <span><strong>{{ badge.badgeName }}</strong><small>{{ badge.description }}</small><b>{{ badge.xpValue }} XP · {{ rarityLabel(badge.rarity) }}</b></span>
                <Star v-if="badge.isFeatured" :size="16" fill="currentColor" />
              </button>
              <p v-if="!profile.badges.length">还没有获得徽章。</p>
            </div>
          </section>
        </main>

        <aside>
          <section class="profile-level"><span>等级</span><strong>{{ level }}</strong><i><b :style="{ width: `${levelProgress}%` }" /></i><small>距离下一级还需 {{ nextLevelXp }} XP</small></section>
          <section class="profile-stats"><RouterLink to="/community"><strong>{{ profile.friendCount }}</strong><span>好友</span></RouterLink><div><strong>{{ profile.badges.length }}</strong><span>徽章</span></div><div><strong>{{ profile.totalXp }}</strong><span>经验值</span></div></section>
          <RouterLink v-if="profile.isOwnProfile" class="aside-link" to="/trade-offers"><ArrowLeftRight :size="17" />交易报价</RouterLink>
          <RouterLink class="aside-link" to="/community"><Users :size="17" />社区动态</RouterLink>
        </aside>
      </div>
    </template>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { ArrowLeftRight, Award, Check, ChevronRight, Gamepad2, Medal, Pencil, Save, Shield, ShieldAlert, SlidersHorizontal, Star, Trophy, Users } from '@lucide/vue';
import { useRoute, useRouter } from 'vue-router';
import { getOwnProfile, getProfile, setFeaturedBadge, updateOwnProfile } from '../api/engagementApi';
import type { PlayerProfileView, ProfileAvatarKey, ProfileBackgroundKey, ProfileThemeKey, ProfileVisibility, UpdatePlayerProfileRequest } from '../api/types';
import { useAuthStore } from '../stores/auth';

const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const profile = ref<PlayerProfileView | null>(null);
const loading = ref(true);
const saving = ref(false);
const featuring = ref(false);
const editing = ref(false);
const error = ref('');
const saveMessage = ref('');
const draft = reactive<UpdatePlayerProfileRequest>({
  headline: null, bio: null, avatarKey: 'AVATAR_BLUE', backgroundKey: 'BACKGROUND_CS2', themeKey: 'STEAM_BLUE', showcaseGameId: null, profileVisibility: 'PUBLIC'
});

const avatarOptions: { key: ProfileAvatarKey; label: string }[] = [
  { key: 'AVATAR_BLUE', label: '深海蓝' }, { key: 'AVATAR_ORANGE', label: '战术橙' }, { key: 'AVATAR_GREEN', label: '荒野绿' }, { key: 'AVATAR_PURPLE', label: '稀有紫' }
];
const backgroundOptions: { key: ProfileBackgroundKey; label: string; image: string }[] = [
  { key: 'BACKGROUND_CS2', label: 'CS2 竞技场', image: '/assets/games/cs2-library-hero.jpg' },
  { key: 'BACKGROUND_DST', label: '永恒领域', image: '/assets/games/dst-library-hero.jpg' },
  { key: 'BACKGROUND_LIBRARY', label: '游戏库', image: '/assets/games/cs2-library-cover.jpg' }
];
const themeOptions: { key: ProfileThemeKey; label: string; className: string }[] = [
  { key: 'STEAM_BLUE', label: '平台蓝', className: 'blue' }, { key: 'TACTICAL_ORANGE', label: '战术橙', className: 'orange' }, { key: 'SURVIVAL_GREEN', label: '生存绿', className: 'green' }
];
const backgroundMap: Record<ProfileBackgroundKey, string> = Object.fromEntries(backgroundOptions.map((item) => [item.key, item.image])) as Record<ProfileBackgroundKey, string>;
const heroStyle = computed(() => ({ backgroundImage: `url(${backgroundMap[profile.value?.backgroundKey || 'BACKGROUND_CS2']})` }));
const level = computed(() => Math.floor((profile.value?.totalXp || 0) / 100) + 1);
const levelProgress = computed(() => (profile.value?.totalXp || 0) % 100);
const nextLevelXp = computed(() => 100 - levelProgress.value);

onMounted(loadProfile);
watch(() => route.fullPath, loadProfile);

async function loadProfile() {
  loading.value = true;
  error.value = '';
  editing.value = false;
  try {
    const userId = typeof route.params.userId === 'string' ? route.params.userId : null;
    profile.value = route.name === 'own-profile' || (!userId && auth.isAuthenticated) ? await getOwnProfile() : await getProfile(userId || '');
    syncDraft();
  } catch (reason) {
    profile.value = null;
    error.value = reason instanceof Error ? reason.message : '资料读取失败。';
  } finally {
    loading.value = false;
  }
}

function syncDraft() {
  if (!profile.value) return;
  Object.assign(draft, {
    headline: profile.value.headline,
    bio: profile.value.bio,
    avatarKey: profile.value.avatarKey,
    backgroundKey: profile.value.backgroundKey,
    themeKey: profile.value.themeKey,
    showcaseGameId: profile.value.showcaseGameId,
    profileVisibility: profile.value.profileVisibility
  });
}

async function saveProfile() {
  if (saving.value) return;
  saving.value = true;
  saveMessage.value = '';
  try {
    profile.value = await updateOwnProfile({ ...draft });
    syncDraft();
    editing.value = false;
    saveMessage.value = '个人资料已保存。';
  } catch (reason) {
    saveMessage.value = reason instanceof Error ? reason.message : '保存失败。';
  } finally {
    saving.value = false;
  }
}

async function featureBadge(badgeId: string) {
  if (!profile.value?.isOwnProfile || featuring.value) return;
  featuring.value = true;
  try { profile.value = await setFeaturedBadge(badgeId); } finally { featuring.value = false; }
}

function goTrade() { router.push({ name: 'trade-offers', query: { recipient: profile.value?.userId } }); }
function avatarClass(key: ProfileAvatarKey) { return key.toLowerCase().replace(/_/g, '-'); }
function formatDate(value: string) { return new Date(value).toLocaleDateString('zh-CN'); }
function rarityLabel(value: string) { return value === 'EPIC' ? '史诗' : value === 'RARE' ? '稀有' : '普通'; }
function badgeIcon(key: string) { return key.includes('MARKET') ? ArrowLeftRight : key.includes('SURVIVAL') ? Shield : key.includes('ACH') ? Trophy : key.includes('COMMUNITY') ? Users : key.includes('GAME') ? Gamepad2 : key.includes('AWARD') ? Award : Medal; }
</script>

<style scoped>
.profile-page { --accent: #66c0f4; width: min(1080px, calc(100% - 32px)); margin: 24px auto 70px; color: #d6d7d8; }
.profile-page.tactical-orange { --accent: #f29d49; }
.profile-page.survival-green { --accent: #8bc34a; }
.profile-state { display: grid; min-height: 420px; place-content: center; gap: 10px; color: #8f98a0; text-align: center; }
.profile-state.error strong { color: #fff; font-size: 1.25rem; }
.profile-hero { position: relative; min-height: 260px; overflow: hidden; background-position: center; background-size: cover; border-bottom: 2px solid var(--accent); }
.hero-shade { position: absolute; inset: 0; background: rgba(10, 18, 28, .58); }
.profile-identity { position: absolute; right: 28px; bottom: 24px; left: 28px; display: flex; align-items: flex-end; gap: 22px; }
.profile-identity p { margin: 0 0 4px; color: var(--accent); font-size: .76rem; text-transform: uppercase; }
.profile-identity h1 { margin: 0; color: #fff; font-size: 2rem; font-weight: 400; }
.profile-identity span { color: #c7d5e0; }
.profile-avatar { display: grid; width: 118px; height: 118px; flex: 0 0 118px; place-items: center; border: 3px solid #71808e; background: #294d6b; color: #fff; font-size: 3rem; box-shadow: 0 0 0 4px rgba(0, 0, 0, .45); }
.profile-avatar.avatar-orange, .avatar-choice.avatar-orange { background: #9b542b; }
.profile-avatar.avatar-green, .avatar-choice.avatar-green { background: #3f6c42; }
.profile-avatar.avatar-purple, .avatar-choice.avatar-purple { background: #60447c; }
.profile-actions { position: absolute; top: 18px; right: 18px; }
.profile-actions button, .editor-footer button { display: inline-flex; align-items: center; gap: 8px; border: 0; padding: 9px 15px; color: #fff; background: #3d6f8e; cursor: pointer; }
.profile-actions button:hover, .editor-footer button:hover { background: #67a7cd; }
.profile-columns { display: grid; grid-template-columns: minmax(0, 1fr) 260px; gap: 18px; padding: 22px; background: rgba(15, 25, 36, .86); }
.profile-columns main { min-width: 0; }
.profile-panel, .profile-editor, .profile-level, .profile-stats { margin-bottom: 16px; padding: 18px; background: rgba(27, 40, 55, .92); border: 1px solid rgba(103, 193, 245, .12); }
.profile-panel > header, .profile-editor > header { display: flex; justify-content: space-between; gap: 12px; margin-bottom: 14px; color: #8f98a0; font-size: .78rem; }
.profile-panel > header strong, .profile-editor > header strong { color: #fff; font-size: 1rem; font-weight: 400; }
.profile-editor > header div { display: flex; align-items: center; gap: 8px; }
.about-panel p { margin: 0; color: #c7d5e0; white-space: pre-wrap; }
.game-showcase { display: grid; grid-template-columns: minmax(0, 1fr) 260px; gap: 18px; align-items: center; padding: 0; overflow: hidden; }
.game-showcase > div { padding: 18px; }
.game-showcase small { color: var(--accent); text-transform: uppercase; }
.game-showcase h2 { margin: 6px 0 3px; color: #fff; font-weight: 400; }
.game-showcase p { margin: 0 0 18px; color: #8f98a0; }
.game-showcase a { display: inline-flex; align-items: center; color: var(--accent); }
.game-showcase img { width: 100%; height: 160px; object-fit: cover; }
.badge-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; }
.badge-card { position: relative; display: grid; grid-template-columns: 50px minmax(0, 1fr) auto; align-items: center; gap: 12px; min-height: 92px; border: 1px solid #34465a; padding: 12px; color: #c7d5e0; background: #162535; text-align: left; }
.badge-card:not(:disabled) { cursor: pointer; }
.badge-card:not(:disabled):hover, .badge-card.featured { border-color: var(--accent); background: #20364a; }
.badge-card > svg { color: #f3cb67; }
.badge-icon { display: grid; width: 48px; height: 48px; place-items: center; border: 2px solid #64778c; border-radius: 50%; color: #9acbe7; background: #0e1925; }
.badge-card.rare .badge-icon { color: #b8a0ee; border-color: #7656a4; }
.badge-card.epic .badge-icon { color: #f3cb67; border-color: #a97a30; }
.badge-card strong, .badge-card small, .badge-card b { display: block; }
.badge-card strong { color: #fff; font-weight: 400; }
.badge-card small { overflow: hidden; color: #8f98a0; text-overflow: ellipsis; white-space: nowrap; }
.badge-card b { color: var(--accent); font-size: .72rem; font-weight: 400; }
.profile-editor label { display: grid; gap: 5px; margin-bottom: 14px; color: #8f98a0; font-size: .78rem; }
.profile-editor input, .profile-editor textarea, .profile-editor select { width: 100%; border: 1px solid #30465a; border-radius: 0; padding: 9px 10px; color: #fff; background: #101923; resize: vertical; }
.profile-editor fieldset { display: flex; flex-wrap: wrap; gap: 9px; margin: 0 0 16px; border: 0; padding: 0; }
.profile-editor legend { width: 100%; margin-bottom: 6px; color: #8f98a0; font-size: .78rem; }
.avatar-choice { width: 48px; height: 48px; border: 2px solid transparent; color: #fff; background: #294d6b; cursor: pointer; }
.avatar-choice.active { border-color: #fff; box-shadow: 0 0 0 2px var(--accent); }
.background-choice { position: relative; width: 150px; height: 76px; overflow: hidden; border: 2px solid transparent; padding: 0; color: #fff; background: #101923; cursor: pointer; }
.background-choice img { width: 100%; height: 100%; object-fit: cover; opacity: .68; }
.background-choice span { position: absolute; right: 6px; bottom: 5px; left: 6px; text-align: left; }
.background-choice svg { position: absolute; top: 5px; right: 5px; }
.background-choice.active { border-color: var(--accent); }
.theme-choice { display: inline-flex; align-items: center; gap: 8px; border: 1px solid #34465a; padding: 8px 10px; color: #c7d5e0; background: #162535; cursor: pointer; }
.theme-choice i { width: 18px; height: 18px; background: #66c0f4; }
.theme-choice.orange i { background: #f29d49; }
.theme-choice.green i { background: #8bc34a; }
.theme-choice.active { border-color: #fff; color: #fff; }
.editor-row { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.editor-footer { display: flex; align-items: center; justify-content: flex-end; gap: 14px; color: #a8c8dc; font-size: .8rem; }
.profile-level span, .profile-level small, .profile-stats span { display: block; color: #8f98a0; font-size: .76rem; }
.profile-level strong { display: grid; width: 58px; height: 58px; place-items: center; margin: 8px 0; border: 3px solid var(--accent); border-radius: 50%; color: #fff; font-size: 1.45rem; font-weight: 400; }
.profile-level i { display: block; height: 4px; margin: 8px 0; background: #0d1721; }
.profile-level i b { display: block; height: 100%; background: var(--accent); }
.profile-stats { display: grid; grid-template-columns: repeat(3, 1fr); gap: 6px; text-align: center; }
.profile-stats strong { display: block; color: #fff; font-size: 1.15rem; font-weight: 400; }
.aside-link { display: flex; align-items: center; gap: 9px; margin-bottom: 6px; padding: 10px 12px; color: #c7d5e0; background: #26394d; }
.aside-link:hover { color: #fff; background: #3c5a76; }
@media (max-width: 760px) {
  .profile-page { width: 100%; margin-top: 0; }
  .profile-hero { min-height: 310px; }
  .profile-identity { align-items: center; flex-direction: column; text-align: center; }
  .profile-avatar { width: 92px; height: 92px; flex-basis: 92px; }
  .profile-columns { grid-template-columns: 1fr; padding: 12px; }
  .game-showcase, .editor-row { grid-template-columns: 1fr; }
  .badge-grid { grid-template-columns: 1fr; }
}
</style>
