namespace BigTwo.Api.Services;

/// <summary>
/// Background service that periodically removes stale rooms
/// (inactive for more than 30 minutes) to prevent memory leaks.
/// </summary>
public class RoomCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxAge   = TimeSpan.FromMinutes(30);

    private readonly RoomService _rooms;
    private readonly ILogger<RoomCleanupService> _logger;

    public RoomCleanupService(RoomService rooms, ILogger<RoomCleanupService> logger)
    {
        _rooms  = rooms;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken);

            var removed = _rooms.RemoveStaleRooms(MaxAge);
            if (removed > 0)
                _logger.LogInformation("Room cleanup: removed {Count} stale room(s)", removed);
        }
    }
}
