# Big Two — Multiplayer Card Game

A real-time multiplayer **Big Two** (大老二) web app.

| Layer    | Stack                                    |
|----------|------------------------------------------|
| Backend  | ASP.NET Core 10 · SignalR                |
| Frontend | Vue 3 · TypeScript · Vite · Pinia        |
| Transport| WebSockets (SignalR)                     |
| Deploy   | Docker Compose · Azure Container Apps     |

---

## Running locally (two options)

### Option A — Docker (recommended, one command)

Requires Docker Desktop or Docker Engine + Compose v2.

```bash
cd "Big Two"
docker compose up --build
```

Open **http://localhost:3000** in two or more browser tabs.  
The backend API runs on **http://localhost:5000**.

---

### Option B — Without Docker (dev mode)

**Backend** (requires .NET 10 SDK):
```bash
cd "Big Two/backend/BigTwo.Api"
dotnet run
# Listening on http://localhost:5000
```

**Frontend** (requires Node 18+):
```bash
cd "Big Two/frontend"
npm install
npm run dev
# Open http://localhost:3000
```

The Vite dev server proxies `/gamehub` → `http://localhost:5000` automatically.

---

## How to play

1. Open the app and enter a nickname.
2. Click **Create Room** — note the 6-character room code.
3. Share the code with up to 3 friends; they click **Join Room**.
4. The host clicks **Start Game**.
5. The player holding **3♦** goes first and must play it.
6. Play combinations — singles, pairs, triples, or 5-card hands.
7. Beat the previous play or click **Pass**.
8. First to empty their hand wins!

### Card ranking

`3 < 4 < 5 < 6 < 7 < 8 < 9 < 10 < J < Q < K < A < 2`  
Suits (low → high): ♦ ♣ ♥ ♠

### 5-card hand hierarchy (lowest → highest)

Straight → Flush → Full House → Four-of-a-Kind + kicker → Straight Flush

---

## Solution design

### Architecture

```
┌────────────────────┐         WebSocket / HTTP          ┌────────────────────────┐
│                    │  ◄──────── /gamehub ─────────────► │                        │
│   Vue 3 SPA        │          (SignalR)                │   ASP.NET Core 10      │
│   Pinia store      │                                   │   SignalR Hub          │
│   Vue Router       │  nginx reverse-proxies /gamehub   │   In-memory state      │
│                    │  to the backend container         │                        │
└────────────────────┘                                   └────────────────────────┘
      Frontend                                                  Backend
  (Azure Container App)                                   (Azure Container App)
```

### State management

- **Backend**: All game state is held **in-memory** in `RoomService` (a thread-safe singleton). Two dictionaries map room codes → `Room` objects and connection IDs → room codes. A `System.Threading.Lock` protects all mutations.
- **Frontend**: A single **Pinia store** (`gameStore`) holds the reactive state. SignalR event handlers update the store; Vue components react automatically.
- **No database** — when the backend restarts, all rooms are lost. This is acceptable for a casual game.

### SignalR message flow

#### Client → Server (hub methods)

| Method | Parameters | Description |
|---|---|---|
| `CreateRoom` | `nickname` | Create a new room; caller becomes host |
| `JoinRoom` | `roomCode, nickname` | Join an existing room |
| `StartGame` | — | Host starts the game (deals cards, picks first player) |
| `PlayCards` | `cardIds[]` | Play a combo from your hand |
| `Pass` | — | Skip your turn (not allowed when leading) |

#### Server → Client (events)

| Event | Sent to | Payload |
|---|---|---|
| `RoomCreated` | Caller | `{ roomCode, playerId, isHost, players[] }` |
| `RoomJoined` | Caller | `{ roomCode, playerId, isHost, players[] }` |
| `LobbyUpdated` | Room group | `{ players[] }` |
| `GameStarted` | Each player individually | `{ hand[], currentPlayerId, players[] }` |
| `CardsPlayed` | Room group | `{ playerId, cards[], currentPlayerId, players[] }` |
| `HandUpdated` | Caller only | `{ hand[] }` |
| `PlayerPassed` | Room group | `{ playerId, currentPlayerId, newRound, tableCards[] }` |
| `PlayerDisconnected` | Room group | `{ playerId, nickname, players[] }` |
| `GameOver` | Room group | `{ winnerId, winnerNickname }` |
| `Error` | Caller | `string` message |

Each player receives their own hand privately via `GameStarted` and `HandUpdated`. Other players only see card counts.

### Game flow

```
 HomeView              LobbyView                GameView
┌──────────┐         ┌────────────┐         ┌─────────────────┐
│ nickname  │         │ room code  │         │ opponents (top) │
│ create /  │──lobby──│ player list│──play───│ table (center)  │
│ join room │         │ start btn  │         │ my hand (bottom)│
└──────────┘         └────────────┘         │ play/pass btns  │
     │                     │                │ round history   │
     │ createRoom()        │ startGame()    │ game over overlay│
     │ joinRoom()          │               └─────────────────┘
     ▼                     ▼                        │
  RoomCreated/          GameStarted              GameOver
  RoomJoined          (deals cards,           (winner shown,
  → navigate           finds 3♦ holder)        "Play Again"
    to /lobby           → navigate              → reset, go /)
                          to /game
```

1. **Create/Join** — A player enters a nickname and creates or joins a room. The server generates a 6-char room code and adds players to a SignalR group.
2. **Lobby** — Up to 4 players wait. The host sees a "Start Game" button (enabled at ≥ 2 players).
3. **Deal** — The server shuffles a 52-card deck, deals evenly round-robin, and picks the player holding 3♦ to go first.
4. **Play loop** — On each turn the current player either plays a valid combo that beats the table, or passes. When all other players pass consecutively, the last player who played starts a new round (free lead).
5. **Win** — The first player to empty their hand wins. The server emits `GameOver` and all clients show the result overlay.

### Combo validation

Cards are ranked `3 (lowest)` through `2 (highest)`, with suits breaking ties: ♦ < ♣ < ♥ < ♠. Valid combos:

| Combo | Count | Rule |
|---|---|---|
| Single | 1 | Any card |
| Pair | 2 | Same rank |
| Triple | 3 | Same rank |
| Straight | 5 | Consecutive ranks (any suits) |
| Flush | 5 | Same suit (not consecutive) |
| Full House | 5 | Three of a kind + pair (ranked by triple) |
| Four of a Kind | 5 | Four of a kind + any kicker (ranked by quad) |
| Straight Flush | 5 | Same suit + consecutive ranks |

A play must match the card count of the previous play. For 5-card combos, a higher combo type always beats a lower one (e.g., Full House beats Flush). Within the same type, the higher combo value wins.

### Navigation & routing

| Path | View | Guard |
|---|---|---|
| `/` | `HomeView` | — |
| `/lobby` | `LobbyView` | Redirects to `/` if status is `idle` |
| `/game` | `GameView` | Redirects to `/` if status is `idle` |

Transitions are driven by store status changes: `idle` → `lobby` → `playing` → `finished`.

---

## Project layout

```
Big Two/
├── backend/
│   └── BigTwo.Api/          # ASP.NET Core 10 project
│       ├── Hubs/GameHub.cs  # SignalR hub (all client↔server messages)
│       ├── Models/          # Card, Room, GameState, Player …
│       ├── Services/
│       │   ├── GameLogicService.cs  # Pure game rules & combo parsing
│       │   └── RoomService.cs       # Room/lobby state management
│       └── Program.cs
├── frontend/
│   └── src/
│       ├── views/           # HomeView, LobbyView, GameView
│       ├── components/      # PlayingCard
│       ├── stores/          # Pinia gameStore (all state + SignalR)
│       ├── types/game.ts    # Shared TypeScript interfaces
│       └── router/index.ts
└── docker-compose.yml
```

---

## Deploying to Azure

The app runs on **Azure Container Apps** (one for the backend, one for the frontend) with images stored in **Azure Container Registry**.  
Infrastructure is managed with **OpenTofu** (`infra/azure-tofu/`). The frontend nginx proxies `/gamehub` → backend over HTTPS, so no CORS configuration is needed.

### First deployment (run once)

Prerequisites: Azure CLI installed and logged in.

```powershell
.\infra\azure-bootstrap.ps1
```

The script creates a resource group (`bigtwo-state-rg`) with a Storage Account for OpenTofu remote state, and prints the GitHub secrets you need.

### Subsequent deployments (automatic)

Add the secrets to your repo under **Settings → Secrets and variables → Actions**, then push to `main`:

```bash
git push origin main
```

The workflow in `.github/workflows/azure-deploy.yml` runs:
1. **Plan** — shows the infrastructure diff
2. **Apply base** — creates/updates the resource group, ACR, and Container Apps environment
3. **Build** — builds and pushes Docker images to ACR
4. **Apply apps** — creates/updates the container apps with the new image tag

The live URL is printed in the workflow summary.

### Secrets reference

| Secret | Description |
|---|---|
| `AZURE_CLIENT_ID` | Service principal client ID |
| `AZURE_CLIENT_SECRET` | Service principal client secret |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_LOCATION` | Azure region (e.g. `westeurope`) |
| `AZURE_STATE_STORAGE_ACCOUNT` | Storage account name for OpenTofu state |

> **Note:** The backend runs with `min_replicas=1` (always on) because game state is held in memory.  
> To support multiple backend replicas you would add an Azure SignalR Service or Redis backplane.
