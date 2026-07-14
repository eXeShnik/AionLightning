using Microsoft.Extensions.Hosting;

namespace AionLightning.Game.Services;

/// <summary>
/// Arms the housing maintenance/rent cron (Java <c>MaintenanceTask</c>'s static-singleton construction) —
/// a no-op while <see cref="Configs.Options.HousingOptions.Enable"/> is false; see
/// <see cref="MaintenanceTask.ScheduleMaintenanceCron"/>. Registered after
/// <see cref="HousingBidServiceHostedService"/> (a due house may need to be put up for auction) and after
/// <see cref="CronServiceHostedService"/> (the shared Quartz scheduler must already be running).
/// </summary>
public sealed class MaintenanceTaskHostedService(MaintenanceTask maintenanceTask) : IHostedService
{
    public Task StartAsync(CancellationToken ct) => maintenanceTask.ScheduleMaintenanceCron(ct);

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
