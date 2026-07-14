using Microsoft.Extensions.Hosting;

namespace AionLightning.Game.Services;

/// <summary>
/// Loads persisted house ownership from the DB once at boot, after schema migration has created the
/// houses table (Java HousingService's constructor-time load, minus the singleton — data/persistence
/// always load in this port; only the client broadcast is gated, via HousingOptions.Enable), then spawns
/// every house into the world (P2 — see HousingService.SpawnHouses, itself a no-op unless
/// HousingOptions.Enable is true), then loads every house's decoration scripts (P5 — see
/// HousingService.LoadHouseScriptsAsync) and each owned house's placed furniture/custom decorations
/// (P3 — see HousingService.LoadRegisteredItemsAsync). Runs before HousingBidServiceHostedService so bid
/// data resolves against a fully-populated house set.
/// </summary>
public sealed class HousingServiceHostedService(HousingService housingService) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await housingService.LoadAsync(ct);
        housingService.SpawnHouses();
        await housingService.LoadHouseScriptsAsync(ct);
        await housingService.LoadRegisteredItemsAsync(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
