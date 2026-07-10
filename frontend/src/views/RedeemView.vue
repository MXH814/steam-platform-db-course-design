<template>
  <div>
    <section v-if="auth.isDeveloper" class="form-panel">
      <h2>开发商无需兑换 CDKey</h2>
      <p>开发商只需生成 CDKey 并分发给玩家。请前往 <RouterLink to="/developer/cdkeys">CDKey 批次管理</RouterLink> 页面生成并查看明文密钥（明文仅展示一次）。</p>
    </section>

    <div v-else class="cdkey-modal-overlay">
      <div class="cdkey-modal" role="dialog" aria-labelledby="cdkey-title">
        <button class="close-x" @click="cancel" aria-label="关闭">×</button>
        <header class="modal-header">
          <h1 id="cdkey-title">输入您的产品代码</h1>
          <p class="muted">输入代码将会在 Steam 上注册您的产品，并将其添加至您的库中。您可在此处输入零售 CD/DVD 上或其他 Steam 产品上的序列号。</p>
        </header>

        <section class="modal-body">
          <div class="examples">
            <div class="examples-label">产品代码示例</div>
            <pre class="examples-text">AAAAA-BBBBB-CCCCC
AAAAA-BBBBB-CCCCC-DDDDD-EEEEE
237ABCDGHJLPRST 23</pre>
          </div>

          <form class="cdkey-form" @submit.prevent="redeem">
            <label class="input-label">
              <span class="label-title">在此处输入您的产品代码：</span>
              <input v-model.trim="cdkey" class="cdkey-input" placeholder="例如 AAAAA-BBBBB-CCCCC" autocomplete="off" required />
            </label>

            <div class="modal-actions">
              <button type="button" class="ghost-button" @click="cancel" :disabled="submitting">取消</button>
              <button type="submit" class="primary-button" :disabled="submitting || !cdkey">
                {{ submitting ? '兑换中...' : '确认' }}
              </button>
            </div>
          </form>

          <p v-if="error" class="message error">{{ error }}</p>
          <p v-if="message" class="message success">{{ message }}</p>

          <section v-if="result" class="details-panel result-panel">
            <dl>
              <div>
                <dt>结果</dt>
                <dd>{{ result.result }}</dd>
              </div>
              <div>
                <dt>游戏</dt>
                <dd>{{ result.gameId || '-' }}</dd>
              </div>
              <div>
                <dt>库记录</dt>
                <dd>{{ result.libraryId || '-' }}</dd>
              </div>
              <div>
                <dt>说明</dt>
                <dd>{{ result.message }}</dd>
              </div>
            </dl>
          </section>
        </section>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter, RouterLink } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import { redeemCdkey, type CdkeyRedeemResult } from '../api/coreApi';
import { getApiError } from '../api/http';

const router = useRouter();
const auth = useAuthStore();
const cdkey = ref('');
const submitting = ref(false);
const result = ref<CdkeyRedeemResult | null>(null);
const error = ref('');
const message = ref('');

function normalizeErrorMessage(rawMessage: string): string {
  if (!rawMessage) {
    return '兑换失败，请稍后重试。';
  }

  if (rawMessage.includes('ORA-') || rawMessage.includes('Oracle')) {
    return '玩家已拥有该游戏或 CDKey 已被兑换。';
  }

  if (rawMessage.includes('already owns') || rawMessage.includes('already owned')) {
    return '您已经拥有该游戏，无需重复兑换。';
  }

  if (rawMessage.includes('redeemed') && !rawMessage.toLowerCase().includes('success')) {
    return '该 CDKey 已被兑换，请使用其他 CDKey。';
  }

  if (rawMessage.includes('invalid') || rawMessage.includes('does not exist')) {
    return '无效的 CDKey，请检查输入是否正确。';
  }

  if (rawMessage.includes('expire') || rawMessage.includes('valid time')) {
    return '该 CDKey 已过期或尚未生效。';
  }

  return rawMessage;
}

async function redeem() {
  if (!cdkey.value) return;
  submitting.value = true;
  result.value = null;
  error.value = '';
  message.value = '';

  try {
    result.value = await redeemCdkey(cdkey.value);
    if (result.value.result && result.value.result.toLowerCase().includes('success')) {
      message.value = result.value.message || 'CDKey 兑换成功！游戏已添加到您的库中。';
      cdkey.value = '';
    } else {
      error.value = normalizeErrorMessage(result.value.message);
    }
  } catch (requestError) {
    error.value = normalizeErrorMessage(getApiError(requestError));
  } finally {
    submitting.value = false;
  }
}

function cancel() {
  // 清理状态
  cdkey.value = '';
  result.value = null;
  error.value = '';
  message.value = '';
  // 返回上一页，如果没有历史则回到首页
  try {
    router.back();
    // 有时 router.back 无法返回（单页直接打开），则导航到主页
    setTimeout(() => {
      if (window.location.pathname === '/redeem') {
        router.push({ name: 'home' });
      }
    }, 120);
  } catch {
    router.push({ name: 'home' });
  }
}
</script>

<style scoped>
.cdkey-modal-overlay {
  position: fixed;
  inset: 0;
  display: grid;
  place-items: center;
  background: rgba(10, 12, 14, 0.6);
  z-index: 1000;
}

.cdkey-modal {
  width: min(760px, calc(100% - 48px));
  background: linear-gradient(180deg, rgba(27,40,56,0.98), rgba(22,32,45,0.98));
  border: 1px solid rgba(102, 170, 200, 0.12);
  box-shadow: 0 8px 30px rgba(0,0,0,0.6);
  border-radius: 6px;
  padding: 18px 20px 20px;
  position: relative;
}

.close-x {
  position: absolute;
  right: 12px;
  top: 10px;
  background: transparent;
  border: none;
  color: #cfdff0;
  font-size: 20px;
  cursor: pointer;
}

.modal-header h1 {
  margin: 4px 0 6px 0;
  color: #eef2f8;
}

.modal-header .muted {
  margin: 0 0 12px 0;
  color: #9aa9b7;
  font-size: 0.95rem;
}

.examples {
  background: rgba(15, 22, 32, 0.6);
  border: 1px solid rgba(151,170,195,0.06);
  padding: 12px;
  border-radius: 4px;
  margin-bottom: 14px;
}

.examples-label {
  color: #8f98a0;
  font-weight: 800;
  font-size: 0.8rem;
  margin-bottom: 6px;
}

.examples-text {
  margin: 0;
  color: #cbdff2;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, 'Courier New', monospace;
  font-size: 0.9rem;
  white-space: pre-wrap;
}

.input-label {
  display: block;
  margin: 8px 0 12px 0;
}

.label-title {
  display: block;
  margin-bottom: 6px;
  color: #c9d3e3;
  font-weight: 700;
}

.cdkey-input {
  width: 100%;
  padding: 14px 12px;
  font-size: 1.05rem;
  border-radius: 6px;
  border: 1px solid rgba(151,170,195,0.14);
  background: rgba(11,16,22,0.6);
  color: #eef2f8;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 8px;
}

.result-panel {
  margin-top: 14px;
}

/* reuse existing button styles where possible */
.primary-button {
  min-height: 40px;
  padding: 0 16px;
  font-weight: 800;
}

.ghost-button {
  min-height: 40px;
  padding: 0 16px;
  background: transparent;
  border: 1px solid rgba(151,170,195,0.12);
  color: #dbe9f5;
}

.message {
  margin-top: 10px;
}

</style>
