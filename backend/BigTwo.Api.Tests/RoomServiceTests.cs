using BigTwo.Api.Models;
using BigTwo.Api.Services;
using Xunit;

namespace BigTwo.Api.Tests;

public class RoomServiceTests
{
    private static Card C(Rank rank, Suit suit) => new(suit, rank);

    private RoomService NewSut() => new(new GameLogicService());

    // Starts a game with N players. Host connection = "host-conn", others "conn1".."connN-1"
    private (RoomService sut, Room room) StartedGame(int playerCount)
    {
        var sut  = NewSut();
        var room = sut.CreateRoom("host-conn", "Host");
        for (int i = 1; i < playerCount; i++)
            sut.JoinRoom($"conn{i}", room.Code, $"Player{i}");
        sut.StartGame("host-conn");
        return (sut, room);
    }

    // ── CreateRoom ────────────────────────────────────────────────────────────

    [Fact]
    public void CreateRoom_ReturnsRoomWithHost()
    {
        var sut  = NewSut();
        var room = sut.CreateRoom("conn1", "Alice");

        Assert.NotNull(room);
        Assert.Equal(6, room.Code.Length);
        Assert.Single(room.Players);
        Assert.Equal("Alice", room.Players[0].Nickname);
        Assert.Equal(room.Players[0].Id, room.HostId);
        Assert.Equal(RoomStatus.Waiting, room.Status);
    }

    [Fact]
    public void CreateRoom_GeneratesUniqueRoomCodes()
    {
        var sut   = NewSut();
        var codes = Enumerable.Range(0, 20)
            .Select(i => sut.CreateRoom($"conn{i}", $"Player{i}").Code)
            .ToList();

        Assert.Equal(20, codes.Distinct().Count());
    }

    [Fact]
    public void CreateRoom_PlayerHasSessionToken()
    {
        var sut    = NewSut();
        var room   = sut.CreateRoom("conn1", "Alice");
        var player = room.Players[0];

        Assert.NotNull(player.SessionToken);
        Assert.NotEmpty(player.SessionToken);
    }

    // ── JoinRoom ──────────────────────────────────────────────────────────────

    [Fact]
    public void JoinRoom_ValidRoom_AddsPlayerAndReturnsNoError()
    {
        var sut  = NewSut();
        var room = sut.CreateRoom("conn1", "Alice");

        var (joinedRoom, player, error) = sut.JoinRoom("conn2", room.Code, "Bob");

        Assert.Null(error);
        Assert.NotNull(joinedRoom);
        Assert.NotNull(player);
        Assert.Equal(2, joinedRoom!.Players.Count);
        Assert.Equal("Bob", player!.Nickname);
    }

    [Fact]
    public void JoinRoom_NonExistentRoomCode_ReturnsError()
    {
        var sut = NewSut();
        var (_, _, error) = sut.JoinRoom("conn1", "ZZZZZZ", "Alice");

        Assert.NotNull(error);
    }

    [Fact]
    public void JoinRoom_DuplicateNickname_ReturnsError()
    {
        var sut  = NewSut();
        var room = sut.CreateRoom("conn1", "Alice");

        var (_, _, error) = sut.JoinRoom("conn2", room.Code, "Alice");

        Assert.NotNull(error);
    }

    [Fact]
    public void JoinRoom_DuplicateNickname_CaseInsensitive()
    {
        var sut  = NewSut();
        var room = sut.CreateRoom("conn1", "Alice");

        var (_, _, error) = sut.JoinRoom("conn2", room.Code, "alice");

        Assert.NotNull(error);
    }

    [Fact]
    public void JoinRoom_FullRoom_ReturnsError()
    {
        var sut  = NewSut();
        var room = sut.CreateRoom("conn1", "P1");
        sut.JoinRoom("conn2", room.Code, "P2");
        sut.JoinRoom("conn3", room.Code, "P3");
        sut.JoinRoom("conn4", room.Code, "P4");

        var (_, _, error) = sut.JoinRoom("conn5", room.Code, "P5");

        Assert.NotNull(error);
        Assert.Contains("full", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JoinRoom_GameAlreadyStarted_ReturnsError()
    {
        var sut  = NewSut();
        var room = sut.CreateRoom("conn1", "Host");
        sut.JoinRoom("conn2", room.Code, "Player");
        sut.StartGame("conn1");

        var (_, _, error) = sut.JoinRoom("conn3", room.Code, "Latecomer");

        Assert.NotNull(error);
    }

    // ── GetRoom / Lookup ──────────────────────────────────────────────────────

    [Fact]
    public void GetRoom_ExistingCode_ReturnsRoom()
    {
        var sut  = NewSut();
        var room = sut.CreateRoom("conn1", "Alice");

        Assert.NotNull(sut.GetRoom(room.Code));
    }

    [Fact]
    public void GetRoom_UnknownCode_ReturnsNull()
    {
        var sut = NewSut();
        Assert.Null(sut.GetRoom("XXXXXX"));
    }

    [Fact]
    public void Lookup_KnownConnection_ReturnsRoomAndPlayer()
    {
        var sut  = NewSut();
        var room = sut.CreateRoom("conn1", "Alice");

        var (foundRoom, foundPlayer) = sut.Lookup("conn1");

        Assert.NotNull(foundRoom);
        Assert.NotNull(foundPlayer);
        Assert.Equal(room.Code, foundRoom!.Code);
    }

    [Fact]
    public void Lookup_UnknownConnection_ReturnsNulls()
    {
        var sut = NewSut();
        var (room, player) = sut.Lookup("no-such-conn");

        Assert.Null(room);
        Assert.Null(player);
    }

    // ── StartGame ─────────────────────────────────────────────────────────────

    [Fact]
    public void StartGame_AsHost_StartsGameWithNoError()
    {
        var sut  = NewSut();
        var room = sut.CreateRoom("conn1", "Host");
        sut.JoinRoom("conn2", room.Code, "Guest");

        var error = sut.StartGame("conn1");

        Assert.Null(error);
        Assert.Equal(RoomStatus.Playing, room.Status);
        Assert.NotNull(room.GameState);
    }

    [Fact]
    public void StartGame_AsNonHost_ReturnsError()
    {
        var sut  = NewSut();
        var room = sut.CreateRoom("conn1", "Host");
        sut.JoinRoom("conn2", room.Code, "Guest");

        var error = sut.StartGame("conn2");

        Assert.NotNull(error);
        Assert.Contains("host", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StartGame_WithOnlyOnePlayer_ReturnsError()
    {
        var sut = NewSut();
        sut.CreateRoom("conn1", "Solo");

        var error = sut.StartGame("conn1");

        Assert.NotNull(error);
    }

    [Fact]
    public void StartGame_With4Players_Deals13CardsEach()
    {
        var (_, room) = StartedGame(4);
        Assert.All(room.Players, p => Assert.Equal(13, p.Hand.Count));
    }

    [Fact]
    public void StartGame_GameState_StartingPlayerHasThreeDiamonds()
    {
        var (_, room) = StartedGame(2);
        var startPlayer = room.Players[room.GameState!.CurrentPlayerIndex];
        Assert.Contains(startPlayer.Hand, c => c.Rank == Rank.Three && c.Suit == Suit.Diamonds);
    }

    [Fact]
    public void StartGame_GameState_IsFirstTurnTrue()
    {
        var (_, room) = StartedGame(2);
        Assert.True(room.GameState!.IsFirstTurn);
    }

    // ── PlayCards ─────────────────────────────────────────────────────────────

    [Fact]
    public void PlayCards_NotYourTurn_ReturnsError()
    {
        var (sut, room) = StartedGame(2);
        var state       = room.GameState!;
        // The player at index 0 or 1 who is NOT the current player
        var nonCurrentPlayer = room.Players[1 - state.CurrentPlayerIndex];
        // Try to play any card from the current player's hand (wrong person)
        var currentHand = room.Players[state.CurrentPlayerIndex].Hand;

        var result = sut.PlayCards(nonCurrentPlayer.ConnectionId, [currentHand[0].Id]);

        Assert.NotNull(result.Error);
        Assert.Contains("turn", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PlayCards_FirstTurnWithoutThreeDiamonds_ReturnsError()
    {
        var (sut, room) = StartedGame(2);
        var state       = room.GameState!;
        var startPlayer = room.Players[state.CurrentPlayerIndex];
        var notThreeDiamonds = startPlayer.Hand
            .First(c => !(c.Rank == Rank.Three && c.Suit == Suit.Diamonds));

        var result = sut.PlayCards(startPlayer.ConnectionId, [notThreeDiamonds.Id]);

        Assert.NotNull(result.Error);
        Assert.Contains("3♦", result.Error);
    }

    [Fact]
    public void PlayCards_ValidFirstPlay_Succeeds()
    {
        var (sut, room) = StartedGame(2);
        var state       = room.GameState!;
        var startPlayer = room.Players[state.CurrentPlayerIndex];
        var threeDiamonds = startPlayer.Hand
            .First(c => c.Rank == Rank.Three && c.Suit == Suit.Diamonds);

        var result = sut.PlayCards(startPlayer.ConnectionId, [threeDiamonds.Id]);

        Assert.Null(result.Error);
        Assert.False(result.GameOver);
    }

    [Fact]
    public void PlayCards_ValidPlay_RemovesCardsFromHand()
    {
        var (sut, room) = StartedGame(2);
        var state       = room.GameState!;
        var startPlayer = room.Players[state.CurrentPlayerIndex];
        var before      = startPlayer.Hand.Count;
        var threeDiamonds = startPlayer.Hand
            .First(c => c.Rank == Rank.Three && c.Suit == Suit.Diamonds);

        sut.PlayCards(startPlayer.ConnectionId, [threeDiamonds.Id]);

        Assert.Equal(before - 1, startPlayer.Hand.Count);
    }

    [Fact]
    public void PlayCards_CardNotInHand_ReturnsError()
    {
        var (sut, room) = StartedGame(2);
        var state       = room.GameState!;
        var startPlayer = room.Players[state.CurrentPlayerIndex];

        var result = sut.PlayCards(startPlayer.ConnectionId, ["99-9"]);

        Assert.NotNull(result.Error);
    }

    [Fact]
    public void PlayCards_WeakerThanTablePlay_ReturnsError()
    {
        var (sut, room) = StartedGame(2);
        var state       = room.GameState!;
        var startPlayer = room.Players[state.CurrentPlayerIndex];

        // First play: 3♦
        var threeDiamonds = startPlayer.Hand
            .First(c => c.Rank == Rank.Three && c.Suit == Suit.Diamonds);
        sut.PlayCards(startPlayer.ConnectionId, [threeDiamonds.Id]);

        // Second player tries to play something weaker — but must beat 3♦.
        // 3♦ is the weakest card (value=0), so any single card beats it.
        // Instead, let second player try to play a card that does NOT beat 3♦.
        // Since 3♦ is the absolute minimum, this case can only be triggered by
        // trying to play the same card (which isn't possible since it was removed)
        // OR by a different combo type mismatch.
        var nextPlayer = room.Players[state.CurrentPlayerIndex];
        // Find 3♣ which is the second weakest: value=1 > value=0, so it DOES beat 3♦.
        // To force an "invalid" play, we attempt to pass invalid combo type, e.g. a pair
        // where the table has a single — but pair vs single returns false from Beats().
        var nextHand = nextPlayer.Hand;
        var pairCards = nextHand
            .GroupBy(c => c.Rank)
            .FirstOrDefault(g => g.Count() >= 2)?
            .Take(2)
            .Select(c => c.Id)
            .ToList();

        if (pairCards is null) return; // not always possible depending on deal

        var result = sut.PlayCards(nextPlayer.ConnectionId, pairCards);

        Assert.NotNull(result.Error); // pair cannot beat a single
    }

    [Fact]
    public void PlayCards_UnknownConnection_ReturnsError()
    {
        var (sut, _) = StartedGame(2);
        var result = sut.PlayCards("ghost-conn", ["3-0"]);
        Assert.NotNull(result.Error);
    }

    // ── Pass ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Pass_OnLeadTurn_ReturnsError()
    {
        var (sut, room) = StartedGame(2);
        var state       = room.GameState!;
        var startPlayer = room.Players[state.CurrentPlayerIndex];

        // Lead turn (table empty) — passing is not allowed
        var result = sut.Pass(startPlayer.ConnectionId);

        Assert.NotNull(result.Error);
        Assert.Contains("pass", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pass_AfterCardPlayed_Succeeds()
    {
        var (sut, room) = StartedGame(2);
        var state       = room.GameState!;
        var startPlayer = room.Players[state.CurrentPlayerIndex];

        // First player plays 3♦
        var threeDiamonds = startPlayer.Hand
            .First(c => c.Rank == Rank.Three && c.Suit == Suit.Diamonds);
        sut.PlayCards(startPlayer.ConnectionId, [threeDiamonds.Id]);

        // Second player passes
        var nextPlayer = room.Players[state.CurrentPlayerIndex];
        var result = sut.Pass(nextPlayer.ConnectionId);

        Assert.Null(result.Error);
    }

    [Fact]
    public void Pass_NotYourTurn_ReturnsError()
    {
        var (sut, room) = StartedGame(2);
        var state       = room.GameState!;
        var startPlayer = room.Players[state.CurrentPlayerIndex];
        var otherPlayer = room.Players[1 - state.CurrentPlayerIndex];

        // Play 3♦ so table is not empty
        var threeDiamonds = startPlayer.Hand
            .First(c => c.Rank == Rank.Three && c.Suit == Suit.Diamonds);
        sut.PlayCards(startPlayer.ConnectionId, [threeDiamonds.Id]);

        // The original start player tries to pass (it's no longer their turn)
        var result = sut.Pass(startPlayer.ConnectionId);

        Assert.NotNull(result.Error);
    }

    [Fact]
    public void Pass_WhenEveryoneElsePasses_StartsNewRound()
    {
        var (sut, room) = StartedGame(3);
        var state       = room.GameState!;
        var startPlayer = room.Players[state.CurrentPlayerIndex];

        // First player leads with 3♦
        var threeDiamonds = startPlayer.Hand
            .First(c => c.Rank == Rank.Three && c.Suit == Suit.Diamonds);
        sut.PlayCards(startPlayer.ConnectionId, [threeDiamonds.Id]);

        // Second player passes
        var pass1 = sut.Pass(room.Players[state.CurrentPlayerIndex].ConnectionId);
        Assert.False(pass1.NewRound);

        // Third player passes → new round
        var pass2 = sut.Pass(room.Players[state.CurrentPlayerIndex].ConnectionId);
        Assert.True(pass2.NewRound);

        // Table should be cleared after new round
        Assert.Empty(state.LastPlayedCards);
        Assert.Null(state.LastPlayerId);
        Assert.Equal(0, state.PassCount);
    }

    [Fact]
    public void Pass_UnknownConnection_ReturnsError()
    {
        var (sut, _) = StartedGame(2);
        var result = sut.Pass("ghost-conn");
        Assert.NotNull(result.Error);
    }

    // ── Disconnect / Reconnect ────────────────────────────────────────────────

    [Fact]
    public void Disconnect_MarksPlayerAsDisconnected()
    {
        var sut    = NewSut();
        var room   = sut.CreateRoom("conn1", "Alice");
        var player = room.Players[0];

        sut.Disconnect("conn1");

        Assert.False(player.IsConnected);
    }

    [Fact]
    public void Disconnect_UnknownConnection_DoesNotThrow()
    {
        var sut = NewSut();
        var (room, player, newId) = sut.Disconnect("no-such-conn");
        Assert.Null(room);
        Assert.Null(player);
        Assert.Null(newId);
    }

    [Fact]
    public void Disconnect_CurrentPlayer_AdvancesTurn()
    {
        var (sut, room) = StartedGame(2);
        var state       = room.GameState!;
        var currentConn = room.Players[state.CurrentPlayerIndex].ConnectionId;

        var (_, _, newCurrentPlayerId) = sut.Disconnect(currentConn);

        Assert.NotNull(newCurrentPlayerId);
        // Turn should have moved to the other player
        var nextPlayer = room.Players[state.CurrentPlayerIndex];
        Assert.Equal(nextPlayer.Id, newCurrentPlayerId);
    }

    [Fact]
    public void Disconnect_NonCurrentPlayer_DoesNotAdvanceTurn()
    {
        var (sut, room) = StartedGame(2);
        var state       = room.GameState!;
        var originalIdx = state.CurrentPlayerIndex;
        var otherConn   = room.Players[(originalIdx + 1) % 2].ConnectionId;

        var (_, _, newCurrentPlayerId) = sut.Disconnect(otherConn);

        Assert.Null(newCurrentPlayerId);
        Assert.Equal(originalIdx, state.CurrentPlayerIndex);
    }

    [Fact]
    public void Disconnect_BeforeGameStarts_ReturnsNullCurrentPlayerId()
    {
        var sut  = NewSut();
        var room = sut.CreateRoom("conn1", "Alice");

        var (_, _, newCurrentPlayerId) = sut.Disconnect("conn1");

        Assert.Null(newCurrentPlayerId);
    }

    [Fact]
    public void Reconnect_ValidSession_UpdatesConnectionAndRestoresPlayer()
    {
        var sut    = NewSut();
        var room   = sut.CreateRoom("conn1", "Alice");
        var player = room.Players[0];

        sut.Disconnect("conn1");
        var (reconRoom, reconPlayer, error) = sut.Reconnect(room.Code, player.SessionToken, "conn1-new");

        Assert.Null(error);
        Assert.NotNull(reconRoom);
        Assert.NotNull(reconPlayer);
        Assert.Equal("conn1-new", reconPlayer!.ConnectionId);
        Assert.True(reconPlayer.IsConnected);
    }

    [Fact]
    public void Reconnect_InvalidSessionToken_ReturnsError()
    {
        var sut  = NewSut();
        var room = sut.CreateRoom("conn1", "Alice");

        var (_, _, error) = sut.Reconnect(room.Code, "wrong-token", "conn1-new");

        Assert.NotNull(error);
    }

    [Fact]
    public void Reconnect_NonExistentRoom_ReturnsError()
    {
        var sut = NewSut();
        var (_, _, error) = sut.Reconnect("ZZZZZZ", "any-token", "conn1-new");
        Assert.NotNull(error);
    }

    [Fact]
    public void Reconnect_OldConnectionIdReleased_CanBeUsedElsewhere()
    {
        var sut    = NewSut();
        var room   = sut.CreateRoom("conn1", "Alice");
        var player = room.Players[0];

        sut.Disconnect("conn1");
        sut.Reconnect(room.Code, player.SessionToken, "conn1-new");

        // Old connection id should no longer map to this player
        var (oldRoom, oldPlayer) = sut.Lookup("conn1");
        Assert.Null(oldRoom);
        Assert.Null(oldPlayer);
    }

    // ── RemoveStaleRooms ──────────────────────────────────────────────────────

    [Fact]
    public void RemoveStaleRooms_RemovesRoomsOlderThanMaxAge()
    {
        var sut  = NewSut();
        var room = sut.CreateRoom("conn1", "Alice");
        room.LastActivity = DateTime.UtcNow - TimeSpan.FromHours(1); // simulate 1-hour-old room

        var removed = sut.RemoveStaleRooms(TimeSpan.FromMinutes(30));

        Assert.Equal(1, removed);
        Assert.Null(sut.GetRoom(room.Code));
    }

    [Fact]
    public void RemoveStaleRooms_KeepsRecentRooms()
    {
        var sut = NewSut();
        sut.CreateRoom("conn1", "Alice"); // just created → LastActivity = UtcNow

        var removed = sut.RemoveStaleRooms(TimeSpan.FromMinutes(30));

        Assert.Equal(0, removed);
    }

    [Fact]
    public void RemoveStaleRooms_RemovesConnectionMappings()
    {
        var sut  = NewSut();
        var room = sut.CreateRoom("conn1", "Alice");
        sut.JoinRoom("conn2", room.Code, "Bob");
        room.LastActivity = DateTime.UtcNow - TimeSpan.FromHours(1);

        sut.RemoveStaleRooms(TimeSpan.FromMinutes(30));

        // Both connections should be gone
        var (r1, _) = sut.Lookup("conn1");
        var (r2, _) = sut.Lookup("conn2");
        Assert.Null(r1);
        Assert.Null(r2);
    }

    [Fact]
    public void RemoveStaleRooms_PartialStale_OnlyRemovesOldOnes()
    {
        var sut   = NewSut();
        var stale = sut.CreateRoom("conn1", "Old");
        var fresh = sut.CreateRoom("conn2", "New");
        stale.LastActivity = DateTime.UtcNow - TimeSpan.FromHours(1);

        var removed = sut.RemoveStaleRooms(TimeSpan.FromMinutes(30));

        Assert.Equal(1, removed);
        Assert.Null(sut.GetRoom(stale.Code));
        Assert.NotNull(sut.GetRoom(fresh.Code));
    }
}
