# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added
- **Session-based reconnect** — players can refresh the page and rejoin their room with full state restored (hand, turn, table)
- **Sound effects** for card play, pass, your turn, game start/over, and player join (synthesized via Web Audio API; no audio assets required)
- Mute toggle button (bottom-right) with preference persisted to localStorage
- Disconnected opponents are visually greyed out with a ⚠ badge in the game view
- `CHANGELOG.md`

### Fixed
- Refreshing as the only connected player no longer destroys the room before reconnect can complete — immediate cleanup on disconnect was removed; rooms are now only pruned by the background cleanup service after 30 minutes of inactivity
- `LobbyView` and `GameView` no longer redirect to home during an in-flight reconnect (race condition where `status === 'idle'` briefly held during page load)
- In-game player list now carries `isConnected`, so disconnects are visible during gameplay (not just in the lobby)

## [1.1.0] — 2026-05-05

### Added
- **Azure deployment pipeline** with OpenTofu IaC (`infra/azure-tofu/`) and a 4-stage GitHub Actions workflow (plan → apply-base → build → apply-apps)
- **Room cleanup**: rooms are removed immediately when all players disconnect, plus a background service that prunes stale rooms after 30 minutes of inactivity
- Solution-design section in the README (architecture, SignalR message flow, game flow, combo rules)
- HTML metadata: title, description, Open Graph tags, theme-color, custom card-themed favicon

### Changed
- Frontend `nginx.conf` — increased `large_client_header_buffers` to handle Azure Container Apps ingress headers
- Frontend `nginx.conf` — `proxy_set_header Host $proxy_host` so the backend's ingress accepts proxied requests
- OpenTofu uses stable `ingress[0].fqdn` instead of `latest_revision_fqdn` to avoid "inconsistent final plan" errors on apply
- README updated for .NET 10, Azure deployment, and the current project layout
- `package.json` name → `bigtwo-frontend`, version → `1.0.0`

### Removed
- Vite scaffolding leftovers: `HelloWorld.vue`, `vite.svg`, `vue.svg`, `hero.png`
- Stale build outputs (`dist2/`, `dist3/`, `dist4/`)
- Scaleway deployment workflow (`scaleway-deploy.yml`) — replaced by Azure

## [1.0.0] — Initial release

### Added
- Real-time multiplayer Big Two with SignalR + WebSockets
- ASP.NET Core 10 backend with in-memory room/game state
- Vue 3 + TypeScript + Pinia frontend
- Docker Compose for local development
- Full game logic: 1/2/3-card combos, all 5-card hands, 3♦ first-turn rule, pass-and-new-round
