using Microsoft.Extensions.Hosting;

namespace AionLightning.Game.Services;

/// <summary>
/// Arms the serial-killer victim-count decay cron at boot (Java SerialKillerService.initSerialKillers —
/// a no-op while SerialKillerOptions.Enabled is false). Registered after CronServiceHostedService so
/// the shared Quartz scheduler is already running.
/// </summary>
public sealed class SerialKillerServiceHostedService(SerialKillerService serialKillerService) : IHostedService
{
    public Task StartAsync(CancellationToken ct) => serialKillerService.ScheduleDecayAsync(ct);

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
