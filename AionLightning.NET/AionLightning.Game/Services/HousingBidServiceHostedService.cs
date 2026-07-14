using Microsoft.Extensions.Hosting;

namespace AionLightning.Game.Services;

/// <summary>
/// Loads persisted house bids and correlates them to houses once at boot (Java
/// <c>HousingBidService.start()</c>), then arms the auction-close cron (Java <c>initSieges</c>-equivalent
/// scheduling — a no-op while <see cref="Configs.Options.HousingOptions.Enable"/> is false; see
/// <see cref="HousingBidService.ScheduleAuctionCron"/>). Registered after
/// <see cref="HousingServiceHostedService"/> (bids correlate to houses loaded there) and after
/// <see cref="CronServiceHostedService"/> (the shared Quartz scheduler must already be running).
/// </summary>
public sealed class HousingBidServiceHostedService(HousingBidService bidService) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await bidService.LoadAsync(ct);
        await bidService.ScheduleAuctionCron(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
