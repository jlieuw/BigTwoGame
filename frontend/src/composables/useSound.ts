/**
 * Synthesized sound effects via the Web Audio API.
 *
 * No audio assets needed — every sound is generated from oscillators on the fly.
 * The AudioContext is created lazily on the first sound to comply with browser
 * autoplay policies (which require a user gesture before audio can play).
 */

let ctx: AudioContext | null = null
let muted = false

function getCtx(): AudioContext | null {
  if (typeof window === 'undefined') return null
  if (!ctx) {
    try {
      const Ctor = window.AudioContext || (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext
      if (!Ctor) return null
      ctx = new Ctor()
    } catch {
      return null
    }
  }
  // Some browsers start the context in 'suspended' state until a user gesture.
  if (ctx.state === 'suspended') ctx.resume().catch(() => { /* ignore */ })
  return ctx
}

interface ToneOptions {
  freq: number
  duration: number       // seconds
  type?: OscillatorType  // default 'sine'
  volume?: number        // 0..1, default 0.18
  attack?: number        // seconds, default 0.005
  release?: number       // seconds, default 0.05
  startOffset?: number   // seconds from now, default 0
}

function tone(opts: ToneOptions) {
  const audio = getCtx()
  if (!audio || muted) return

  const start = audio.currentTime + (opts.startOffset ?? 0)
  const end   = start + opts.duration
  const vol   = opts.volume ?? 0.18
  const att   = opts.attack ?? 0.005
  const rel   = opts.release ?? 0.05

  const osc = audio.createOscillator()
  osc.type = opts.type ?? 'sine'
  osc.frequency.value = opts.freq

  const gain = audio.createGain()
  // Quick attack, hold, exponential release for a soft tail
  gain.gain.setValueAtTime(0, start)
  gain.gain.linearRampToValueAtTime(vol, start + att)
  gain.gain.setValueAtTime(vol, end - rel)
  gain.gain.exponentialRampToValueAtTime(0.0001, end)

  osc.connect(gain).connect(audio.destination)
  osc.start(start)
  osc.stop(end + 0.02)
}

export const sounds = {
  /** Soft "tap" when a card is played */
  cardPlayed() {
    tone({ freq: 880, duration: 0.08, type: 'triangle', volume: 0.15 })
  },

  /** Lower neutral blip when someone passes */
  pass() {
    tone({ freq: 330, duration: 0.12, type: 'sine', volume: 0.12 })
  },

  /** Two-note ding when it's your turn */
  yourTurn() {
    tone({ freq: 660, duration: 0.10, type: 'sine', volume: 0.18 })
    tone({ freq: 988, duration: 0.14, type: 'sine', volume: 0.18, startOffset: 0.10 })
  },

  /** Rising arpeggio when the game starts */
  gameStart() {
    const notes = [523, 659, 784]   // C5 E5 G5
    notes.forEach((f, i) => tone({
      freq: f, duration: 0.14, type: 'triangle', volume: 0.16, startOffset: i * 0.10
    }))
  },

  /** Triumphant chord when you win */
  win() {
    const chord = [523, 659, 784, 1047]   // C major
    chord.forEach(f => tone({ freq: f, duration: 0.55, type: 'triangle', volume: 0.14 }))
  },

  /** Sad descending tone when someone else wins */
  lose() {
    tone({ freq: 392, duration: 0.18, type: 'sine', volume: 0.14 })
    tone({ freq: 311, duration: 0.30, type: 'sine', volume: 0.14, startOffset: 0.18 })
  },

  /** Subtle notification when a player joins the lobby */
  playerJoined() {
    tone({ freq: 880, duration: 0.10, type: 'sine', volume: 0.12 })
  },

  /** Buzz on errors */
  error() {
    tone({ freq: 220, duration: 0.18, type: 'square', volume: 0.10 })
  },

  setMuted(value: boolean) { muted = value },
  isMuted() { return muted },
}
