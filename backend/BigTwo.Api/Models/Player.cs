namespace BigTwo.Api.Models;

public class Player
{
    public string      Id           { get; }
    public string      ConnectionId { get; set; }
    public string      Nickname     { get; }
    public string      SessionToken { get; }  // Secret token used by the client to reconnect after a refresh
    public List<Card>  Hand         { get; set; } = new();
    public bool        IsConnected  { get; set; } = true;

    public Player(string id, string connectionId, string nickname, string sessionToken)
    {
        Id           = id;
        ConnectionId = connectionId;
        Nickname     = nickname;
        SessionToken = sessionToken;
    }
}
