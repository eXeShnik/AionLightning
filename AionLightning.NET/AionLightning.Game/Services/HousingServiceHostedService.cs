using Microsoft.Extensions.Hosting;

namespace AionLightning.Game.Services;

/// <summary>
/// Loads persisted house ownership from the DB once at boot, after schema migration has created the
/// houses table (Java HousingService's constructor-time load, minus the singleton — data/persistence
/// always load in this port; only the client broadcast is gated, via HousingOptions.Enable).
/// </summary>
public sealed class HousingServiceHostedService(HousingService housingService) : IHostedService
{
    public Task StartAsync(CancellationToken ct) => housingService.LoadAsync(ct);

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
