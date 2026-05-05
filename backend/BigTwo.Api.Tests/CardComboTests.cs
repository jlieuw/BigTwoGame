using BigTwo.Api.Models;
using Xunit;

namespace BigTwo.Api.Tests;

public class CardComboTests
{
    private static Card C(Rank rank, Suit suit) => new(suit, rank);

    private static CardCombo Single(Rank rank, Suit suit)
    {
        var card = C(rank, suit);
        return new CardCombo(ComboType.Single, [card], card.Value);
    }

    private static CardCombo Pair(Rank rank, Suit s1, Suit s2)
    {
        var cards = new List<Card> { C(rank, s1), C(rank, s2) };
        return new CardCombo(ComboType.Pair, cards, cards.Max(c => c.Value));
    }

    private static CardCombo Triple(Rank rank)
    {
        var cards = new List<Card>
        {
            C(rank, Suit.Diamonds), C(rank, Suit.Clubs), C(rank, Suit.Hearts)
        };
        return new CardCombo(ComboType.Triple, cards, cards.Max(c => c.Value));
    }

    private static List<Card> FiveCardList() =>
    [
        C(Rank.Three, Suit.Diamonds),
        C(Rank.Four,  Suit.Clubs),
        C(Rank.Five,  Suit.Hearts),
        C(Rank.Six,   Suit.Spades),
        C(Rank.Seven, Suit.Diamonds)
    ];

    // ── Singles ──────────────────────────────────────────────────────────────

    [Fact]
    public void Single_HigherRank_BeatsLowerRank()
    {
        var high = Single(Rank.King,  Suit.Diamonds);
        var low  = Single(Rank.Queen, Suit.Diamonds);
        Assert.True(high.Beats(low));
        Assert.False(low.Beats(high));
    }

    [Fact]
    public void Single_SameRank_HigherSuitWins()
    {
        var spades   = Single(Rank.Seven, Suit.Spades);
        var diamonds = Single(Rank.Seven, Suit.Diamonds);
        Assert.True(spades.Beats(diamonds));
        Assert.False(diamonds.Beats(spades));
    }

    [Fact]
    public void Single_SuitOrder_IsCorrect()
    {
        // Suit order: Diamonds(0) < Clubs(1) < Hearts(2) < Spades(3)
        var s = Single(Rank.Five, Suit.Spades);
        var h = Single(Rank.Five, Suit.Hearts);
        var c = Single(Rank.Five, Suit.Clubs);
        var d = Single(Rank.Five, Suit.Diamonds);
        Assert.True(s.Beats(h));
        Assert.True(h.Beats(c));
        Assert.True(c.Beats(d));
    }

    [Fact]
    public void Single_Two_BeatsAce()
    {
        var two = Single(Rank.Two, Suit.Diamonds);
        var ace = Single(Rank.Ace, Suit.Spades); // A♠ is strongest ace
        Assert.True(two.Beats(ace));
    }

    [Fact]
    public void Single_SameCard_DoesNotBeatItself()
    {
        var a = Single(Rank.King, Suit.Spades);
        var b = Single(Rank.King, Suit.Spades);
        Assert.False(a.Beats(b));
    }

    // ── Pairs ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Pair_HigherRank_BeatsLowerRank()
    {
        var high = Pair(Rank.Ace,  Suit.Diamonds, Suit.Clubs);
        var low  = Pair(Rank.King, Suit.Hearts,   Suit.Spades);
        Assert.True(high.Beats(low));
        Assert.False(low.Beats(high));
    }

    [Fact]
    public void Pair_SameRank_HigherTopSuitWins()
    {
        // Both are 7s; one has 7♠ (value 27) vs 7♥ (value 26)
        var withSpades  = Pair(Rank.Seven, Suit.Hearts, Suit.Spades);
        var withHearts  = Pair(Rank.Seven, Suit.Diamonds, Suit.Hearts);
        Assert.True(withSpades.Beats(withHearts));
    }

    [Fact]
    public void Pair_CannotBeat_Single()
    {
        var pair   = Pair(Rank.Two, Suit.Hearts, Suit.Spades);
        var single = Single(Rank.Three, Suit.Diamonds);
        Assert.False(pair.Beats(single));
        Assert.False(single.Beats(pair));
    }

    // ── Triples ───────────────────────────────────────────────────────────────

    [Fact]
    public void Triple_HigherRank_BeatsLowerRank()
    {
        var high = Triple(Rank.Ace);
        var low  = Triple(Rank.King);
        Assert.True(high.Beats(low));
        Assert.False(low.Beats(high));
    }

    [Fact]
    public void Triple_CannotBeat_Pair()
    {
        var triple = Triple(Rank.Two);
        var pair   = Pair(Rank.Three, Suit.Diamonds, Suit.Clubs);
        Assert.False(triple.Beats(pair));
    }

    // ── Five-card type hierarchy ───────────────────────────────────────────────

    [Fact]
    public void StraightFlush_Beats_FourOfAKind()
    {
        var sf   = new CardCombo(ComboType.StraightFlush, FiveCardList(), 999);
        var foak = new CardCombo(ComboType.FourOfAKind,  FiveCardList(), 0);
        Assert.True(sf.Beats(foak));
        Assert.False(foak.Beats(sf));
    }

    [Fact]
    public void FourOfAKind_Beats_FullHouse()
    {
        var foak = new CardCombo(ComboType.FourOfAKind, FiveCardList(), 0);
        var fh   = new CardCombo(ComboType.FullHouse,   FiveCardList(), 999);
        Assert.True(foak.Beats(fh));
        Assert.False(fh.Beats(foak));
    }

    [Fact]
    public void FullHouse_Beats_Flush()
    {
        var fh    = new CardCombo(ComboType.FullHouse, FiveCardList(), 0);
        var flush = new CardCombo(ComboType.Flush,     FiveCardList(), 999);
        Assert.True(fh.Beats(flush));
        Assert.False(flush.Beats(fh));
    }

    [Fact]
    public void Flush_Beats_Straight()
    {
        var flush    = new CardCombo(ComboType.Flush,    FiveCardList(), 0);
        var straight = new CardCombo(ComboType.Straight, FiveCardList(), 999);
        Assert.True(flush.Beats(straight));
        Assert.False(straight.Beats(flush));
    }

    [Fact]
    public void SameType_FiveCard_HigherComboValue_Wins()
    {
        var strong = new CardCombo(ComboType.Straight, FiveCardList(), 50);
        var weak   = new CardCombo(ComboType.Straight, FiveCardList(), 30);
        Assert.True(strong.Beats(weak));
        Assert.False(weak.Beats(strong));
    }

    [Fact]
    public void SameType_FiveCard_EqualValue_DoesNotBeat()
    {
        var a = new CardCombo(ComboType.Straight, FiveCardList(), 42);
        var b = new CardCombo(ComboType.Straight, FiveCardList(), 42);
        Assert.False(a.Beats(b));
    }

    // ── Cross-count guards ────────────────────────────────────────────────────

    [Fact]
    public void FiveCard_CannotBeat_Single()
    {
        var five   = new CardCombo(ComboType.Straight, FiveCardList(), 100);
        var single = Single(Rank.Two, Suit.Spades);
        Assert.False(five.Beats(single));
        Assert.False(single.Beats(five));
    }

    [Fact]
    public void FiveCard_CannotBeat_Pair()
    {
        var five = new CardCombo(ComboType.Straight, FiveCardList(), 100);
        var pair = Pair(Rank.Two, Suit.Hearts, Suit.Spades);
        Assert.False(five.Beats(pair));
    }
}
