<template>
  <section class="page-state" :class="kind" role="status">
    <strong>{{ title }}</strong>
    <p v-if="message">{{ message }}</p>
    <button v-if="actionLabel" type="button" class="ghost-button" @click="$emit('action')">{{ actionLabel }}</button>
  </section>
</template>

<script setup lang="ts">
withDefaults(
  defineProps<{
    kind?: 'loading' | 'error' | 'empty';
    title: string;
    message?: string;
    actionLabel?: string;
  }>(),
  {
    kind: 'empty',
    message: '',
    actionLabel: ''
  }
);

defineEmits<{
  action: [];
}>();
</script>

<style scoped>
.page-state {
  display: grid;
  gap: 0.55rem;
  border: 1px solid var(--steam-border);
  border-radius: 6px;
  padding: 1.1rem;
  color: var(--steam-text);
  background: rgba(22, 32, 45, 0.88);
}

.page-state p {
  margin: 0;
  color: var(--steam-muted);
  text-wrap: pretty;
}

.page-state.error {
  border-color: rgba(255, 135, 135, 0.35);
}
</style>
