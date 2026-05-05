using BigTwo.Api.Models;
using Xunit;

namespace BigTwo.Api.Tests;

public class CardTests
{
    // Value = (rank - 3) * 4 + suit
    [Theory]
    [InlineData(Rank.Three, Suit.Diamonds, 0)]   // (3-3)*4 + 0  = 0
    [InlineData(Rank.Three, Suit.Clubs,    1)]   // (3-3)*4 + 1  = 1
    [InlineData(Rank.Three, Suit.Hearts,   2)]   // (3-3)*4 + 2  = 2
    [InlineData(Rank.Three, Suit.Spades,   3)]   // (3-3)*4 + 3  = 3
    [InlineData(Rank.Four,  Suit.Diamonds, 4)]   // (4-3)*4 + 0  = 4
    [InlineData(Rank.Ace,   Suit.Spades,  47)]   // (14-3)*4 + 3 = 47
    [InlineData(Rank.Two,   Suit.Diamonds,48)]   // (15-3)*4 + 0 = 48
    [InlineData(Rank.Two,   Suit.Spades,  51)]   // (15-3)*4 + 3 = 51
    public void Value_ComputesCorrectly(Rank rank, Suit suit, int expected)
    {
        var card = new Card(suit, rank);
        Assert.Equal(expected, card.Value);
    }

    [Theory]
    [InlineData(Rank.Three, Suit.Diamonds, "3-0")]
    [InlineData(Rank.Two,   Suit.Spades,  "15-3")]
    [InlineData(Rank.Ace,   Suit.Hearts,  "14-2")]
    [InlineData(Rank.Jack,  Suit.Clubs,   "11-1")]
    public void Id_HasCorrectFormat(Rank rank, Suit suit, string expected)
    {
        var card = new Card(suit, rank);
        Assert.Equal(expected, card.Id);
    }

    [Theory]
    [InlineData(Rank.Three, "3")]
    [InlineData(Rank.Ten,   "10")]
    [InlineData(Rank.Jack,  "J")]
    [InlineData(Rank.Queen, "Q")]
    [InlineData(Rank.King,  "K")]
    [InlineData(Rank.Ace,   "A")]
    [InlineData(Rank.Two,   "2")]
    public void RankStr_ReturnsCorrectString(Rank rank, string expected)
    {
        var card = new Card(Suit.Diamonds, rank);
        Assert.Equal(expected, card.RankStr);
    }

    [Theory]
    [InlineData(Suit.Diamonds, "♦")]
    [InlineData(Suit.Clubs,    "♣")]
    [InlineData(Suit.Hearts,   "♥")]
    [InlineData(Suit.Spades,   "♠")]
    public void SuitStr_ReturnsCorrectSymbol(Suit suit, string expected)
    {
        var card = new Card(suit, Rank.Three);
        Assert.Equal(expected, card.SuitStr);
    }

    [Theory]
    [InlineData(Suit.Hearts,   true)]
    [InlineData(Suit.Diamonds, true)]
    [InlineData(Suit.Clubs,    false)]
    [InlineData(Suit.Spades,   false)]
    public void IsRed_CorrectForSuit(Suit suit, bool expectedRed)
    {
        var card = new Card(suit, Rank.Three);
        Assert.Equal(expectedRed, card.IsRed);
    }

    [Fact]
    public void ToString_ReturnsRankAndSuit()
    {
        var card = new Card(Suit.Diamonds, Rank.Three);
        Assert.Equal("3♦", card.ToString());
    }

    [Fact]
    public void TwoDiamonds_HasHighestRank_LowestSuit()
    {
        // 2♠ is the strongest single card (value = 51)
        var twoSpades   = new Card(Suit.Spades,   Rank.Two);
        var twoDiamonds = new Card(Suit.Diamonds, Rank.Two);
        Assert.True(twoSpades.Value > twoDiamonds.Value);
    }
}
