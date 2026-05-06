using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace AionLightning.Game;

public sealed class SchemaMigrationHost(
    IConfiguration config,
    ILogger<SchemaMigrationHost> log) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var cs = config.GetConnectionString("GameDb")
            ?? throw new InvalidOperationException("ConnectionStrings:GameDb is required");

        await using var conn = new MySqlConnection(cs);
        await conn.OpenAsync(ct);

        var sqlPath = Path.Combine(AppContext.BaseDirectory, "Sql", "game");
        var evolve = new EvolveDb.Evolve(conn, msg => log.LogInformation("Evolve: {Message}", msg))
        {
            Locations = [sqlPath],
            IsEraseDisabled = true,
        };
        evolve.Migrate();

        log.LogInformation("Game schema migration completed.");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
