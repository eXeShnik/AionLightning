using Microsoft.Extensions.Hosting;

namespace AionLightning.Game.Services;

/// <summary>
/// Arms the veteran-reward minute sweep at boot (Java <c>VeteranRewardsService.Init_VeteranRewardStatusLoop</c>).
/// Registered after <see cref="CronServiceHostedService"/> so the shared Quartz scheduler is already running.
/// </summary>
public sealed class VeteranRewardServiceHostedService(VeteranRewardService veteranRewardService) : IHostedService
{
    public Task StartAsync(CancellationToken ct) => veteranRewardService.ScheduleAsync(ct);

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
