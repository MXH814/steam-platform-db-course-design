<template>
  <div class="game-price" :class="{ compact }">
    <span v-if="isFree" class="free">免费入库</span>
    <template v-else>
      <span v-if="discountPercent > 0" class="discount">-{{ discountPercent }}%</span>
      <span class="price-stack">
        <span v-if="discountPercent > 0" class="base">¥ {{ money(basePrice) }}</span>
        <span class="final">¥ {{ money(finalPrice) }}</span>
      </span>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = withDefaults(
  defineProps<{
    basePrice: number;
    finalPrice: number;
    discountRate: number;
    compact?: boolean;
  }>(),
  {
    compact: false
  }
);

const isFree = computed(() => props.finalPrice <= 0);
const discountPercent = computed(() => Math.round((1 - props.discountRate) * 100));

function money(value: number): string {
  return value.toFixed(2);
}
</script>

<style scoped>
.game-price {
  display: inline-flex;
  min-height: 2.4rem;
  align-items: stretch;
  justify-content: flex-end;
  font-variant-numeric: tabular-nums;
}

.game-price.compact {
  min-height: 1.9rem;
}

.discount {
  display: inline-flex;
  align-items: center;
  padding: 0 0.52rem;
  color: var(--steam-discount-text);
  background: var(--steam-discount-bg);
  font-weight: 900;
}

.price-stack {
  display: grid;
  min-width: 5.6rem;
  align-content: center;
  justify-items: end;
  padding: 0.22rem 0.62rem;
  color: var(--steam-text);
  background: rgba(8, 13, 19, 0.82);
}

.base {
  color: #798796;
  font-size: 0.72rem;
  line-height: 1;
  text-decoration: line-through;
}

.final,
.free {
  font-weight: 900;
}

.free {
  display: inline-flex;
  align-items: center;
  padding: 0 0.74rem;
  color: #07120c;
  background: var(--steam-green);
}
</style>
