# Big Two — Multiplayer Card Game

A real-time multiplayer **Big Two** (大老二) web app.

| Layer    | Stack                                    |
|----------|------------------------------------------|
| Backend  | ASP.NET Core 10 · SignalR                |
| Frontend | Vue 3 · TypeScript · Vite · Pinia        |
| Transport| WebSockets (SignalR)                     |
| Deploy   | Docker Compose                           |

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

**Backend** (requires .NET 8 SDK):
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

## Project layout

```
Big Two/
├── backend/
│   └── BigTwo.Api/          # ASP.NET Core 8 project
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

## Deploying to Scaleway

The app runs on **Scaleway Serverless Containers** (one for the backend, one for the frontend).  
The frontend nginx proxies `/gamehub` → backend over HTTPS, so no CORS configuration is needed and the browser only ever talks to one host.

### First deployment (run once)

Prerequisites: `scw` CLI, `docker`, `jq` installed; `scw init` completed with your API keys.

```bash
chmod +x infra/scaleway-setup.sh
./infra/scaleway-setup.sh
```

The script will:
1. Create a **Container Registry** namespace and push both images
2. Create a **Serverless Containers** namespace with backend (min 1 replica) and frontend (scales to zero)
3. Wire `BACKEND_URL` into the frontend container so nginx knows where to proxy SignalR traffic
4. Print the **7 GitHub secrets** you need to add to your repository

### Subsequent deployments (automatic)

Add the 7 secrets printed by the setup script to your repo under  
**Settings → Secrets and variables → Actions**, then push to `main`:

```bash
git push origin main
```

The workflow in `.github/workflows/scaleway-deploy.yml` builds both images with layer caching, pushes them to the registry, and rolls them out. The live URL is printed in the workflow summary.

### Secrets reference

| Secret | Description |
|---|---|
| `SCW_ACCESS_KEY` | IAM access key |
| `SCW_SECRET_KEY` | IAM secret key |
| `SCW_DEFAULT_REGION` | e.g. `fr-par` |
| `SCW_DEFAULT_PROJECT_ID` | Your Scaleway project ID |
| `SCW_REGISTRY_NAMESPACE` | Registry namespace name (e.g. `bigtwo`) |
| `SCW_BACKEND_CONTAINER_ID` | Serverless Container ID for the backend |
| `SCW_FRONTEND_CONTAINER_ID` | Serverless Container ID for the frontend |

> **Note:** The backend runs at `min-scale=1` (always on) because game state is held in memory.  
> To support multiple backend replicas you would add an Azure SignalR Service or Redis backplane.
