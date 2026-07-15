using Microsoft.Extensions.Hosting;

namespace AionLightning.Game.Services;

/// <summary>
/// Java EventService's static init (activeEvents seeded from DataManager.EVENT_DATA.getActiveEvents() at
/// construction) + start() — runs one <see cref="EventService.CheckEvents"/> pass at boot so any event
/// already inside its date window is started immediately, then arms the ~5-minute recheck cron. Registered
/// after CronServiceHostedService so the shared Quartz scheduler is already running.
/// </summary>
public sealed class EventServiceHostedService(EventService eventService) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        eventService.CheckEvents();
        await eventService.ScheduleAsync(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
