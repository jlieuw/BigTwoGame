using BigTwo.Api.Models;

namespace BigTwo.Api.Services;

/// <summary>
/// Pure, stateless Big Two game-logic service.
/// </summary>
public class GameLogicService
{
    // ──────────────────────────────────────────────────────────────────────────
    // Deck / dealing
    // ──────────────────────────────────────────────────────────────────────────

    public List<Card> CreateShuffledDeck()
    {
        var deck = new List<Card>(52);
        foreach (Suit suit in Enum.GetValues<Suit>())
            foreach (Rank rank in Enum.GetValues<Rank>())
                deck.Add(new Card(suit, rank));

        var rng = Random.Shared;
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
        return deck;
    }

    public void DealCards(List<Player> players)
    {
        var deck = CreateShuffledDeck();
        for (int i = 0; i < deck.Count; i++)
            players[i % players.Count].Hand.Add(deck[i]);

        foreach (var p in players)
            p.Hand = p.Hand.OrderBy(c => c.Value).ToList();
    }

    /// <summary>Returns the index of the player who holds 3♦.</summary>
    public int FindStartingPlayerIndex(List<Player> players)
    {
        for (int i = 0; i < players.Count; i++)
            if (players[i].Hand.Any(c => c.Rank == Rank.Three && c.Suit == Suit.Diamonds))
                return i;
        return 0;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Combo parsing
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Resolve card IDs against the player's hand and parse into a combo.
    /// Returns null if any ID is not found or the combination is illegal.
    /// </summary>
    public CardCombo? ParseCombo(IEnumerable<string> cardIds, List<Card> hand)
    {
        var cards = new List<Card>();
        foreach (var id in cardIds)
        {
            var card = hand.FirstOrDefault(c => c.Id == id);
            if (card is null) return null;
            cards.Add(card);
        }
        return ParseCombo(cards);
    }

    public CardCombo? ParseCombo(List<Card> cards) =>
        cards.Count switch
        {
            1 => new CardCombo(ComboType.Single, cards, cards[0].Value),
            2 => ParsePair(cards),
            3 => ParseTriple(cards),
            5 => ParseFiveCard(cards),
            _ => null
        };

    private static CardCombo? ParsePair(List<Card> cards)
    {
        if (cards[0].Rank != cards[1].Rank) return null;
        return new CardCombo(ComboType.Pair, cards, cards.Max(c => c.Value));
    }

    private static CardCombo? ParseTriple(List<Card> cards)
    {
        if (cards.Select(c => c.Rank).Distinct().Count() != 1) return null;
        return new CardCombo(ComboType.Triple, cards, cards.Max(c => c.Value));
    }

    private static CardCombo? ParseFiveCard(List<Card> cards)
    {
        bool flush  = cards.Select(c => c.Suit).Distinct().Count() == 1;
        bool straight = IsStraight(cards);

        if (straight && flush)
            return new CardCombo(ComboType.StraightFlush, cards, cards.Max(c => c.Value));

        if (IsFourOfAKind(cards))
        {
            var fourRank = cards.GroupBy(c => c.Rank).First(g => g.Count() == 4).Key;
            // Value = rank portion only (suit of kicker doesn't matter for comparison)
            return new CardCombo(ComboType.FourOfAKind, cards, ((int)fourRank - 3) * 4);
        }

        if (IsFullHouse(cards))
        {
            var threeRank = cards.GroupBy(c => c.Rank).First(g => g.Count() == 3).Key;
            return new CardCombo(ComboType.FullHouse, cards, ((int)threeRank - 3) * 4);
        }

        if (flush)
        {
            // Suit of flush determines order between flushes first,
            // then highest card within that suit.
            int flushValue = (int)cards[0].Suit * 1000 + cards.Max(c => c.Value);
            return new CardCombo(ComboType.Flush, cards, flushValue);
        }

        if (straight)
            return new CardCombo(ComboType.Straight, cards, cards.Max(c => c.Value));

        return null;
    }

    private static bool IsStraight(List<Card> cards)
    {
        var sorted = cards.Select(c => (int)c.Rank).OrderBy(r => r).ToList();
        for (int i = 1; i < sorted.Count; i++)
            if (sorted[i] - sorted[i - 1] != 1) return false;
        return true;
    }

    private static bool IsFullHouse(List<Card> cards)
    {
        var groups = cards.GroupBy(c => c.Rank).OrderByDescending(g => g.Count()).ToList();
        return groups.Count == 2 && groups[0].Count() == 3 && groups[1].Count() == 2;
    }

    private static bool IsFourOfAKind(List<Card> cards) =>
        cards.GroupBy(c => c.Rank).Any(g => g.Count() == 4);

    // ──────────────────────────────────────────────────────────────────────────
    // Move validation
    // ──────────────────────────────────────────────────────────────────────────

    /// <param name="play">The combo the current player wants to play.</param>
    /// <param name="lastPlay">The combo currently on the table (null / empty on a lead turn).</param>
    /// <param name="isLeadTurn">True when the player is leading (table is clear).</param>
    /// <param name="mustIncludeThreeDiamonds">True for the very first play of the game.</param>
    public bool IsValidPlay(
        CardCombo  play,
        CardCombo? lastPlay,
        bool       isLeadTurn,
        bool       mustIncludeThreeDiamonds)
    {
        if (mustIncludeThreeDiamonds)
        {
            if (!play.Cards.Any(c => c.Rank == Rank.Three && c.Suit == Suit.Diamonds))
                return false;
        }

        if (isLeadTurn || lastPlay is null)
            return true; // Any valid combo is fine when leading

        return play.Beats(lastPlay);
    }
}
