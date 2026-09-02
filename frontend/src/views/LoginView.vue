<template>
  <section class="auth-layout">
    <form class="form-panel" @submit.prevent="submit">
      <div class="section-heading compact">
        <p class="eyebrow">账户认证</p>
        <h1>登录</h1>
      </div>

      <label>
        <span>角色</span>
        <select v-model="form.role">
          <option value="PLAYER">玩家</option>
          <option value="ADMIN">管理员</option>
          <option value="DEVELOPER">开发商</option>
        </select>
      </label>

      <label>
        <span>账号</span>
        <input v-model.trim="form.account" autocomplete="username" required />
      </label>

      <label>
        <span>密码</span>
        <input v-model="form.password" type="password" autocomplete="current-password" required />
      </label>

      <p v-if="sessionNotice" class="message error">{{ sessionNotice }}</p>
      <p v-else-if="error" class="message error">{{ error }}</p>
      <button class="primary-button" type="submit" :disabled="submitting">
        {{ submitting ? '登录中...' : '登录' }}
      </button>
      <RouterLink class="inline-link" to="/register">没有账号，注册玩家账号</RouterLink>
    </form>
  </section>
</template>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { getApiError } from '../api/http';
import type { LoginRole } from '../api/types';
import { useAuthStore } from '../stores/auth';

const auth = useAuthStore();
const route = useRoute();

const sessionNotice = computed(() => {
  if (route.query.expired === '1') {
    return '登录状态已失效，请重新登录。';
  }
  if (typeof route.query.redirect === 'string' && route.query.redirect && route.query.redirect !== '/store') {
    return '该页面需要登录后访问，请先完成登录。';
  }
  return '';
});
const router = useRouter();
const submitting = ref(false);
const error = ref('');
const form = reactive({
  role: 'PLAYER' as LoginRole,
  account: '',
  password: ''
});

async function submit() {
  error.value = '';
  submitting.value = true;

  try {
    await auth.login(form);
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/store';
    router.push(redirect);
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    submitting.value = false;
  }
}
</script>
