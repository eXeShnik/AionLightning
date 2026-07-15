using AionLightning.Game.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>
/// Loads persisted announcements and arms one repeating broadcast loop per row (Java
/// <c>AnnouncementService.load()</c>). No-ops gracefully when the table is empty (fresh DB). Cancelling at
/// shutdown mirrors Java's <c>reload()</c>, which cancelled every scheduled <c>Future</c> before reloading.
/// </summary>
public sealed class AnnouncementServiceHostedService(
    AnnouncementService announcementService,
    ILogger<AnnouncementServiceHostedService> log) : IHostedService
{
    private readonly List<Task> _loops = new();
    private CancellationTokenSource? _cts;

    public async Task StartAsync(CancellationToken ct)
    {
        var announcements = await announcementService.LoadAsync(ct);
        if (announcements.Count == 0)
            return;

        _cts = new CancellationTokenSource();
        foreach (var announcement in announcements)
            _loops.Add(RunLoopAsync(announcement, _cts.Token));
    }

    public async Task StopAsync(CancellationToken ct)
    {
        if (_cts is null)
            return;

        _cts.Cancel();
        try { await Task.WhenAll(_loops); }
        catch (OperationCanceledException) { /* expected on shutdown */ }
    }

    /// <summary>Java <c>ThreadPoolManager.scheduleAtFixedRate(runnable, delay * 1000, delay * 1000)</c> —
    /// first broadcast fires after <see cref="Announcement.DelaySeconds"/>, then repeats at that interval.</summary>
    private async Task RunLoopAsync(Announcement announcement, CancellationToken ct)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, announcement.DelaySeconds));
        using var timer = new PeriodicTimer(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
                await announcementService.BroadcastAsync(announcement, ct);
        }
        catch (OperationCanceledException) { /* expected on shutdown */ }
        catch (Exception e)
        {
            log.LogError(e, "Announcement loop for id {AnnouncementId} faulted", announcement.Id);
        }
    }
}
