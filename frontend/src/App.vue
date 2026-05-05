<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useGameStore } from './stores/gameStore'
import { sounds } from './composables/useSound'

const store  = useGameStore()
const router = useRouter()
const muted  = ref(false)

const MUTE_KEY = 'bigtwo:muted'

// Route to the correct view as soon as a Reconnected event updates the store status
let didInitialReconnect = false
watch(() => store.status, (s) => {
  if (!didInitialReconnect) return
  if (s === 'lobby') router.replace('/lobby')
  else if (s === 'playing' || s === 'finished') router.replace('/game')
})

onMounted(async () => {
  // Restore mute preference
  try {
    muted.value = localStorage.getItem(MUTE_KEY) === '1'
    sounds.setMuted(muted.value)
  } catch { /* ignore */ }

  // Try to silently reconnect to a previous session
  try {
    didInitialReconnect = await store.tryReconnect()
  } catch {
    // Reconnect failed (server unreachable, room gone, etc.) — stay on home
  }
})

function toggleMute() {
  muted.value = !muted.value
  sounds.setMuted(muted.value)
  try { localStorage.setItem(MUTE_KEY, muted.value ? '1' : '0') } catch { /* ignore */ }
}
</script>

<template>
  <RouterView />
  <button
    class="mute-btn"
    :title="muted ? 'Unmute sounds' : 'Mute sounds'"
    @click="toggleMute"
  >
    {{ muted ? '🔇' : '🔊' }}
  </button>
</template>

<style scoped>
.mute-btn {
  position: fixed;
  bottom: 12px;
  right: 12px;
  width: 40px;
  height: 40px;
  border-radius: 50%;
  border: none;
  background: rgba(0, 0, 0, 0.4);
  color: #fff;
  font-size: 18px;
  cursor: pointer;
  z-index: 1000;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.15s;
}
.mute-btn:hover { background: rgba(0, 0, 0, 0.6); }
</style>
