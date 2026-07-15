using Microsoft.Extensions.Hosting;

namespace AionLightning.Game.Services;

/// <summary>
/// Spawns every vortex location's peace-state garrison and arms the Brusthonin/Theobomos cron schedule
/// at boot (Java initVortexLocations — both no-op while VortexOptions.Enable is false). Registered after
/// CronServiceHostedService so the shared Quartz scheduler is already running.
/// </summary>
public sealed class VortexServiceHostedService(VortexService vortexService) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await vortexService.StartAllPeaceAsync(ct);
        await vortexService.ScheduleAsync(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
