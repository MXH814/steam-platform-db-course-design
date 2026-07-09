<template>
  <span class="status-badge" :class="statusClass">{{ label }}</span>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
  status: string;
}>();

const normalized = computed(() => props.status.toUpperCase());
const label = computed(() => {
  if (normalized.value === 'ONLINE') return '已上架';
  if (normalized.value === 'OFFLINE') return '已下架';
  if (normalized.value === 'DRAFT') return '草稿';
  return props.status;
});
const statusClass = computed(() => ({
  online: normalized.value === 'ONLINE',
  offline: normalized.value === 'OFFLINE',
  draft: normalized.value === 'DRAFT'
}));
</script>

<style scoped>
.status-badge {
  display: inline-flex;
  width: fit-content;
  align-items: center;
  border-radius: 4px;
  padding: 0.18rem 0.42rem;
  color: var(--steam-muted);
  background: rgba(143, 152, 160, 0.14);
  font-size: 0.74rem;
  font-weight: 800;
}

.online {
  color: #c9f7aa;
  background: rgba(117, 197, 63, 0.18);
}

.offline {
  color: #ffc3c3;
  background: rgba(210, 76, 76, 0.18);
}

.draft {
  color: #d7e3f2;
  background: rgba(151, 170, 195, 0.16);
}
</style>
