import { setActivePinia, createPinia } from 'pinia'
import { describe, it, expect, beforeEach, vi } from 'vitest'
import { useGameStore } from '../stores/gameStore'
import type { Card, PlayerInfo } from '../types/game'

// Mock @microsoft/signalr — tests never open a real WebSocket connection
vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: vi.fn(() => ({
    withUrl: vi.fn().mockReturnThis(),
    withAutomaticReconnect: vi.fn().mockReturnThis(),
    build: vi.fn(() => ({
      on: vi.fn(),
      off: vi.fn(),
      start: vi.fn().mockResolvedValue(undefined),
      invoke: vi.fn().mockResolvedValue(undefined),
      state: 'Disconnected',
    })),
  })),
  HubConnectionState: {
    Connected: 'Connected',
    Disconnected: 'Disconnected',
  },
  LogLevel: { Warning: 1 },
}))

// Mock the sound composable — AudioContext is unavailable in jsdom
vi.mock('../composables/useSound', () => ({
  sounds: {
    playerJoined: vi.fn(),
    gameStart: vi.fn(),
    yourTurn: vi.fn(),
    cardPlayed: vi.fn(),
    pass: vi.fn(),
    win: vi.fn(),
    lose: vi.fn(),
    error: vi.fn(),
  },
}))

function makeCard(id: string, value: number): Card {
  return { id, rank: '3', suit: '♦', value, isRed: true, rankEnum: 3, suitEnum: 0 }
}

function makePlayer(id: string, nickname: string): PlayerInfo {
  return { id, nickname, cardCount: 13, isConnected: true }
}

describe('gameStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  // ── isMyTurn ────────────────────────────────────────────────────────────

  describe('isMyTurn', () => {
    it('returns true when currentPlayerId matches myId', () => {
      const store = useGameStore()
      store.myId = 'player1'
      store.currentPlayerId = 'player1'
      expect(store.isMyTurn).toBe(true)
    })

    it('returns false when currentPlayerId does not match myId', () => {
      const store = useGameStore()
      store.myId = 'player1'
      store.currentPlayerId = 'player2'
      expect(store.isMyTurn).toBe(false)
    })

    it('returns false when myId is null', () => {
      const store = useGameStore()
      store.currentPlayerId = 'player1'
      // myId defaults to null
      expect(store.isMyTurn).toBe(false)
    })
  })

  // ── me ──────────────────────────────────────────────────────────────────

  describe('me', () => {
    it('returns the matching player when found', () => {
      const store = useGameStore()
      store.myId = 'player1'
      store.players = [makePlayer('player1', 'Alice'), makePlayer('player2', 'Bob')]
      expect(store.me?.nickname).toBe('Alice')
    })

    it('returns null when players list is empty', () => {
      const store = useGameStore()
      store.myId = 'player1'
      store.players = []
      expect(store.me).toBeNull()
    })

    it('returns null when myId is not in the players list', () => {
      const store = useGameStore()
      store.myId = 'ghost'
      store.players = [makePlayer('player1', 'Alice')]
      expect(store.me).toBeNull()
    })
  })

  // ── selectedCards ────────────────────────────────────────────────────────

  describe('selectedCards', () => {
    it('returns empty array when nothing is selected', () => {
      const store = useGameStore()
      store.myHand = [makeCard('card-1', 10), makeCard('card-2', 20)]
      expect(store.selectedCards).toHaveLength(0)
    })

    it('returns only selected cards', () => {
      const store = useGameStore()
      store.myHand = [makeCard('card-1', 10), makeCard('card-2', 20), makeCard('card-3', 30)]
      store.selectedCardIds = new Set(['card-1', 'card-3'])
      expect(store.selectedCards).toHaveLength(2)
      expect(store.selectedCards.map(c => c.id)).toContain('card-1')
      expect(store.selectedCards.map(c => c.id)).toContain('card-3')
    })
  })

  // ── toggleCard ───────────────────────────────────────────────────────────

  describe('toggleCard', () => {
    it('adds a card id when it is not selected', () => {
      const store = useGameStore()
      store.toggleCard('card-1')
      expect(store.selectedCardIds.has('card-1')).toBe(true)
    })

    it('removes a card id when it is already selected (toggle off)', () => {
      const store = useGameStore()
      store.toggleCard('card-1')
      store.toggleCard('card-1')
      expect(store.selectedCardIds.has('card-1')).toBe(false)
    })

    it('allows selecting multiple different cards', () => {
      const store = useGameStore()
      store.toggleCard('card-1')
      store.toggleCard('card-2')
      store.toggleCard('card-3')
      expect(store.selectedCardIds.size).toBe(3)
    })

    it('toggling one card does not affect others', () => {
      const store = useGameStore()
      store.toggleCard('card-1')
      store.toggleCard('card-2')
      store.toggleCard('card-1') // deselect card-1
      expect(store.selectedCardIds.has('card-1')).toBe(false)
      expect(store.selectedCardIds.has('card-2')).toBe(true)
    })
  })

  // ── clearError ────────────────────────────────────────────────────────────

  describe('clearError', () => {
    it('sets errorMessage to null', () => {
      const store = useGameStore()
      store.errorMessage = 'Something went wrong'
      store.clearError()
      expect(store.errorMessage).toBeNull()
    })

    it('is a no-op when there is no error', () => {
      const store = useGameStore()
      store.clearError()
      expect(store.errorMessage).toBeNull()
    })
  })

  // ── reset ─────────────────────────────────────────────────────────────────

  describe('reset', () => {
    it('clears roomCode and myId', () => {
      const store = useGameStore()
      store.roomCode = 'ABC123'
      store.myId = 'player1'
      store.reset()
      expect(store.roomCode).toBeNull()
      expect(store.myId).toBeNull()
    })

    it('clears session token and host flag', () => {
      const store = useGameStore()
      store.sessionToken = 'secret-token'
      store.isHost = true
      store.reset()
      expect(store.sessionToken).toBeNull()
      expect(store.isHost).toBe(false)
    })

    it('clears game state collections', () => {
      const store = useGameStore()
      store.players = [makePlayer('p1', 'Alice')]
      store.myHand = [makeCard('card-1', 0)]
      store.tableCards = [makeCard('card-2', 10)]
      store.roundHistory = [{ playerId: 'p1', nickname: 'Alice', cards: [] }]
      store.reset()
      expect(store.players).toHaveLength(0)
      expect(store.myHand).toHaveLength(0)
      expect(store.tableCards).toHaveLength(0)
      expect(store.roundHistory).toHaveLength(0)
    })

    it('clears current and last player ids', () => {
      const store = useGameStore()
      store.currentPlayerId = 'player1'
      store.lastPlayerId = 'player2'
      store.reset()
      expect(store.currentPlayerId).toBeNull()
      expect(store.lastPlayerId).toBeNull()
    })

    it('clears selected cards', () => {
      const store = useGameStore()
      store.selectedCardIds = new Set(['card-1', 'card-2'])
      store.reset()
      expect(store.selectedCardIds.size).toBe(0)
    })

    it('resets status to idle', () => {
      const store = useGameStore()
      store.status = 'playing'
      store.reset()
      expect(store.status).toBe('idle')
    })

    it('clears winner info', () => {
      const store = useGameStore()
      store.winnerId = 'player1'
      store.winnerNickname = 'Alice'
      store.reset()
      expect(store.winnerId).toBeNull()
      expect(store.winnerNickname).toBeNull()
    })
  })

  // ── initial state ─────────────────────────────────────────────────────────

  describe('initial state', () => {
    it('starts with idle status', () => {
      const store = useGameStore()
      expect(store.status).toBe('idle')
    })

    it('starts with no room code', () => {
      const store = useGameStore()
      expect(store.roomCode).toBeNull()
    })

    it('starts with empty hand', () => {
      const store = useGameStore()
      expect(store.myHand).toHaveLength(0)
    })

    it('starts with empty selected cards', () => {
      const store = useGameStore()
      expect(store.selectedCardIds.size).toBe(0)
    })
  })
})
