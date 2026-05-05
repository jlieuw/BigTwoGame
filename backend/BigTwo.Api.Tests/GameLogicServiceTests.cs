using BigTwo.Api.Models;
using BigTwo.Api.Services;
using Xunit;

namespace BigTwo.Api.Tests;

public class GameLogicServiceTests
{
    private readonly GameLogicService _sut = new();

    private static Card C(Rank rank, Suit suit) => new(suit, rank);

    private static Player MakePlayer(string id = "p1") =>
        new(id, $"conn-{id}", $"Nick-{id}", Guid.NewGuid().ToString("N"));

    private static List<Player> MakePlayers(int n) =>
        Enumerable.Range(0, n)
            .Select(i => MakePlayer($"p{i}"))
            .ToList();

    // ── CreateShuffledDeck ────────────────────────────────────────────────────

    [Fact]
    public void CreateShuffledDeck_Returns52Cards()
    {
        var deck = _sut.CreateShuffledDeck();
        Assert.Equal(52, deck.Count);
    }

    [Fact]
    public void CreateShuffledDeck_AllCardsUnique()
    {
        var deck = _sut.CreateShuffledDeck();
        Assert.Equal(52, deck.Select(c => c.Id).Distinct().Count());
    }

    [Fact]
    public void CreateShuffledDeck_ContainsEveryRankAndSuit()
    {
        var deck = _sut.CreateShuffledDeck();
        foreach (Suit suit in Enum.GetValues<Suit>())
            foreach (Rank rank in Enum.GetValues<Rank>())
                Assert.Contains(deck, c => c.Suit == suit && c.Rank == rank);
    }

    [Fact]
    public void CreateShuffledDeck_DifferentCallsProduceDifferentOrder()
    {
        // Probability of two shuffles being identical is astronomically small
        var deck1 = _sut.CreateShuffledDeck().Select(c => c.Id).ToList();
        var deck2 = _sut.CreateShuffledDeck().Select(c => c.Id).ToList();
        Assert.False(deck1.SequenceEqual(deck2), "Two shuffles should not be in the same order");
    }

    // ── DealCards ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void DealCards_AllCardsDistributed(int playerCount)
    {
        var players = MakePlayers(playerCount);
        _sut.DealCards(players);
        Assert.Equal(52, players.Sum(p => p.Hand.Count));
    }

    [Fact]
    public void DealCards_With4Players_EachGets13Cards()
    {
        var players = MakePlayers(4);
        _sut.DealCards(players);
        Assert.All(players, p => Assert.Equal(13, p.Hand.Count));
    }

    [Fact]
    public void DealCards_HandsSortedByValue()
    {
        var players = MakePlayers(4);
        _sut.DealCards(players);
        foreach (var player in players)
        {
            var values = player.Hand.Select(c => c.Value).ToList();
            for (int i = 1; i < values.Count; i++)
                Assert.True(values[i - 1] <= values[i], "Hand must be sorted ascending by value");
        }
    }

    [Fact]
    public void DealCards_NoCardDealtTwice()
    {
        var players = MakePlayers(4);
        _sut.DealCards(players);
        var allIds = players.SelectMany(p => p.Hand).Select(c => c.Id).ToList();
        Assert.Equal(52, allIds.Distinct().Count());
    }

    // ── FindStartingPlayerIndex ───────────────────────────────────────────────

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void FindStartingPlayerIndex_ReturnPlayerWithThreeDiamonds(int playerCount)
    {
        var players = MakePlayers(playerCount);
        _sut.DealCards(players);
        var idx = _sut.FindStartingPlayerIndex(players);
        Assert.Contains(players[idx].Hand, c => c.Rank == Rank.Three && c.Suit == Suit.Diamonds);
    }

    // ── ParseCombo (by List<Card>) ────────────────────────────────────────────

    [Fact]
    public void ParseCombo_Single_ReturnsCorrectType()
    {
        var combo = _sut.ParseCombo(new List<Card> { C(Rank.Five, Suit.Hearts) });
        Assert.NotNull(combo);
        Assert.Equal(ComboType.Single, combo!.Type);
    }

    [Fact]
    public void ParseCombo_Single_ValueEqualsCardValue()
    {
        var card  = C(Rank.Five, Suit.Hearts);
        var combo = _sut.ParseCombo(new List<Card> { card });
        Assert.Equal(card.Value, combo!.ComboValue);
    }

    [Fact]
    public void ParseCombo_ValidPair_ReturnsCorrectType()
    {
        var cards = new List<Card> { C(Rank.Seven, Suit.Diamonds), C(Rank.Seven, Suit.Hearts) };
        var combo = _sut.ParseCombo(cards);
        Assert.NotNull(combo);
        Assert.Equal(ComboType.Pair, combo!.Type);
    }

    [Fact]
    public void ParseCombo_Pair_ValueIsMaxOfTwoCards()
    {
        // 7♦ value=16, 7♥ value=18 → max = 18
        var cards = new List<Card> { C(Rank.Seven, Suit.Diamonds), C(Rank.Seven, Suit.Hearts) };
        var combo = _sut.ParseCombo(cards);
        Assert.Equal(18, combo!.ComboValue);
    }

    [Fact]
    public void ParseCombo_PairDifferentRank_ReturnsNull()
    {
        var cards = new List<Card> { C(Rank.Seven, Suit.Diamonds), C(Rank.Eight, Suit.Hearts) };
        Assert.Null(_sut.ParseCombo(cards));
    }

    [Fact]
    public void ParseCombo_ValidTriple_ReturnsCorrectType()
    {
        var cards = new List<Card>
        {
            C(Rank.King, Suit.Diamonds), C(Rank.King, Suit.Clubs), C(Rank.King, Suit.Hearts)
        };
        var combo = _sut.ParseCombo(cards);
        Assert.NotNull(combo);
        Assert.Equal(ComboType.Triple, combo!.Type);
    }

    [Fact]
    public void ParseCombo_TripleNotAllSameRank_ReturnsNull()
    {
        var cards = new List<Card>
        {
            C(Rank.King, Suit.Diamonds), C(Rank.King, Suit.Clubs), C(Rank.Queen, Suit.Hearts)
        };
        Assert.Null(_sut.ParseCombo(cards));
    }

    [Fact]
    public void ParseCombo_Straight_ReturnsCorrectType()
    {
        var cards = new List<Card>
        {
            C(Rank.Three, Suit.Diamonds),
            C(Rank.Four,  Suit.Clubs),
            C(Rank.Five,  Suit.Hearts),
            C(Rank.Six,   Suit.Spades),
            C(Rank.Seven, Suit.Diamonds)
        };
        var combo = _sut.ParseCombo(cards);
        Assert.NotNull(combo);
        Assert.Equal(ComboType.Straight, combo!.Type);
    }

    [Fact]
    public void ParseCombo_Straight_AceHighWrap_IsNotStraight()
    {
        // A K Q J 10 → consecutive (10,11,12,13,14) → should be a straight
        var cards = new List<Card>
        {
            C(Rank.Ten,   Suit.Diamonds),
            C(Rank.Jack,  Suit.Clubs),
            C(Rank.Queen, Suit.Hearts),
            C(Rank.King,  Suit.Spades),
            C(Rank.Ace,   Suit.Diamonds)
        };
        var combo = _sut.ParseCombo(cards);
        Assert.NotNull(combo);
        Assert.Equal(ComboType.Straight, combo!.Type);
    }

    [Fact]
    public void ParseCombo_Flush_ReturnsCorrectType()
    {
        var cards = new List<Card>
        {
            C(Rank.Three, Suit.Hearts),
            C(Rank.Five,  Suit.Hearts),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.Nine,  Suit.Hearts),
            C(Rank.Jack,  Suit.Hearts)
        };
        var combo = _sut.ParseCombo(cards);
        Assert.NotNull(combo);
        Assert.Equal(ComboType.Flush, combo!.Type);
    }

    [Fact]
    public void ParseCombo_FullHouse_ReturnsCorrectType()
    {
        var cards = new List<Card>
        {
            C(Rank.Ace,  Suit.Diamonds), C(Rank.Ace,  Suit.Clubs), C(Rank.Ace,  Suit.Hearts),
            C(Rank.King, Suit.Diamonds), C(Rank.King, Suit.Clubs)
        };
        var combo = _sut.ParseCombo(cards);
        Assert.NotNull(combo);
        Assert.Equal(ComboType.FullHouse, combo!.Type);
    }

    [Fact]
    public void ParseCombo_FullHouse_ValueBasedOnTripleRank()
    {
        // Triple of Aces (rank=14): (14-3)*4 = 44
        var cards = new List<Card>
        {
            C(Rank.Ace,  Suit.Diamonds), C(Rank.Ace,  Suit.Clubs), C(Rank.Ace,  Suit.Hearts),
            C(Rank.King, Suit.Diamonds), C(Rank.King, Suit.Clubs)
        };
        var combo = _sut.ParseCombo(cards);
        Assert.Equal((14 - 3) * 4, combo!.ComboValue);
    }

    [Fact]
    public void ParseCombo_FourOfAKind_ReturnsCorrectType()
    {
        var cards = new List<Card>
        {
            C(Rank.Queen, Suit.Diamonds), C(Rank.Queen, Suit.Clubs),
            C(Rank.Queen, Suit.Hearts),   C(Rank.Queen, Suit.Spades),
            C(Rank.Three, Suit.Diamonds)
        };
        var combo = _sut.ParseCombo(cards);
        Assert.NotNull(combo);
        Assert.Equal(ComboType.FourOfAKind, combo!.Type);
    }

    [Fact]
    public void ParseCombo_FourOfAKind_ValueIgnoresKickerSuit()
    {
        // Value = (rank - 3) * 4, ignoring the kicker's suit
        // Queens (rank=12): (12-3)*4 = 36
        var cards1 = new List<Card>
        {
            C(Rank.Queen, Suit.Diamonds), C(Rank.Queen, Suit.Clubs),
            C(Rank.Queen, Suit.Hearts),   C(Rank.Queen, Suit.Spades),
            C(Rank.Three, Suit.Diamonds)   // kicker low suit
        };
        var cards2 = new List<Card>
        {
            C(Rank.Queen, Suit.Diamonds), C(Rank.Queen, Suit.Clubs),
            C(Rank.Queen, Suit.Hearts),   C(Rank.Queen, Suit.Spades),
            C(Rank.Three, Suit.Spades)    // kicker high suit
        };
        var combo1 = _sut.ParseCombo(cards1)!;
        var combo2 = _sut.ParseCombo(cards2)!;
        Assert.Equal(combo1.ComboValue, combo2.ComboValue);
    }

    [Fact]
    public void ParseCombo_StraightFlush_ReturnsCorrectType()
    {
        var cards = new List<Card>
        {
            C(Rank.Three, Suit.Clubs), C(Rank.Four,  Suit.Clubs),
            C(Rank.Five,  Suit.Clubs), C(Rank.Six,   Suit.Clubs),
            C(Rank.Seven, Suit.Clubs)
        };
        var combo = _sut.ParseCombo(cards);
        Assert.NotNull(combo);
        Assert.Equal(ComboType.StraightFlush, combo!.Type);
    }

    [Fact]
    public void ParseCombo_FourCards_ReturnsNull()
    {
        var cards = new List<Card>
        {
            C(Rank.Three, Suit.Diamonds), C(Rank.Four, Suit.Clubs),
            C(Rank.Five,  Suit.Hearts),   C(Rank.Six,  Suit.Spades)
        };
        Assert.Null(_sut.ParseCombo(cards));
    }

    [Fact]
    public void ParseCombo_FiveInvalidCards_ReturnsNull()
    {
        // No straight, no flush, no matching groups
        var cards = new List<Card>
        {
            C(Rank.Three, Suit.Diamonds), C(Rank.Five,  Suit.Clubs),
            C(Rank.Seven, Suit.Hearts),   C(Rank.Nine,  Suit.Spades),
            C(Rank.Jack,  Suit.Diamonds)
        };
        Assert.Null(_sut.ParseCombo(cards));
    }

    [Fact]
    public void ParseCombo_ZeroCards_ReturnsNull()
    {
        Assert.Null(_sut.ParseCombo(new List<Card>()));
    }

    // ── ParseCombo (by card IDs) ──────────────────────────────────────────────

    [Fact]
    public void ParseComboByIds_ValidIds_ResolvesToCorrectCombo()
    {
        // 7♥ = "7-2", 7♠ = "7-3"
        var hand = new List<Card>
        {
            C(Rank.Three, Suit.Diamonds),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.Seven, Suit.Spades),
            C(Rank.Ace,   Suit.Clubs)
        };
        var combo = _sut.ParseCombo(new[] { "7-2", "7-3" }, hand);
        Assert.NotNull(combo);
        Assert.Equal(ComboType.Pair, combo!.Type);
    }

    [Fact]
    public void ParseComboByIds_UnknownId_ReturnsNull()
    {
        var hand = new List<Card> { C(Rank.Three, Suit.Diamonds) };
        Assert.Null(_sut.ParseCombo(new[] { "99-9" }, hand));
    }

    [Fact]
    public void ParseComboByIds_CardNotInHand_ReturnsNull()
    {
        var hand = new List<Card> { C(Rank.Three, Suit.Diamonds) }; // only 3♦
        // Try to play 3♣ which is not in hand
        Assert.Null(_sut.ParseCombo(new[] { "3-1" }, hand));
    }

    // ── IsValidPlay ───────────────────────────────────────────────────────────

    [Fact]
    public void IsValidPlay_LeadTurn_AnyComboIsValid()
    {
        var combo = _sut.ParseCombo(new List<Card> { C(Rank.Three, Suit.Clubs) })!;
        Assert.True(_sut.IsValidPlay(combo, null, isLeadTurn: true, mustIncludeThreeDiamonds: false));
    }

    [Fact]
    public void IsValidPlay_FirstTurn_WithoutThreeDiamonds_IsInvalid()
    {
        var combo = _sut.ParseCombo(new List<Card> { C(Rank.Three, Suit.Clubs) })!;
        Assert.False(_sut.IsValidPlay(combo, null, isLeadTurn: true, mustIncludeThreeDiamonds: true));
    }

    [Fact]
    public void IsValidPlay_FirstTurn_WithThreeDiamonds_IsValid()
    {
        var combo = _sut.ParseCombo(new List<Card> { C(Rank.Three, Suit.Diamonds) })!;
        Assert.True(_sut.IsValidPlay(combo, null, isLeadTurn: true, mustIncludeThreeDiamonds: true));
    }

    [Fact]
    public void IsValidPlay_FirstTurn_ThreeDiamondsInPair_IsValid()
    {
        var combo = _sut.ParseCombo(new List<Card>
        {
            C(Rank.Three, Suit.Diamonds), C(Rank.Three, Suit.Clubs)
        })!;
        Assert.True(_sut.IsValidPlay(combo, null, isLeadTurn: true, mustIncludeThreeDiamonds: true));
    }

    [Fact]
    public void IsValidPlay_MustBeatLastPlay_StrongerCardIsValid()
    {
        var lastPlay  = _sut.ParseCombo(new List<Card> { C(Rank.Eight, Suit.Spades) })!;
        var strongPlay = _sut.ParseCombo(new List<Card> { C(Rank.Nine, Suit.Diamonds) })!;
        Assert.True(_sut.IsValidPlay(strongPlay, lastPlay, isLeadTurn: false, mustIncludeThreeDiamonds: false));
    }

    [Fact]
    public void IsValidPlay_MustBeatLastPlay_WeakerCardIsInvalid()
    {
        var lastPlay  = _sut.ParseCombo(new List<Card> { C(Rank.Eight, Suit.Spades) })!;
        var weakPlay  = _sut.ParseCombo(new List<Card> { C(Rank.Seven, Suit.Spades) })!;
        Assert.False(_sut.IsValidPlay(weakPlay, lastPlay, isLeadTurn: false, mustIncludeThreeDiamonds: false));
    }

    [Fact]
    public void IsValidPlay_FiveCardBeats_LowerFiveCard()
    {
        var lastPlay = _sut.ParseCombo(new List<Card>
        {
            C(Rank.Three, Suit.Diamonds), C(Rank.Four,  Suit.Clubs),
            C(Rank.Five,  Suit.Hearts),   C(Rank.Six,   Suit.Spades),
            C(Rank.Seven, Suit.Diamonds)
        })!; // Straight, lowest

        var newPlay = _sut.ParseCombo(new List<Card>
        {
            C(Rank.Three, Suit.Clubs), C(Rank.Four, Suit.Clubs),
            C(Rank.Five,  Suit.Clubs), C(Rank.Six,  Suit.Clubs),
            C(Rank.Seven, Suit.Clubs)
        })!; // Straight Flush beats any Straight

        Assert.True(_sut.IsValidPlay(newPlay, lastPlay, isLeadTurn: false, mustIncludeThreeDiamonds: false));
    }
}
