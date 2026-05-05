<script setup lang="ts">
import type { Card } from '../types/game'

const props = defineProps<{
  card: Card
  selected?: boolean
  faceDown?: boolean
  small?: boolean
}>()
</script>

<template>
  <div
    class="playing-card"
    :class="{
      red: !faceDown && card.isRed,
      selected,
      'face-down': faceDown,
      small
    }"
  >
    <template v-if="!faceDown">
      <div class="corner top-left">
        <div class="rank">{{ card.rank }}</div>
        <div class="suit">{{ card.suit }}</div>
      </div>
      <div class="center-suit">{{ card.suit }}</div>
      <div class="corner bottom-right">
        <div class="rank">{{ card.rank }}</div>
        <div class="suit">{{ card.suit }}</div>
      </div>
    </template>
    <template v-else>
      <div class="card-back-pattern"></div>
    </template>
  </div>
</template>

<style scoped>
.playing-card {
  --w: 72px;
  --h: 100px;
  --fs-rank: 14px;
  --fs-suit-corner: 12px;
  --fs-center: 32px;

  width: var(--w);
  height: var(--h);
  background: var(--card-bg);
  border-radius: 8px;
  box-shadow: var(--card-shadow);
  border: 1px solid #d0d0d0;
  position: relative;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  user-select: none;
  transition: transform 0.12s, box-shadow 0.12s;
  color: var(--card-black);
}

.playing-card.red { color: var(--card-red); }

.playing-card.selected {
  transform: translateY(-12px);
  box-shadow: 0 8px 20px rgba(0,0,0,0.45), 0 0 0 3px var(--card-selected);
  border-color: var(--card-selected);
  background: #fffef0;
}

.playing-card.small {
  --w: 50px;
  --h: 70px;
  --fs-rank: 11px;
  --fs-suit-corner: 10px;
  --fs-center: 22px;
  border-radius: 6px;
}

.corner {
  position: absolute;
  line-height: 1;
  font-weight: 700;
}
.top-left {
  top: 5px;
  left: 6px;
  text-align: left;
}
.bottom-right {
  bottom: 5px;
  right: 6px;
  text-align: right;
  transform: rotate(180deg);
}
.rank {
  font-size: var(--fs-rank);
  font-weight: 800;
}
.suit {
  font-size: var(--fs-suit-corner);
  line-height: 1;
}

.center-suit {
  font-size: var(--fs-center);
  line-height: 1;
  pointer-events: none;
}

/* Face-down back */
.face-down {
  cursor: default;
  background: #1a6b9a;
}
.card-back-pattern {
  width: calc(100% - 12px);
  height: calc(100% - 12px);
  border-radius: 4px;
  border: 2px solid rgba(255,255,255,0.3);
  background: repeating-linear-gradient(
    45deg,
    rgba(255,255,255,0.05),
    rgba(255,255,255,0.05) 4px,
    transparent 4px,
    transparent 10px
  );
}
</style>
