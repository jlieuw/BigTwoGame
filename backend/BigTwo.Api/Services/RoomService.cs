using BigTwo.Api.Models;

namespace BigTwo.Api.Services;

/// <summary>
/// Thread-safe (via lock) singleton that manages all active rooms.
/// Uses <see cref="System.Threading.Lock"/> (.NET 9+) for better performance
/// over the classic <c>object</c> pattern.
/// Note: <see cref="System.Threading.Lock"/> is non-reentrant, so methods that
/// already hold the lock call the private *Core helpers instead of the public
/// locking overloads.
/// </summary>
public class RoomService
{
    private readonly Dictionary<string, Room>   _rooms      = new();
    private readonly Dictionary<string, string> _connToRoom = new(); // connectionId → roomCode
    private readonly GameLogicService           _logic;
    private readonly Lock                       _lock       = new();  // System.Threading.Lock (.NET 9+)

    public RoomService(GameLogicService logic) => _logic = logic;

    // ──────────────────────────────────────────────────────────────────────────
    // Lobby
    // ──────────────────────────────────────────────────────────────────────────

    public Room CreateRoom(string connectionId, string nickname)
    {
        lock (_lock)
        {
            var code     = GenerateRoomCode();
            var playerId = Guid.NewGuid().ToString("N");
            var player   = new Player(playerId, connectionId, nickname);
            var room     = new Room(code, playerId);
            room.Players.Add(player);
            room.LastActivity         = DateTime.UtcNow;
            _rooms[code]              = room;
            _connToRoom[connectionId] = code;
            return room;
        }
    }

    public (Room? room, Player? player, string? error) JoinRoom(
        string connectionId, string roomCode, string nickname)
    {
        lock (_lock)
        {
            if (!_rooms.TryGetValue(roomCode, out var room))
                return (null, null, "Room not found.");
            if (room.Status != RoomStatus.Waiting)
                return (null, null, "The game has already started.");
            if (room.IsFull)
                return (null, null, "Room is full (max 4 players).");
            if (room.Players.Any(p => p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase)))
                return (null, null, "That nickname is already taken in this room.");

            var playerId = Guid.NewGuid().ToString("N");
            var player   = new Player(playerId, connectionId, nickname);
            room.Players.Add(player);
            room.LastActivity         = DateTime.UtcNow;
            _connToRoom[connectionId] = roomCode;
            return (room, player, null);
        }
    }

    /// <summary>Public lookup — acquires the lock.</summary>
    public (Room? room, Player? player) Lookup(string connectionId)
    {
        lock (_lock) { return LookupCore(connectionId); }
    }

    public Room? GetRoom(string roomCode)
    {
        lock (_lock) { return _rooms.GetValueOrDefault(roomCode); }
    }

    public void Disconnect(string connectionId)
    {
        lock (_lock)
        {
            if (!_connToRoom.TryGetValue(connectionId, out var code)) return;
            if (_rooms.TryGetValue(code, out var room))
            {
                var p = room.Players.FirstOrDefault(x => x.ConnectionId == connectionId);
                if (p is not null) p.IsConnected = false;

                // Remove the room if every player has disconnected
                if (room.Players.All(x => !x.IsConnected))
                {
                    _rooms.Remove(code);
                    // Clean up any remaining connection mappings for this room
                    foreach (var player in room.Players)
                    {
                        // connectionId is already being removed below, skip it
                        if (player.ConnectionId != connectionId)
                            _connToRoom.Remove(player.ConnectionId);
                    }
                }
            }
            _connToRoom.Remove(connectionId);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Game flow
    // ──────────────────────────────────────────────────────────────────────────

    public string? StartGame(string connectionId)
    {
        lock (_lock)
        {
            // Use LookupCore — we already hold _lock (System.Threading.Lock is non-reentrant)
            var (room, player) = LookupCore(connectionId);
            if (room is null || player is null) return "You are not in a room.";
            if (room.HostId != player.Id)       return "Only the host can start the game.";
            if (!room.CanStart)                  return "Need at least 2 players to start.";

            room.Status = RoomStatus.Playing;
            room.LastActivity = DateTime.UtcNow;
            _logic.DealCards(room.Players);

            room.GameState = new GameState
            {
                CurrentPlayerIndex = _logic.FindStartingPlayerIndex(room.Players),
                IsFirstTurn        = true
            };
            return null;
        }
    }

    public record PlayResult(string? Error, bool GameOver);

    public PlayResult PlayCards(string connectionId, List<string> cardIds)
    {
        lock (_lock)
        {
            var (room, player) = LookupCore(connectionId);
            if (room is null || player is null) return new("You are not in a room.", false);

            var state = room.GameState;
            if (state is null) return new("Game has not started.", false);

            var current = room.Players[state.CurrentPlayerIndex];
            if (current.Id != player.Id) return new("It is not your turn.", false);

            var combo = _logic.ParseCombo(cardIds, player.Hand);
            if (combo is null) return new("Invalid card combination.", false);

            // Check the 3♦ first-turn rule separately so the error message is accurate.
            if (state.IsFirstTurn &&
                !combo.Cards.Any(c => c.Rank == Rank.Three && c.Suit == Suit.Diamonds))
                return new("Your first play must include 3♦.", false);

            CardCombo? lastCombo = state.LastPlayedCards.Count > 0
                ? _logic.ParseCombo(state.LastPlayedCards)
                : null;

            // Pass mustIncludeThreeDiamonds=false — we already handled that check above.
            if (!_logic.IsValidPlay(combo, lastCombo, state.IsLeadTurn, mustIncludeThreeDiamonds: false))
                return new("That combination does not beat the current table.", false);

            // Remove played cards from hand
            foreach (var c in combo.Cards)
                player.Hand.RemoveAll(x => x.Id == c.Id);

            state.LastPlayedCards = combo.Cards;
            state.LastPlayerId    = player.Id;
            state.LastComboType   = combo.Type;
            state.PassCount       = 0;
            state.IsFirstTurn     = false;
            room.LastActivity     = DateTime.UtcNow;

            if (player.Hand.Count == 0)
            {
                state.IsOver   = true;
                state.WinnerId = player.Id;
                room.Status    = RoomStatus.Finished;
                return new(null, true);
            }

            AdvanceTurn(room, state);
            return new(null, false);
        }
    }

    public record PassResult(string? Error, bool NewRound);

    public PassResult Pass(string connectionId)
    {
        lock (_lock)
        {
            var (room, player) = LookupCore(connectionId);
            if (room is null || player is null) return new("You are not in a room.", false);

            var state = room.GameState;
            if (state is null)    return new("Game has not started.", false);
            if (state.IsLeadTurn) return new("You cannot pass when leading.", false);

            var current = room.Players[state.CurrentPlayerIndex];
            if (current.Id != player.Id) return new("It is not your turn.", false);

            state.PassCount++;
            room.LastActivity = DateTime.UtcNow;
            int activePlayers = room.Players.Count(p => p.IsConnected);

            // Everyone else passed → the last player who played leads the new round
            bool newRound = state.PassCount >= activePlayers - 1;

            if (newRound)
            {
                var leaderIdx            = room.Players.FindIndex(p => p.Id == state.LastPlayerId);
                state.CurrentPlayerIndex = leaderIdx >= 0 ? leaderIdx : 0;
                state.LastPlayedCards    = [];
                state.LastPlayerId       = null;
                state.LastComboType      = null;
                state.PassCount          = 0;
            }
            else
            {
                AdvanceTurn(room, state);
            }

            return new(null, newRound);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Removes rooms that have been inactive for longer than <paramref name="maxAge"/>.
    /// Called periodically by the background cleanup service.
    /// </summary>
    public int RemoveStaleRooms(TimeSpan maxAge)
    {
        lock (_lock)
        {
            var cutoff = DateTime.UtcNow - maxAge;
            var stale  = _rooms.Where(kv => kv.Value.LastActivity < cutoff)
                                .Select(kv => kv.Key)
                                .ToList();

            foreach (var code in stale)
            {
                if (_rooms.TryGetValue(code, out var room))
                {
                    foreach (var p in room.Players)
                        _connToRoom.Remove(p.ConnectionId);
                    _rooms.Remove(code);
                }
            }

            return stale.Count;
        }
    }

    /// <summary>
    /// Lock-free lookup — MUST only be called while the caller already holds <see cref="_lock"/>.
    /// This avoids re-entrancy on <see cref="System.Threading.Lock"/> which is non-reentrant.
    /// </summary>
    private (Room? room, Player? player) LookupCore(string connectionId)
    {
        if (!_connToRoom.TryGetValue(connectionId, out var code)) return (null, null);
        if (!_rooms.TryGetValue(code, out var room))               return (null, null);
        var player = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
        return (room, player);
    }

    private static void AdvanceTurn(Room room, GameState state)
    {
        int n    = room.Players.Count;
        int next = (state.CurrentPlayerIndex + 1) % n;
        // Skip disconnected players (max one full loop to avoid infinite spin)
        int attempts = 0;
        while (!room.Players[next].IsConnected && attempts++ < n)
            next = (next + 1) % n;
        state.CurrentPlayerIndex = next;
    }

    private string GenerateRoomCode()
    {
        // Random.GetItems (introduced in .NET 8) picks N items with replacement from a ReadOnlySpan.
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<char> buf = stackalloc char[6];
        string code;
        do
        {
            Random.Shared.GetItems<char>(alphabet.AsSpan(), buf);
            code = new string(buf);
        }
        while (_rooms.ContainsKey(code));
        return code;
    }
}
