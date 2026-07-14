using AionLightning.Commons.Services;
using Microsoft.Extensions.Hosting;

namespace AionLightning.Game.Services;

/// <summary>
/// Starts/stops the shared <see cref="CronService"/> alongside the rest of the game server (Java
/// initialized its CronService singleton once at boot via <c>CronService.initSingleton(...)</c>).
/// Consumers (siege scheduling, housing auction/maintenance) inject <see cref="CronService"/> directly
/// and call <see cref="CronService.Schedule"/> once it has started.
/// </summary>
public sealed class CronServiceHostedService(CronService cronService) : IHostedService
{
    public Task StartAsync(CancellationToken ct) => cronService.StartAsync(ct);

    public Task StopAsync(CancellationToken ct) => cronService.StopAsync(ct);
}
