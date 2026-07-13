using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>
/// Sweeps every live instance channel on a fixed tick and destroys the empty ones (Java
/// <c>EmptyInstanceCheckerTask</c>, one sweeper instead of one task per channel). A solo channel is
/// destroyed <see cref="InstanceService.SoloDestroyDelayMs"/> after it is first seen empty; a
/// team-registered channel is destroyed as soon as none of its registered members are online inside.
/// </summary>
public sealed class EmptyInstanceCheckerService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(60);

    private readonly World.InstanceRegistry _registry;
    private readonly InstanceService _instanceService;
    private readonly ILogger<EmptyInstanceCheckerService> _log;

    public EmptyInstanceCheckerService(World.InstanceRegistry registry, InstanceService instanceService,
        ILogger<EmptyInstanceCheckerService> log)
    {
        _registry        = registry;
        _instanceService = instanceService;
        _log             = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Give the server time to finish booting before the first sweep (Java initial delay 150s).
        try { await Task.Delay(TimeSpan.FromSeconds(150), ct); } catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(TickInterval);
        while (await timer.WaitForNextTickAsync(ct))
        {
            try { Sweep(); }
            catch (Exception ex) { _log.LogError(ex, "EmptyInstanceChecker sweep failed"); }
        }
    }

    private void Sweep()
    {
        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        foreach (var instance in _registry.All().ToList())
        {
            int inside = _instanceService.PlayersInside(instance);

            if (instance.IsTeamRegistered)
            {
                // Team channel: destroy as soon as nobody registered is inside.
                if (inside == 0) _instanceService.DestroyInstance(instance);
                continue;
            }

            // Solo channel: start (or continue) the 10-minute empty countdown.
            if (inside > 0)
            {
                instance.EmptyDestroyDueUtcMs = null;
                continue;
            }

            instance.EmptyDestroyDueUtcMs ??= nowMs + InstanceService.SoloDestroyDelayMs;
            if (nowMs >= instance.EmptyDestroyDueUtcMs.Value)
                _instanceService.DestroyInstance(instance);
        }
    }
}
