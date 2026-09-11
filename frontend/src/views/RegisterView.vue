<template>
  <section class="auth-layout">
    <form class="form-panel" @submit.prevent="submit">
      <div class="section-heading compact">
        <p class="eyebrow">玩家注册</p>
        <h1>创建账号</h1>
      </div>

      <label>
        <span>账号</span>
        <input v-model.trim="form.account" autocomplete="username" minlength="3" maxlength="64" required />
      </label>

      <label>
        <span>昵称</span>
        <input v-model.trim="form.nickname" autocomplete="nickname" maxlength="64" required />
      </label>

      <div class="field-group">
        <label for="register-password">密码</label>
        <div class="password-input">
          <input id="register-password" v-model="form.password" :type="showPassword ? 'text' : 'password'" autocomplete="new-password" minlength="8" maxlength="128" required />
          <button type="button" :title="showPassword ? '隐藏密码' : '显示密码'" :aria-label="showPassword ? '隐藏密码' : '显示密码'" @click="showPassword = !showPassword">
            <EyeOff v-if="showPassword" :size="18" />
            <Eye v-else :size="18" />
          </button>
        </div>
        <small>至少 8 位，建议同时包含字母、数字和符号。</small>
      </div>

      <div class="field-group">
        <label for="register-confirm-password">确认密码</label>
        <div class="password-input">
          <input id="register-confirm-password" v-model="form.confirmPassword" :type="showConfirmPassword ? 'text' : 'password'" autocomplete="new-password" minlength="8" maxlength="128" required />
          <button type="button" :title="showConfirmPassword ? '隐藏确认密码' : '显示确认密码'" :aria-label="showConfirmPassword ? '隐藏确认密码' : '显示确认密码'" @click="showConfirmPassword = !showConfirmPassword">
            <EyeOff v-if="showConfirmPassword" :size="18" />
            <Eye v-else :size="18" />
          </button>
        </div>
        <small v-if="passwordsDiffer" class="field-error">两次输入的密码不一致。</small>
      </div>

      <p v-if="error" class="message error">{{ error }}</p>
      <button class="primary-button" type="submit" :disabled="submitting">
        {{ submitting ? '注册中...' : '注册并登录' }}
      </button>
      <RouterLink class="inline-link" to="/login">已有账号，返回登录</RouterLink>
    </form>
  </section>
</template>

<script setup lang="ts">
import { Eye, EyeOff } from '@lucide/vue';
import { computed, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { getApiError } from '../api/http';
import { useAuthStore } from '../stores/auth';

const auth = useAuthStore();
const router = useRouter();
const submitting = ref(false);
const error = ref('');
const showPassword = ref(false);
const showConfirmPassword = ref(false);
const form = reactive({
  account: '',
  nickname: '',
  password: '',
  confirmPassword: ''
});
const passwordsDiffer = computed(() => Boolean(form.confirmPassword) && form.password !== form.confirmPassword);

async function submit() {
  error.value = '';
  if (form.password !== form.confirmPassword) {
    error.value = '两次输入的密码不一致。';
    return;
  }
  submitting.value = true;

  try {
    await auth.register({ account: form.account, nickname: form.nickname, password: form.password });
    router.push({ name: 'account' });
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    submitting.value = false;
  }
}
</script>

<style scoped>
.field-group {
  display: grid;
  gap: 7px;
  color: #c9d3e3;
  font-size: 0.92rem;
  font-weight: 700;
}

.password-input {
  position: relative;
}

.password-input input {
  width: 100%;
  padding-right: 2.8rem;
}

.password-input button {
  position: absolute;
  top: 50%;
  right: 0.35rem;
  display: grid;
  width: 2.15rem;
  height: 2.15rem;
  padding: 0;
  place-items: center;
  transform: translateY(-50%);
  border: 0;
  color: var(--steam-muted);
  background: transparent;
  cursor: pointer;
}

.password-input button:hover,
.password-input button:focus-visible {
  color: #fff;
}

.field-error {
  color: #ff9b9b;
}
</style>
