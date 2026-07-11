<template>
  <section class="info-panel">
    <h2>{{ title }}</h2>
    <div class="rows">
      <p v-for="row in rows" :key="row">{{ row }}</p>
    </div>
  </section>
</template>

<script setup lang="ts">
defineProps<{
  title: string;
  rows: string[];
}>();
</script>

<style scoped>
.info-panel {
  display: grid;
  gap: 0.65rem;
  border-radius: 4px;
  padding: 1rem;
  background: #080d13;
}

.info-panel h2,
.info-panel p {
  margin: 0;
}

.info-panel h2 {
  color: #d8e4ef;
  font-size: 1rem;
}

.rows {
  display: grid;
  gap: 0.35rem;
}

.rows p {
  color: #c7d5e0;
}

.rows p:last-child {
  color: var(--steam-blue);
}
</style>
