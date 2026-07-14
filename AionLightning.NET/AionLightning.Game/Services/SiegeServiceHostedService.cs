using Microsoft.Extensions.Hosting;

namespace AionLightning.Game.Services;

/// <summary>
/// Loads persisted siege ownership (race/legion) from the DB once at boot, after schema migration has
/// created the siege_locations table (Java SiegeService.initSiegeLocations, minus the
/// SiegeConfig.SIEGE_ENABLED gate — data/persistence always load in this port; only the client
/// broadcasts are gated, via SiegeOptions.Enable).
/// </summary>
public sealed class SiegeServiceHostedService(SiegeService siegeService) : IHostedService
{
    public Task StartAsync(CancellationToken ct) => siegeService.LoadPersistedOwnershipAsync(ct);

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
