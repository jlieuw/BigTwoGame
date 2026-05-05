export interface Card {
  id: string
  rank: string      // "3"–"10", "J", "Q", "K", "A", "2"
  suit: string      // "♦", "♣", "♥", "♠"
  value: number     // 0–51, used for sorting
  isRed: boolean
  rankEnum: number  // 3–15
  suitEnum: number  // 0–3
}

export interface PlayerInfo {
  id: string
  nickname: string
  cardCount: number
  isConnected: boolean
}

export interface LobbyPlayer {
  id: string
  nickname: string
  isConnected: boolean
}

export type GameStatus = 'idle' | 'lobby' | 'playing' | 'finished'

/** One entry in the per-round play history. */
export interface RoundPlay {
  playerId: string
  nickname: string
  cards: Card[]
}
