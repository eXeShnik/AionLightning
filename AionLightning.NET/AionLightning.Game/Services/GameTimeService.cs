using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>
/// Port of Java <c>services.GameTimeService</c> — periodically re-broadcasts <see cref="SM_GAME_TIME"/>
/// to every online player so client-side day/night stays synced with the server clock over a long
/// session (the packet is otherwise only ever sent once, at login, by PlayerEnterWorldService). Java's
/// <c>GAMETIME_UPDATE</c> constant is 3 minutes.
///
/// Java also called <c>GameTimeManager.saveTime()</c> each tick to persist an adjustable game-time
/// offset; this port's <see cref="SM_GAME_TIME"/> instead computes the value live from
/// <c>DateTime.UtcNow</c> (see that packet's own doc comment), so there is no equivalent offset state to
/// save — this service stays DB-free by design.
/// </summary>
public sealed class GameTimeService : BackgroundService
{
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromMinutes(3);

    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ILogger<GameTimeService> _log;

    public GameTimeService(PlayerConnectionRegistry connRegistry, ILogger<GameTimeService> log)
    {
        _connRegistry = connRegistry;
        _log          = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _log.LogInformation("GameTimeService started. Update interval: {Interval}", UpdateInterval);

        using var timer = new PeriodicTimer(UpdateInterval);
        while (await timer.WaitForNextTickAsync(ct))
        {
            _log.LogInformation("Sending current game time to all players");
            var packet = new SM_GAME_TIME();
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer is not null)
                    try { await conn.SendAsync(packet, ct); } catch { }
        }
    }
}
