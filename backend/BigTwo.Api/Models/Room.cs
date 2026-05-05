namespace BigTwo.Api.Models;

public enum RoomStatus { Waiting, Playing, Finished }

public class Room
{
    public string       Code      { get; }
    public string       HostId    { get; }
    public List<Player> Players   { get; } = new();
    public RoomStatus   Status    { get; set; } = RoomStatus.Waiting;
    public GameState?   GameState { get; set; }

    public Room(string code, string hostId)
    {
        Code   = code;
        HostId = hostId;
    }

    public bool IsFull   => Players.Count >= 4;
    public bool CanStart => Players.Count >= 2 && Status == RoomStatus.Waiting;
}
