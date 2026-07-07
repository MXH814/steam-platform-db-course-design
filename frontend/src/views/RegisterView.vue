<template>
  <section class="auth-layout">
    <form class="form-panel" @submit.prevent="submit">
      <div class="section-heading compact">
        <p class="eyebrow">玩家注册</p>
        <h1>创建账号</h1>
      </div>

      <label>
        <span>账号</span>
        <input v-model.trim="form.account" autocomplete="username" required />
      </label>

      <label>
        <span>昵称</span>
        <input v-model.trim="form.nickname" required />
      </label>

      <label>
        <span>密码</span>
        <input v-model="form.password" type="password" autocomplete="new-password" required />
      </label>

      <p v-if="error" class="message error">{{ error }}</p>
      <button class="primary-button" type="submit" :disabled="submitting">
        {{ submitting ? '注册中...' : '注册并登录' }}
      </button>
      <RouterLink class="inline-link" to="/login">已有账号，返回登录</RouterLink>
    </form>
  </section>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { getApiError } from '../api/http';
import { useAuthStore } from '../stores/auth';

const auth = useAuthStore();
const router = useRouter();
const submitting = ref(false);
const error = ref('');
const form = reactive({
  account: '',
  nickname: '',
  password: ''
});

async function submit() {
  error.value = '';
  submitting.value = true;

  try {
    await auth.register(form);
    router.push({ name: 'account' });
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    submitting.value = false;
  }
}
</script>
