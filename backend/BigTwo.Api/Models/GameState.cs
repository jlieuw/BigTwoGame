namespace BigTwo.Api.Models;

public class GameState
{
    /// <summary>Index into Room.Players of who should act next.</summary>
    public int          CurrentPlayerIndex { get; set; }

    /// <summary>Cards that were most recently played on the table.</summary>
    public List<Card>   LastPlayedCards    { get; set; } = new();

    /// <summary>Player.Id of whoever last played cards (null at start of a new round).</summary>
    public string?      LastPlayerId       { get; set; }

    public ComboType?   LastComboType      { get; set; }

    /// <summary>How many consecutive passes have been made since the last play.</summary>
    public int          PassCount          { get; set; }

    /// <summary>True until the very first cards are played in the game.</summary>
    public bool         IsFirstTurn        { get; set; } = true;

    public bool         IsOver             { get; set; }
    public string?      WinnerId           { get; set; }

    public bool IsLeadTurn => LastPlayedCards.Count == 0;
}
