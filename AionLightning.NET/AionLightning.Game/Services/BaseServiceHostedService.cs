using Microsoft.Extensions.Hosting;

namespace AionLightning.Game.Services;

/// <summary>
/// Loads persisted base ownership from the DB once at boot, after schema migration has created the
/// `bases` table (Java BaseService.initBaseLocations+BaseDAO.LoadBases — data/persistence always load in
/// this port; only spawns/cron are gated, via BaseOptions.Enable), starts every base's garrison (Java
/// initBases()), then arms the assault cron (Java's per-base randomized Base.delayedAssault, approximated
/// as a fixed cron interval — see BaseService.ScheduleBasesAsync/TriggerAssaultsAsync). Registered after
/// SchemaMigrationHost and CronServiceHostedService so both the `bases` table and the shared Quartz
/// scheduler are ready before this runs.
/// </summary>
public sealed class BaseServiceHostedService(BaseService baseService) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await baseService.LoadPersistedOwnershipAsync(ct);
        await baseService.StartAllAsync(ct);
        await baseService.ScheduleBasesAsync(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
