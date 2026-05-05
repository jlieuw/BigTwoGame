<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useGameStore } from '../stores/gameStore'

const router = useRouter()
const store  = useGameStore()

const nickname  = ref('')
const roomInput = ref('')
const mode      = ref<'choose' | 'create' | 'join'>('choose')
const loading   = ref(false)
const err       = ref('')

async function create() {
  if (!nickname.value.trim()) { err.value = 'Please enter a nickname.'; return }
  loading.value = true
  err.value = ''
  try {
    await store.createRoom(nickname.value.trim())
    router.push('/lobby')
  } catch (e: any) {
    err.value = e?.message ?? 'Connection failed.'
  } finally {
    loading.value = false
  }
}

async function join() {
  if (!nickname.value.trim())  { err.value = 'Please enter a nickname.'; return }
  if (!roomInput.value.trim()) { err.value = 'Please enter a room code.'; return }
  loading.value = true
  err.value = ''
  try {
    await store.joinRoom(roomInput.value.trim(), nickname.value.trim())
    router.push('/lobby')
  } catch (e: any) {
    err.value = e?.message ?? 'Connection failed.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="home">
    <div class="card-pile">
      <span class="card-deco">🂡</span>
      <span class="card-deco">🂱</span>
      <span class="card-deco">🃁</span>
      <span class="card-deco">🃑</span>
    </div>

    <h1>Big Two</h1>
    <p class="subtitle">Multiplayer card game · up to 4 players</p>

    <div class="panel">
      <template v-if="mode === 'choose'">
        <button class="btn btn-primary" @click="mode = 'create'">Create Room</button>
        <button class="btn btn-secondary" @click="mode = 'join'">Join Room</button>
      </template>

      <template v-else>
        <button class="btn-back" @click="mode = 'choose'; err = ''">← Back</button>

        <label>Your Nickname
          <input v-model="nickname" maxlength="20" placeholder="e.g. Alice" @keyup.enter="mode === 'create' ? create() : join()" />
        </label>

        <label v-if="mode === 'join'">Room Code
          <input v-model="roomInput" maxlength="6" placeholder="e.g. AB3X7Y"
                 style="text-transform:uppercase"
                 @keyup.enter="join" />
        </label>

        <p v-if="err" class="error">{{ err }}</p>

        <button class="btn btn-primary" :disabled="loading" @click="mode === 'create' ? create() : join()">
          {{ loading ? 'Connecting…' : mode === 'create' ? 'Create Room' : 'Join Room' }}
        </button>
      </template>
    </div>

    <p class="rules-hint">
      13 cards each · 3♦ starts · singles, pairs, trips, and 5-card combos
    </p>
  </div>
</template>

<style scoped>
.home {
  min-height: 100dvh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: radial-gradient(ellipse at 60% 40%, #2d7a3a 0%, #1a4a22 100%);
  padding: 24px;
  gap: 16px;
}

.card-deco {
  font-size: 64px;
  opacity: 0.18;
  display: inline-block;
  margin: 0 4px;
  user-select: none;
}

h1 {
  font-size: 56px;
  font-weight: 800;
  color: #fff;
  letter-spacing: -1px;
  margin: 0;
  text-shadow: 0 2px 12px rgba(0,0,0,0.4);
}

.subtitle {
  color: rgba(255,255,255,0.65);
  font-size: 15px;
  margin: 0;
}

.panel {
  background: rgba(0,0,0,0.45);
  border: 1px solid rgba(255,255,255,0.12);
  border-radius: 16px;
  padding: 32px 28px;
  display: flex;
  flex-direction: column;
  gap: 14px;
  width: 100%;
  max-width: 340px;
  backdrop-filter: blur(8px);
}

label {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 13px;
  color: rgba(255,255,255,0.7);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

input {
  padding: 10px 14px;
  border-radius: 8px;
  border: 1px solid rgba(255,255,255,0.2);
  background: rgba(255,255,255,0.1);
  color: #fff;
  font-size: 16px;
  outline: none;
  transition: border-color 0.2s;
}
input:focus { border-color: rgba(255,255,255,0.5); }
input::placeholder { color: rgba(255,255,255,0.3); }

.btn {
  padding: 12px 20px;
  border-radius: 10px;
  border: none;
  font-size: 16px;
  font-weight: 600;
  cursor: pointer;
  transition: transform 0.1s, opacity 0.2s;
}
.btn:active { transform: scale(0.97); }
.btn:disabled { opacity: 0.5; cursor: not-allowed; }
.btn-primary   { background: var(--btn); color: #1a1a1a; }
.btn-primary:hover:not(:disabled) { background: var(--btn-hover); }
.btn-secondary { background: rgba(255,255,255,0.15); color: #fff; }
.btn-secondary:hover { background: rgba(255,255,255,0.25); }

.btn-back {
  background: none;
  border: none;
  color: rgba(255,255,255,0.6);
  font-size: 14px;
  cursor: pointer;
  padding: 0;
  text-align: left;
}
.btn-back:hover { color: #fff; }

.error {
  color: #ff8080;
  font-size: 13px;
  margin: 0;
}

.rules-hint {
  color: rgba(255,255,255,0.35);
  font-size: 12px;
  margin: 0;
}
</style>
