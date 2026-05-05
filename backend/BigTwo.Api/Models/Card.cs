namespace BigTwo.Api.Models;

/// <summary>
/// Suit order low → high: Diamonds, Clubs, Hearts, Spades
/// </summary>
public enum Suit
{
    Diamonds = 0,
    Clubs    = 1,
    Hearts   = 2,
    Spades   = 3
}

/// <summary>
/// Rank order in Big Two: 3 is lowest, 2 is highest.
/// Int values map to 3–15 for easy consecutive-rank arithmetic.
/// </summary>
public enum Rank
{
    Three = 3,
    Four  = 4,
    Five  = 5,
    Six   = 6,
    Seven = 7,
    Eight = 8,
    Nine  = 9,
    Ten   = 10,
    Jack  = 11,
    Queen = 12,
    King  = 13,
    Ace   = 14,
    Two   = 15   // "Big Two" — highest rank
}

public class Card
{
    public Suit Suit { get; }
    public Rank Rank { get; }

    public Card(Suit suit, Rank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    /// <summary>
    /// Composite value for ordering. Higher is stronger.
    /// 3♦ = 0, 3♣ = 1, 3♥ = 2, 3♠ = 3, 4♦ = 4, ... 2♠ = 51
    /// </summary>
    public int Value => ((int)Rank - 3) * 4 + (int)Suit;

    /// <summary>Stable unique ID used to reference this card across client/server.</summary>
    public string Id => $"{(int)Rank}-{(int)Suit}";

    public string RankStr => Rank switch
    {
        Rank.Jack  => "J",
        Rank.Queen => "Q",
        Rank.King  => "K",
        Rank.Ace   => "A",
        Rank.Two   => "2",
        _          => ((int)Rank).ToString()
    };

    public string SuitStr => Suit switch
    {
        Suit.Diamonds => "♦",
        Suit.Clubs    => "♣",
        Suit.Hearts   => "♥",
        Suit.Spades   => "♠",
        _             => "?"
    };

    public bool IsRed => Suit is Suit.Hearts or Suit.Diamonds;

    public override string ToString() => $"{RankStr}{SuitStr}";
}
