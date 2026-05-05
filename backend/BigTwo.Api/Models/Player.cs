namespace BigTwo.Api.Models;

public class Player
{
    public string      Id           { get; }
    public string      ConnectionId { get; set; }
    public string      Nickname     { get; }
    public List<Card>  Hand         { get; set; } = new();
    public bool        IsConnected  { get; set; } = true;

    public Player(string id, string connectionId, string nickname)
    {
        Id           = id;
        ConnectionId = connectionId;
        Nickname     = nickname;
    }
}
