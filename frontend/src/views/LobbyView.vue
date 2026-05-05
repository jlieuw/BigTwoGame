<script setup lang="ts">
import { watch } from 'vue'
import { useRouter } from 'vue-router'
import { useGameStore } from '../stores/gameStore'

const router = useRouter()
const store  = useGameStore()

// Redirect if not in a room
if (store.status === 'idle') router.replace('/')

// When game starts, go to game view
watch(() => store.status, (s) => {
  if (s === 'playing') router.push('/game')
})

async function start() {
  await store.startGame()
}

function copyCode() {
  navigator.clipboard?.writeText(store.roomCode ?? '')
}
</script>

<template>
  <div class="lobby">
    <div class="lobby-card">
      <h2>Lobby</h2>

      <div class="room-code-block">
        <span class="label">Room Code</span>
        <div class="code-row">
          <span class="code">{{ store.roomCode }}</span>
          <button class="copy-btn" @click="copyCode" title="Copy to clipboard">📋</button>
        </div>
        <span class="hint">Share this code with friends</span>
      </div>

      <div class="players-list">
        <div
          v-for="p in store.lobbyPlayers"
          :key="p.id"
          class="player-row"
          :class="{ host: p.id === store.lobbyPlayers[0]?.id }"
        >
          <span class="avatar">{{ p.nickname.charAt(0).toUpperCase() }}</span>
          <span class="name">{{ p.nickname }}</span>
          <span v-if="p.id === store.lobbyPlayers[0]?.id" class="badge">Host</span>
          <span v-else class="badge ready">Ready</span>
        </div>

        <!-- Empty slots -->
        <div
          v-for="i in Math.max(0, 4 - store.lobbyPlayers.length)"
          :key="'empty-' + i"
          class="player-row empty"
        >
          <span class="avatar empty-av">?</span>
          <span class="name">Waiting…</span>
        </div>
      </div>

      <p class="players-count">
        {{ store.lobbyPlayers.length }} / 4 players
        <template v-if="store.lobbyPlayers.length < 2"> · need at least 2</template>
      </p>

      <button
        v-if="store.isHost"
        class="btn-start"
        :disabled="store.lobbyPlayers.length < 2"
        @click="start"
      >
        Start Game
      </button>
      <p v-else class="waiting-msg">Waiting for the host to start…</p>
    </div>
  </div>
</template>

<style scoped>
.lobby {
  min-height: 100dvh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: radial-gradient(ellipse at 60% 40%, #2d7a3a 0%, #1a4a22 100%);
  padding: 24px;
}

.lobby-card {
  background: rgba(0,0,0,0.5);
  border: 1px solid rgba(255,255,255,0.12);
  border-radius: 20px;
  padding: 32px 28px;
  width: 100%;
  max-width: 380px;
  backdrop-filter: blur(8px);
  display: flex;
  flex-direction: column;
  gap: 20px;
}

h2 {
  color: #fff;
  font-size: 28px;
  font-weight: 700;
  margin: 0;
  text-align: center;
}

.room-code-block {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
}

.label {
  color: rgba(255,255,255,0.5);
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 1px;
}

.code-row {
  display: flex;
  align-items: center;
  gap: 10px;
}

.code {
  font-size: 36px;
  font-weight: 800;
  letter-spacing: 6px;
  color: #ffe066;
  font-family: monospace;
}

.copy-btn {
  background: none;
  border: none;
  font-size: 18px;
  cursor: pointer;
  opacity: 0.7;
  transition: opacity 0.2s;
}
.copy-btn:hover { opacity: 1; }

.hint {
  color: rgba(255,255,255,0.35);
  font-size: 12px;
}

.players-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.player-row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 14px;
  background: rgba(255,255,255,0.07);
  border-radius: 10px;
  border: 1px solid rgba(255,255,255,0.08);
}
.player-row.empty {
  opacity: 0.4;
  border-style: dashed;
}

.avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: var(--btn);
  color: #1a1a1a;
  font-weight: 700;
  font-size: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.empty-av { background: rgba(255,255,255,0.15); color: rgba(255,255,255,0.4); }

.name {
  flex: 1;
  color: #fff;
  font-size: 16px;
}

.badge {
  font-size: 11px;
  padding: 3px 8px;
  border-radius: 20px;
  background: rgba(232, 160, 32, 0.25);
  color: var(--btn);
  font-weight: 600;
  border: 1px solid rgba(232,160,32,0.3);
}
.badge.ready {
  background: rgba(80,200,80,0.15);
  color: #80e080;
  border-color: rgba(80,200,80,0.25);
}

.players-count {
  text-align: center;
  color: rgba(255,255,255,0.45);
  font-size: 13px;
  margin: 0;
}

.btn-start {
  padding: 14px;
  border-radius: 12px;
  border: none;
  background: var(--btn);
  color: #1a1a1a;
  font-size: 17px;
  font-weight: 700;
  cursor: pointer;
  transition: background 0.2s, transform 0.1s;
}
.btn-start:hover:not(:disabled) { background: var(--btn-hover); }
.btn-start:active { transform: scale(0.98); }
.btn-start:disabled { opacity: 0.4; cursor: not-allowed; }

.waiting-msg {
  text-align: center;
  color: rgba(255,255,255,0.45);
  font-size: 14px;
  margin: 0;
}
</style>
