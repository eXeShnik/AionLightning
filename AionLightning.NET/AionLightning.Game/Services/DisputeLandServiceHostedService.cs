using Microsoft.Extensions.Hosting;

namespace AionLightning.Game.Services;

/// <summary>Arms DisputeLandService's cron windows at boot (no-op unless DisputeLandOptions.Enable is
/// true). Registered after CronServiceHostedService so the shared Quartz scheduler is already running.</summary>
public sealed class DisputeLandServiceHostedService(DisputeLandService disputeLandService) : IHostedService
{
    public Task StartAsync(CancellationToken ct) => disputeLandService.InitAsync();

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
