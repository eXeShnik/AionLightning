using Microsoft.Extensions.Hosting;

namespace AionLightning.Game.Services;

/// <summary>
/// Arms the limited-item restock crons at boot (Java <c>LimitedItemTradeService.start()</c>, invoked
/// once from <c>GameServer</c> startup). Registered after <see cref="CronServiceHostedService"/> so the
/// shared Quartz scheduler is already running.
/// </summary>
public sealed class LimitedItemTradeServiceHostedService(LimitedItemTradeService limitedItemTradeService) : IHostedService
{
    public Task StartAsync(CancellationToken ct) => limitedItemTradeService.StartAsync();

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
