using Microsoft.Extensions.Hosting;

namespace AionLightning.Game.Services;

/// <summary>Loads/seeds town level data once at boot, after schema migration creates the `towns` table
/// (see V46__towns.sql). Registered after SchemaMigrationHost.</summary>
public sealed class TownServiceHostedService(TownService townService) : IHostedService
{
    public Task StartAsync(CancellationToken ct) => townService.LoadAsync(ct);

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
