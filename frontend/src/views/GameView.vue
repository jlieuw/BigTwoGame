<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useGameStore } from '../stores/gameStore'
import PlayingCard from '../components/PlayingCard.vue'
import type { PlayerInfo } from '../types/game'

const router = useRouter()
const store  = useGameStore()

if (store.status === 'idle') router.replace('/')

const opponents = computed<PlayerInfo[]>(() =>
  store.players.filter(p => p.id !== store.myId)
)

const myInfo = computed<PlayerInfo | undefined>(() =>
  store.players.find(p => p.id === store.myId)
)

function isCurrentPlayer(id: string) {
  return store.currentPlayerId === id
}

function opponentLabel(p: PlayerInfo) {
  return isCurrentPlayer(p.id) ? `▶ ${p.nickname}` : p.nickname
}

async function play() { await store.playCards() }
async function pass() { await store.pass() }

function opponentPosition(index: number, total: number): string {
  if (total === 1) return 'top'
  if (total === 2) return index === 0 ? 'top-left' : 'top-right'
  return ['top-left', 'top', 'top-right'][index]
}

function playAgain() {
  store.reset()
  router.replace('/')
}
</script>

<template>
  <div class="game-table">

    <!-- Error toast -->
    <Transition name="toast">
      <div v-if="store.errorMessage" class="toast error">{{ store.errorMessage }}</div>
    </Transition>

    <!-- Opponents -->
    <div class="opponents-area">
      <div
        v-for="(p, i) in opponents"
        :key="p.id"
        class="opponent"
        :class="[opponentPosition(i, opponents.length), { active: isCurrentPlayer(p.id) }]"
      >
        <div class="opponent-name">{{ opponentLabel(p) }}</div>
        <div class="opponent-cards">
          <div
            v-for="n in p.cardCount"
            :key="n"
            class="face-down-card"
            :style="{ transform: `rotate(${(n - Math.ceil(p.cardCount/2)) * 4}deg)` }"
          ></div>
        </div>
        <div class="card-count">{{ p.cardCount }} cards</div>
      </div>
    </div>

    <!-- Table / board area -->
    <div class="table-area">

      <div class="table-surface">
        <template v-if="store.tableCards.length">
          <div class="table-label">
            {{ store.players.find(p => p.id === store.lastPlayerId)?.nickname ?? '' }} played:
          </div>
          <div class="table-cards">
            <PlayingCard
              v-for="c in store.tableCards"
              :key="c.id"
              :card="c"
              :small="false"
            />
          </div>
        </template>
        <template v-else>
          <div class="table-empty">
            <span v-if="store.isMyTurn">Your lead — play any combination</span>
            <span v-else>Waiting for {{ store.players.find(p => p.id === store.currentPlayerId)?.nickname }}…</span>
          </div>
        </template>
      </div>

      <!-- Round history — shows every play this round; clears when a new round starts -->
      <Transition name="history-fade">
        <div v-if="store.roundHistory.length" class="round-history">
          <div class="history-label">This round</div>
          <div class="history-scroll">
            <div
              v-for="(play, i) in store.roundHistory"
              :key="i"
              class="history-row"
              :class="{ 'is-me': play.playerId === store.myId }"
            >
              <span class="history-who">{{ play.playerId === store.myId ? 'You' : play.nickname }}</span>
              <div class="history-cards">
                <PlayingCard
                  v-for="c in play.cards"
                  :key="c.id"
                  :card="c"
                  :small="true"
                />
              </div>
            </div>
          </div>
        </div>
      </Transition>

      <!-- Turn indicator -->
      <div class="turn-info" :class="{ 'my-turn': store.isMyTurn }">
        <template v-if="store.isMyTurn">Your turn</template>
        <template v-else>
          Waiting for {{ store.players.find(p => p.id === store.currentPlayerId)?.nickname ?? '…' }}
        </template>
      </div>

    </div>

    <!-- My hand -->
    <div class="my-area">
      <div class="my-info">
        <span class="my-name">{{ myInfo?.nickname ?? 'Me' }}</span>
        <span class="my-count">{{ store.myHand.length }} cards</span>
      </div>

      <div class="my-hand">
        <PlayingCard
          v-for="c in store.myHand"
          :key="c.id"
          :card="c"
          :selected="store.selectedCardIds.has(c.id)"
          @click="store.isMyTurn && store.toggleCard(c.id)"
        />
      </div>

      <div class="action-bar" v-if="store.isMyTurn">
        <button class="btn-pass" @click="pass" :disabled="!store.tableCards.length">Pass</button>
        <button
          class="btn-play"
          @click="play"
          :disabled="store.selectedCardIds.size === 0"
        >
          Play{{ store.selectedCardIds.size > 0 ? ` (${store.selectedCardIds.size})` : '' }}
        </button>
      </div>
      <div class="action-bar" v-else>
        <span class="waiting-label">Waiting…</span>
      </div>
    </div>

    <!-- Game over overlay -->
    <Transition name="overlay">
      <div v-if="store.status === 'finished'" class="overlay">
        <div class="result-card">
          <div class="trophy">🏆</div>
          <h2>{{ store.winnerNickname }}</h2>
          <p>wins the round!</p>
          <button class="btn-play" @click="playAgain">Play Again</button>
        </div>
      </div>
    </Transition>

  </div>
</template>

<style scoped>
.game-table {
  width: 100%;
  min-height: 100dvh;
  background: radial-gradient(ellipse at 50% 60%, #2d7a3a 0%, #1a4a22 100%);
  display: grid;
  grid-template-rows: auto 1fr auto;
  padding: 12px;
  box-sizing: border-box;
  position: relative;
  overflow: hidden;
  gap: 12px;
}

/* Toast */
.toast {
  position: fixed;
  top: 16px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 100;
  padding: 10px 20px;
  border-radius: 10px;
  font-size: 14px;
  font-weight: 600;
  pointer-events: none;
}
.toast.error {
  background: rgba(200, 40, 40, 0.9);
  color: #fff;
  border: 1px solid rgba(255,100,100,0.4);
}
.toast-enter-active, .toast-leave-active { transition: all 0.3s; }
.toast-enter-from, .toast-leave-to { opacity: 0; transform: translateX(-50%) translateY(-10px); }

/* Opponents */
.opponents-area {
  display: flex;
  justify-content: space-around;
  align-items: flex-start;
  padding: 4px 0;
  gap: 12px;
}
.opponent {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  padding: 8px 12px;
  border-radius: 12px;
  border: 1px solid transparent;
  transition: border-color 0.3s;
  min-width: 80px;
}
.opponent.active {
  border-color: #ffe066;
  background: rgba(255, 224, 102, 0.08);
  box-shadow: 0 0 16px rgba(255, 224, 102, 0.2);
}
.opponent-name {
  color: #fff;
  font-size: 13px;
  font-weight: 600;
  text-shadow: 0 1px 4px rgba(0,0,0,0.5);
}
.opponent-cards {
  display: flex;
  position: relative;
  height: 50px;
  width: 80px;
  justify-content: center;
}
.face-down-card {
  position: absolute;
  width: 36px;
  height: 50px;
  background: #1a6b9a;
  border-radius: 5px;
  border: 1px solid rgba(255,255,255,0.2);
  box-shadow: 0 2px 6px rgba(0,0,0,0.4);
  transform-origin: bottom center;
}
.card-count {
  color: rgba(255,255,255,0.55);
  font-size: 11px;
}

/* Table area */
.table-area {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  justify-content: center;
}
.table-surface {
  background: rgba(0,0,0,0.25);
  border: 2px dashed rgba(255,255,255,0.15);
  border-radius: 20px;
  width: 100%;
  max-width: 520px;
  min-height: 110px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 16px;
  box-sizing: border-box;
}
.table-label {
  color: rgba(255,255,255,0.55);
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}
.table-cards {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  justify-content: center;
}
.table-empty {
  color: rgba(255,255,255,0.35);
  font-size: 14px;
  text-align: center;
}

/* Round history */
.round-history {
  width: 100%;
  max-width: 520px;
  background: rgba(0,0,0,0.2);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 14px;
  padding: 10px 14px;
  box-sizing: border-box;
}
.history-label {
  color: rgba(255,255,255,0.4);
  font-size: 10px;
  text-transform: uppercase;
  letter-spacing: 1px;
  margin-bottom: 8px;
}
.history-scroll {
  display: flex;
  flex-direction: column;
  gap: 6px;
  max-height: 160px;
  overflow-y: auto;
  scrollbar-width: thin;
  scrollbar-color: rgba(255,255,255,0.15) transparent;
}
.history-row {
  display: flex;
  align-items: center;
  gap: 10px;
}
.history-who {
  font-size: 12px;
  font-weight: 600;
  color: rgba(255,255,255,0.55);
  min-width: 52px;
  flex-shrink: 0;
  text-align: right;
}
.history-row.is-me .history-who { color: #ffe066; }
.history-cards {
  display: flex;
  gap: 4px;
  flex-wrap: wrap;
}
.history-fade-enter-active, .history-fade-leave-active { transition: opacity 0.3s; }
.history-fade-enter-from, .history-fade-leave-to { opacity: 0; }

/* Turn indicator */
.turn-info {
  padding: 8px 20px;
  border-radius: 20px;
  background: rgba(0,0,0,0.35);
  color: rgba(255,255,255,0.6);
  font-size: 14px;
  font-weight: 500;
  border: 1px solid rgba(255,255,255,0.1);
}
.turn-info.my-turn {
  color: #ffe066;
  border-color: rgba(255,224,102,0.3);
  background: rgba(255,224,102,0.1);
}

/* My area */
.my-area {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  padding-bottom: 8px;
}
.my-info {
  display: flex;
  align-items: center;
  gap: 10px;
}
.my-name {
  color: #fff;
  font-weight: 700;
  font-size: 14px;
}
.my-count {
  color: rgba(255,255,255,0.5);
  font-size: 12px;
}
.my-hand {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 6px;
  padding: 8px;
  min-height: 116px;
}

/* Action buttons */
.action-bar {
  display: flex;
  gap: 12px;
  align-items: center;
}
.btn-play, .btn-pass {
  padding: 12px 28px;
  border-radius: 10px;
  border: none;
  font-size: 15px;
  font-weight: 700;
  cursor: pointer;
  transition: transform 0.1s, opacity 0.2s;
}
.btn-play {
  background: var(--btn);
  color: #1a1a1a;
}
.btn-play:hover:not(:disabled) { background: var(--btn-hover); }
.btn-pass {
  background: rgba(255,255,255,0.15);
  color: rgba(255,255,255,0.8);
}
.btn-pass:hover:not(:disabled) { background: rgba(255,255,255,0.25); }
.btn-play:active, .btn-pass:active { transform: scale(0.96); }
.btn-play:disabled, .btn-pass:disabled { opacity: 0.35; cursor: not-allowed; }
.waiting-label {
  color: rgba(255,255,255,0.35);
  font-size: 14px;
}

/* Game over overlay */
.overlay {
  position: fixed;
  inset: 0;
  background: rgba(0,0,0,0.7);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 50;
  backdrop-filter: blur(4px);
}
.result-card {
  background: rgba(20, 60, 28, 0.95);
  border: 1px solid rgba(255,255,255,0.15);
  border-radius: 24px;
  padding: 48px 40px;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  box-shadow: 0 20px 60px rgba(0,0,0,0.6);
}
.trophy { font-size: 64px; }
.result-card h2 { color: #ffe066; font-size: 32px; font-weight: 800; margin: 0; }
.result-card p  { color: rgba(255,255,255,0.6); margin: 0; }
.overlay-enter-active, .overlay-leave-active { transition: all 0.35s; }
.overlay-enter-from, .overlay-leave-to { opacity: 0; transform: scale(0.9); }
</style>
