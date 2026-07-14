using Microsoft.Extensions.Hosting;

namespace AionLightning.Game.Services;

/// <summary>Rolls the initial weather for every map at boot, then arms the rotation cron. Registered
/// after CronServiceHostedService so the shared Quartz scheduler is already running.</summary>
public sealed class WeatherServiceHostedService(WeatherService weatherService) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        weatherService.Initialize();
        await weatherService.ScheduleAsync(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
