using Microsoft.Extensions.Hosting;

namespace AionLightning.Game.Services;

/// <summary>
/// Arms the rift open/close cron schedule at boot (Java RiftService.initRifts — a no-op while
/// RiftOptions.Enable is false). Registered after CronServiceHostedService so the shared Quartz
/// scheduler is already running.
/// </summary>
public sealed class RiftServiceHostedService(RiftService riftService) : IHostedService
{
    public Task StartAsync(CancellationToken ct) => riftService.ScheduleRiftsAsync(ct);

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
