using Microsoft.Extensions.Hosting;

namespace AionLightning.Game.Services;

/// <summary>
/// Loads persisted siege ownership (race/legion) from the DB once at boot, after schema migration has
/// created the siege_locations table (Java SiegeService.initSiegeLocations, minus the
/// SiegeConfig.SIEGE_ENABLED gate — data/persistence always load in this port; only the client
/// broadcasts are gated, via SiegeOptions.Enable), then arms the siege cron schedule (Java
/// SiegeService.initSieges — a no-op while SiegeOptions.Enable is false). Registered after
/// SchemaMigrationHost and CronServiceHostedService so both the siege_locations table and the shared
/// Quartz scheduler are ready before this runs.
/// </summary>
public sealed class SiegeServiceHostedService(SiegeService siegeService) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await siegeService.LoadPersistedOwnershipAsync(ct);
        await siegeService.ScheduleSieges(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
