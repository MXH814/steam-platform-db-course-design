<template>
  <section class="view-stack">
    <div class="section-heading">
      <p class="eyebrow">系统公告</p>
      <h1>平台最新通知</h1>
    </div>

    <div v-if="loading" class="state-panel">正在加载公告...</div>
    <div v-else-if="error" class="state-panel error">{{ error }}</div>
    <div v-else-if="notices.length === 0" class="state-panel">暂无已发布公告。</div>
    <article v-for="notice in notices" v-else :key="notice.noticeId" class="notice-item">
      <div>
        <h2>{{ notice.title }}</h2>
        <p>{{ notice.content }}</p>
      </div>
      <dl class="notice-meta">
        <div>
          <dt>优先级</dt>
          <dd>{{ notice.priority }}</dd>
        </div>
        <div>
          <dt>发布时间</dt>
          <dd>{{ formatTime(notice.publishTime) }}</dd>
        </div>
      </dl>
    </article>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { getApiError, http } from '../api/http';
import type { SysNotice } from '../api/types';

const notices = ref<SysNotice[]>([]);
const loading = ref(true);
const error = ref('');

onMounted(loadNotices);

async function loadNotices() {
  loading.value = true;
  error.value = '';

  try {
    const { data } = await http.get<SysNotice[]>('/api/notices');
    notices.value = data;
  } catch (requestError) {
    error.value = getApiError(requestError);
  } finally {
    loading.value = false;
  }
}

function formatTime(value: string) {
  return new Date(value).toLocaleString();
}
</script>
