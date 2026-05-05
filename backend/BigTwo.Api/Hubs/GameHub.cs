using BigTwo.Api.Models;
using BigTwo.Api.Services;
using Microsoft.AspNetCore.SignalR;

namespace BigTwo.Api.Hubs;

public class GameHub : Hub
{
    private readonly RoomService _rooms;

    public GameHub(RoomService rooms) => _rooms = rooms;

    // ──────────────────────────────────────────────────────────────────────────
    // Connection lifecycle
    // ──────────────────────────────────────────────────────────────────────────

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var (room, player) = _rooms.Lookup(Context.ConnectionId);
        _rooms.Disconnect(Context.ConnectionId);

        if (room is not null && player is not null)
        {
            await Clients.Group(room.Code).SendAsync("PlayerDisconnected", new
            {
                playerId = player.Id,
                nickname = player.Nickname,
                players  = room.Players.Select(PlayerDto)
            });
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Lobby
    // ──────────────────────────────────────────────────────────────────────────

    public async Task CreateRoom(string nickname)
    {
        nickname = nickname.Trim();
        if (string.IsNullOrEmpty(nickname))
        {
            await Error("Nickname cannot be empty.");
            return;
        }

        var room   = _rooms.CreateRoom(Context.ConnectionId, nickname);
        var player = room.Players[0];

        await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);

        await Clients.Caller.SendAsync("RoomCreated", new
        {
            roomCode = room.Code,
            playerId = player.Id,
            isHost   = true,
            players  = room.Players.Select(PlayerDto)
        });
    }

    public async Task JoinRoom(string roomCode, string nickname)
    {
        nickname = nickname.Trim();
        roomCode = roomCode.Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(nickname))
        {
            await Error("Nickname cannot be empty.");
            return;
        }

        var (room, player, error) = _rooms.JoinRoom(Context.ConnectionId, roomCode, nickname);
        if (error is not null) { await Error(error); return; }

        await Groups.AddToGroupAsync(Context.ConnectionId, room!.Code);

        // Tell the joiner their own info
        await Clients.Caller.SendAsync("RoomJoined", new
        {
            roomCode = room.Code,
            playerId = player!.Id,
            isHost   = false,
            players  = room.Players.Select(PlayerDto)
        });

        // Notify everyone in the room (including the joiner) about the updated player list
        await Clients.Group(room.Code).SendAsync("LobbyUpdated", new
        {
            players = room.Players.Select(PlayerDto)
        });
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Game start
    // ──────────────────────────────────────────────────────────────────────────

    public async Task StartGame()
    {
        var error = _rooms.StartGame(Context.ConnectionId);
        if (error is not null) { await Error(error); return; }

        var (room, _) = _rooms.Lookup(Context.ConnectionId);
        if (room?.GameState is null) return;

        var state = room.GameState;

        // Send each player their private hand + shared game state
        foreach (var p in room.Players)
        {
            await Clients.Client(p.ConnectionId).SendAsync("GameStarted", new
            {
                hand            = p.Hand.Select(CardDto),
                currentPlayerId = room.Players[state.CurrentPlayerIndex].Id,
                players         = room.Players.Select(PlayerInfoDto)
            });
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Gameplay
    // ──────────────────────────────────────────────────────────────────────────

    public async Task PlayCards(List<string> cardIds)
    {
        var (room, player) = _rooms.Lookup(Context.ConnectionId);
        if (room is null || player is null) return;

        var result = _rooms.PlayCards(Context.ConnectionId, cardIds);
        if (result.Error is not null) { await Error(result.Error); return; }

        var state = room.GameState!;

        // Broadcast the played cards
        await Clients.Group(room.Code).SendAsync("CardsPlayed", new
        {
            playerId        = player.Id,
            cards           = state.LastPlayedCards.Select(CardDto),
            currentPlayerId = result.GameOver ? null : room.Players[state.CurrentPlayerIndex].Id,
            players         = room.Players.Select(PlayerInfoDto)
        });

        // Send updated hand only to the player who just played
        await Clients.Caller.SendAsync("HandUpdated", new
        {
            hand = player.Hand.Select(CardDto)
        });

        if (result.GameOver)
        {
            await Clients.Group(room.Code).SendAsync("GameOver", new
            {
                winnerId       = state.WinnerId,
                winnerNickname = room.Players.First(p => p.Id == state.WinnerId).Nickname
            });
        }
    }

    public async Task Pass()
    {
        var (room, player) = _rooms.Lookup(Context.ConnectionId);
        if (room is null || player is null) return;

        var result = _rooms.Pass(Context.ConnectionId);
        if (result.Error is not null) { await Error(result.Error); return; }

        var state = room.GameState!;

        await Clients.Group(room.Code).SendAsync("PlayerPassed", new
        {
            playerId        = player.Id,
            currentPlayerId = room.Players[state.CurrentPlayerIndex].Id,
            newRound        = result.NewRound,
            tableCards      = result.NewRound ? Array.Empty<object>() : state.LastPlayedCards.Select(CardDto)
        });
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DTOs & helpers
    // ──────────────────────────────────────────────────────────────────────────

    private Task Error(string message) =>
        Clients.Caller.SendAsync("Error", message);

    private static object CardDto(Card c) => new
    {
        id       = c.Id,
        rank     = c.RankStr,
        suit     = c.SuitStr,
        value    = c.Value,
        isRed    = c.IsRed,
        rankEnum = (int)c.Rank,
        suitEnum = (int)c.Suit
    };

    private static object PlayerDto(Player p) => new
    {
        id          = p.Id,
        nickname    = p.Nickname,
        isConnected = p.IsConnected
    };

    private static object PlayerInfoDto(Player p) => new
    {
        id        = p.Id,
        nickname  = p.Nickname,
        cardCount = p.Hand.Count
    };
}
