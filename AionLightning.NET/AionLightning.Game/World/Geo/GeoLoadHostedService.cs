using Microsoft.Extensions.Hosting;

namespace AionLightning.Game.World.Geo;

/// <summary>
/// Loads the real geo engine's mesh library and every available world's geodata at startup.
/// Only registered when <c>GeoData:Enable</c> is true (see Program.cs) — the dummy engine needs
/// no loading step.
/// </summary>
public sealed class GeoLoadHostedService(RealGeoService geoService) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => geoService.LoadAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
