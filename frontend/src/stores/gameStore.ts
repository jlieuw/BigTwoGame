import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import * as signalR from '@microsoft/signalr'
import type { Card, LobbyPlayer, PlayerInfo, GameStatus, RoundPlay } from '../types/game'
import { sounds } from '../composables/useSound'

const HUB_URL = import.meta.env.VITE_HUB_URL ?? '/gamehub'
const STORAGE_KEY = 'bigtwo:session'

interface StoredSession {
  roomCode: string
  sessionToken: string
}

function loadSession(): StoredSession | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? JSON.parse(raw) as StoredSession : null
  } catch {
    return null
  }
}

function saveSession(s: StoredSession) {
  try { localStorage.setItem(STORAGE_KEY, JSON.stringify(s)) } catch { /* ignore */ }
}

function clearSession() {
  try { localStorage.removeItem(STORAGE_KEY) } catch { /* ignore */ }
}

export const useGameStore = defineStore('game', () => {
  // Connection
  let connection: signalR.HubConnection | null = null

  // State
  const status          = ref<GameStatus>('idle')
  const roomCode        = ref<string | null>(null)
  const myId            = ref<string | null>(null)
  const sessionToken    = ref<string | null>(null)
  const isHost          = ref(false)
  const lobbyPlayers    = ref<LobbyPlayer[]>([])
  const players         = ref<PlayerInfo[]>([])
  const myHand          = ref<Card[]>([])
  const tableCards      = ref<Card[]>([])
  const currentPlayerId = ref<string | null>(null)
  const lastPlayerId    = ref<string | null>(null)
  const selectedCardIds = ref<Set<string>>(new Set())
  const winnerId        = ref<string | null>(null)
  const winnerNickname  = ref<string | null>(null)
  const errorMessage    = ref<string | null>(null)
  const connecting      = ref(false)
  /** History of plays in the current round. Cleared when a new round starts. */
  const roundHistory    = ref<RoundPlay[]>([])

  // Computed
  const isMyTurn = computed(() => currentPlayerId.value === myId.value)
  const me = computed(() => players.value.find(p => p.id === myId.value) ?? null)
  const selectedCards = computed(() =>
    myHand.value.filter(c => selectedCardIds.value.has(c.id))
  )

  // Connection helpers
  async function ensureConnected() {
    if (connection && connection.state === signalR.HubConnectionState.Connected) return

    connecting.value = true
    connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .build()

    registerHandlers()

    await connection.start()
    connecting.value = false
  }

  function registerHandlers() {
    if (!connection) return

    connection.on('RoomCreated', (data) => {
      roomCode.value     = data.roomCode
      myId.value         = data.playerId
      sessionToken.value = data.sessionToken
      isHost.value       = data.isHost
      lobbyPlayers.value = data.players
      status.value       = 'lobby'
      saveSession({ roomCode: data.roomCode, sessionToken: data.sessionToken })
    })

    connection.on('RoomJoined', (data) => {
      roomCode.value     = data.roomCode
      myId.value         = data.playerId
      sessionToken.value = data.sessionToken
      isHost.value       = data.isHost
      lobbyPlayers.value = data.players
      status.value       = 'lobby'
      saveSession({ roomCode: data.roomCode, sessionToken: data.sessionToken })
    })

    connection.on('Reconnected', (data) => {
      roomCode.value     = data.roomCode
      myId.value         = data.playerId
      isHost.value       = data.isHost
      lobbyPlayers.value = data.lobbyPlayers ?? []

      if (data.status === 'Playing' || data.status === 'Finished') {
        myHand.value          = data.hand ?? []
        tableCards.value      = data.tableCards ?? []
        currentPlayerId.value = data.currentPlayerId ?? null
        lastPlayerId.value    = data.lastPlayerId ?? null
        players.value         = data.players ?? []
        selectedCardIds.value = new Set()
        roundHistory.value    = []   // server doesn't track per-round history
        if (data.isOver) {
          winnerId.value       = data.winnerId
          winnerNickname.value = data.winnerNickname
          status.value         = 'finished'
        } else {
          status.value = 'playing'
        }
      } else {
        status.value = 'lobby'
      }
    })

    connection.on('PlayerReconnected', (data) => {
      lobbyPlayers.value = data.players
    })

    connection.on('LobbyUpdated', (data) => {
      // Sound only when someone *new* joins (not on disconnect updates)
      if (data.players.length > lobbyPlayers.value.length) sounds.playerJoined()
      lobbyPlayers.value = data.players
    })

    connection.on('GameStarted', (data) => {
      myHand.value          = data.hand
      currentPlayerId.value = data.currentPlayerId
      players.value         = data.players
      tableCards.value      = []
      roundHistory.value    = []
      status.value          = 'playing'
      selectedCardIds.value = new Set()
      sounds.gameStart()
      if (data.currentPlayerId === myId.value) sounds.yourTurn()
    })

    connection.on('CardsPlayed', (data) => {
      // Look up the nickname from the updated player list that comes with this event
      const who = (data.players as PlayerInfo[]).find((p: PlayerInfo) => p.id === data.playerId)
      roundHistory.value = [
        ...roundHistory.value,
        { playerId: data.playerId, nickname: who?.nickname ?? '?', cards: data.cards }
      ]
      tableCards.value      = data.cards
      currentPlayerId.value = data.currentPlayerId
      lastPlayerId.value    = data.playerId
      players.value         = data.players
      selectedCardIds.value = new Set()
      sounds.cardPlayed()
      if (data.currentPlayerId && data.currentPlayerId === myId.value) sounds.yourTurn()
    })

    connection.on('HandUpdated', (data) => {
      myHand.value          = data.hand
      selectedCardIds.value = new Set()
    })

    connection.on('PlayerPassed', (data) => {
      currentPlayerId.value = data.currentPlayerId
      if (data.newRound) {
        tableCards.value   = []
        lastPlayerId.value = null
        roundHistory.value = []   // new round — clear history
      }
      sounds.pass()
      if (data.currentPlayerId === myId.value) sounds.yourTurn()
    })

    connection.on('PlayerDisconnected', (data) => {
      lobbyPlayers.value = data.players
    })

    connection.on('GameOver', (data) => {
      winnerId.value       = data.winnerId
      winnerNickname.value = data.winnerNickname
      status.value         = 'finished'
      if (data.winnerId === myId.value) sounds.win()
      else sounds.lose()
      clearSession()   // game over — no point reconnecting
    })

    connection.on('Error', (msg: string) => {
      errorMessage.value = msg
      setTimeout(() => { errorMessage.value = null }, 3500)
      sounds.error()
    })
  }

  // Actions
  async function createRoom(nickname: string) {
    await ensureConnected()
    await connection!.invoke('CreateRoom', nickname)
  }

  async function joinRoom(code: string, nickname: string) {
    await ensureConnected()
    await connection!.invoke('JoinRoom', code, nickname)
  }

  /**
   * Attempts to reconnect using a stored session. Returns true if a session
   * existed and the reconnect call was made (the actual outcome arrives via
   * the `Reconnected` or `Error` event).
   */
  async function tryReconnect(): Promise<boolean> {
    const stored = loadSession()
    if (!stored) return false

    await ensureConnected()
    sessionToken.value = stored.sessionToken
    await connection!.invoke('Reconnect', stored.roomCode, stored.sessionToken)
    return true
  }

  async function startGame() {
    await connection!.invoke('StartGame')
  }

  async function playCards() {
    const ids = Array.from(selectedCardIds.value)
    if (!ids.length) return
    await connection!.invoke('PlayCards', ids)
  }

  async function pass() {
    await connection!.invoke('Pass')
  }

  function toggleCard(cardId: string) {
    const next = new Set(selectedCardIds.value)
    if (next.has(cardId)) next.delete(cardId)
    else next.add(cardId)
    selectedCardIds.value = next
  }

  function clearError() {
    errorMessage.value = null
  }

  function reset() {
    status.value          = 'idle'
    roomCode.value        = null
    myId.value            = null
    sessionToken.value    = null
    isHost.value          = false
    lobbyPlayers.value    = []
    players.value         = []
    myHand.value          = []
    tableCards.value      = []
    currentPlayerId.value = null
    lastPlayerId.value    = null
    selectedCardIds.value = new Set()
    winnerId.value        = null
    winnerNickname.value  = null
    errorMessage.value    = null
    roundHistory.value    = []
    clearSession()
  }

  return {
    // state
    status, roomCode, myId, sessionToken, isHost,
    lobbyPlayers, players, myHand, tableCards,
    currentPlayerId, lastPlayerId, selectedCardIds,
    winnerId, winnerNickname, errorMessage, connecting,
    roundHistory,
    // computed
    isMyTurn, me, selectedCards,
    // actions
    createRoom, joinRoom, tryReconnect,
    startGame, playCards, pass,
    toggleCard, clearError, reset
  }
})
