namespace BigTwo.Api.Models;

/// <summary>
/// Five-card hand type hierarchy (higher int = stronger type).
/// Single/Pair/Triple are also represented for easy typing.
/// </summary>
public enum ComboType
{
    Single       = 1,
    Pair         = 2,
    Triple       = 3,
    Straight     = 5,
    Flush        = 6,
    FullHouse    = 7,
    FourOfAKind  = 8,
    StraightFlush = 9
}

public class CardCombo
{
    public ComboType    Type       { get; }
    public List<Card>   Cards      { get; }
    /// <summary>Comparison value within the same ComboType.</summary>
    public int          ComboValue { get; }

    public CardCombo(ComboType type, List<Card> cards, int comboValue)
    {
        Type       = type;
        Cards      = cards;
        ComboValue = comboValue;
    }

    /// <summary>
    /// Returns true if this combo legally beats <paramref name="other"/>.
    /// Rules:
    ///  – Card count must match (5-card combos can only beat 5-card combos).
    ///  – For 5-card combos a higher type always beats a lower type.
    ///  – Same type: compare ComboValue (higher wins).
    /// </summary>
    public bool Beats(CardCombo other)
    {
        if (Cards.Count != other.Cards.Count) return false;

        if (Cards.Count == 5)
        {
            if (Type != other.Type) return (int)Type > (int)other.Type;
            return ComboValue > other.ComboValue;
        }

        // Singles, pairs, triples: same type required
        if (Type != other.Type) return false;
        return ComboValue > other.ComboValue;
    }
}
