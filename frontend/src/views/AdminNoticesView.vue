<template>
  <section class="admin-layout">
    <div class="section-heading">
      <p class="eyebrow">管理员</p>
      <h1>公告管理</h1>
    </div>

    <form class="form-panel" @submit.prevent="createNotice">
      <h2>发布公告</h2>
      <label>
        <span>标题</span>
        <input v-model.trim="createForm.title" required />
      </label>
      <label>
        <span>内容</span>
        <textarea v-model.trim="createForm.content" rows="5" required />
      </label>
      <label>
        <span>优先级</span>
        <input v-model.number="createForm.priority" type="number" min="0" max="9" required />
      </label>
      <label>
        <span>过期时间</span>
        <input v-model="createExpireTime" type="datetime-local" />
      </label>
      <button class="primary-button" type="submit" :disabled="submitting">
        {{ submitting ? '提交中...' : '发布' }}
      </button>
    </form>

    <form class="form-panel" @submit.prevent="updateNotice">
      <h2>更新公告</h2>
      <label>
        <span>公告 ID</span>
        <input v-model.trim="updateNoticeId" required />
      </label>
      <label>
        <span>标题</span>
        <input v-model.trim="updateForm.title" required />
      </label>
      <label>
        <span>内容</span>
        <textarea v-model.trim="updateForm.content" rows="5" required />
      </label>
      <label>
        <span>状态</span>
        <select v-model="updateForm.status">
          <option value="DRAFT">DRAFT</option>
          <option value="PUBLISHED">PUBLISHED</option>
          <option value="EXPIRED">EXPIRED</option>
          <option value="REVOKED">REVOKED</option>
        </select>
      </label>
      <label>
        <span>优先级</span>
        <input v-model.number="updateForm.priority" type="number" min="0" max="9" required />
      </label>
      <label>
        <span>过期时间</span>
        <input v-model="updateExpireTime" type="datetime-local" />
      </label>
      <button class="secondary-button" type="submit" :disabled="submitting">
        {{ submitting ? '提交中...' : '更新' }}
      </button>
    </form>

    <div v-if="message" class="state-panel success">{{ message }}</div>
    <div v-if="error" class="state-panel error">{{ error }}</div>

    <section class="view-stack">
      <div class="section-heading compact">
        <p class="eyebrow">公开列表</p>
        <h2>已发布公告</h2>
      </div>
      <button class="ghost-button fit" type="button" @click="loadNotices">刷新</button>
      <article v-for="notice in notices" :key="notice.noticeId" class="notice-item selectable" @click="fillUpdateForm(notice)">
        <div>
          <h3>{{ notice.title }}</h3>
          <p>{{ notice.content }}</p>
        </div>
        <dl class="notice-meta">
          <div>
            <dt>ID</dt>
            <dd>{{ notice.noticeId }}</dd>
          </div>
          <div>
            <dt>优先级</dt>
            <dd>{{ notice.priority }}</dd>
          </div>
        </dl>
      </article>
    </section>
  </section>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import { getApiError, http } from '../api/http';
import type { CreateNoticeRequest, SysNotice, UpdateNoticeRequest } from '../api/types';

const notices = ref<SysNotice[]>([]);
const submitting = ref(false);
const message = ref('');
const error = ref('');
const createExpireTime = ref('');
const updateExpireTime = ref('');
const updateNoticeId = ref('');
const createForm = reactive<CreateNoticeRequest>({
  title: '',
  content: '',
  priority: 1,
  expireTime: null
});
const updateForm = reactive<UpdateNoticeRequest>({
  title: '',
  content: '',
  priority: 1,
  status: 'PUBLISHED',
  expireTime: null
});

onMounted(loadNotices);

async function loadNotices() {
  error.value = '';
  try {
    const { data } = await http.get<SysNotice[]>('/api/notices');
    notices.value = data;
  } catch (requestError) {
    error.value = getApiError(requestError);
  }
}

async function createNotice() {
  message.value = '';
  error.value = '';

  if (!createForm.title.trim()) {
    error.value = '标题不能为空。';
    return;
  }
  if (!createForm.content.trim()) {
    error.value = '内容不能为空。';
    return;
  }
  if (createForm.title.length > 200) {
    error.value = '标题长度不能超过 200 个字符。';
    return;
  }
  if (createForm.priority < 0 || createForm.priority > 9) {
    error.value = '优先级必须在 0 到 9 之间。';
    return;
  }
  if (createExpireTime.value && new Date(createExpireTime.value).getTime() <= Date.now()) {
    error.value = '过期时间必须晚于当前时间。';
    return;
  }

  submitting.value = true;
  try {
    const payload = { ...createForm, expireTime: toIsoOrNull(createExpireTime.value) };
    const { data } = await http.post<SysNotice>('/api/admin/notices', payload);
    message.value = `公告已发布：${data.noticeId}`;
    updateNoticeId.value = data.noticeId;
    fillUpdateForm(data);
    await loadNotices();
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    submitting.value = false;
  }
}

async function updateNotice() {
  message.value = '';
  error.value = '';

  if (!updateNoticeId.value.trim()) {
    error.value = '公告 ID 不能为空。';
    return;
  }
  if (!updateForm.title.trim()) {
    error.value = '标题不能为空。';
    return;
  }
  if (!updateForm.content.trim()) {
    error.value = '内容不能为空。';
    return;
  }
  if (updateForm.title.length > 200) {
    error.value = '标题长度不能超过 200 个字符。';
    return;
  }
  if (updateForm.priority < 0 || updateForm.priority > 9) {
    error.value = '优先级必须在 0 到 9 之间。';
    return;
  }

  submitting.value = true;
  try {
    const payload = { ...updateForm, expireTime: toIsoOrNull(updateExpireTime.value) };
    const { data } = await http.put<SysNotice>(`/api/admin/notices/${encodeURIComponent(updateNoticeId.value)}`, payload);
    message.value = `公告已更新：${data.noticeId}`;
    await loadNotices();
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    submitting.value = false;
  }
}

function fillUpdateForm(notice: SysNotice) {
  updateNoticeId.value = notice.noticeId;
  updateForm.title = notice.title;
  updateForm.content = notice.content;
  updateForm.priority = notice.priority;
  updateForm.status = notice.status as UpdateNoticeRequest['status'];
  updateExpireTime.value = toLocalInputValue(notice.expireTime);
}

function toIsoOrNull(value: string) {
  return value ? new Date(value).toISOString() : null;
}

function toLocalInputValue(value: string | null) {
  if (!value) {
    return '';
  }

  const date = new Date(value);
  const offset = date.getTimezoneOffset() * 60000;
  return new Date(date.getTime() - offset).toISOString().slice(0, 16);
}
</script>
