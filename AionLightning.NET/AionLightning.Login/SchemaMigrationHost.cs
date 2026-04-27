using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace AionLightning.Login;

/// <summary>
/// Runs Evolve schema migration on the LoginDb before other hosted services start.
/// Registration order in Program.cs guarantees this runs first.
/// </summary>
public sealed class SchemaMigrationHost(
    IConfiguration config,
    ILogger<SchemaMigrationHost> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var cs = config.GetConnectionString("LoginDb")
            ?? throw new InvalidOperationException("ConnectionStrings:LoginDb is required");

        await using var conn = new MySqlConnection(cs);
        await conn.OpenAsync(ct);

        var evolve = new EvolveDb.Evolve(conn, msg => log.LogInformation("Evolve: {Message}", msg))
        {
            Locations = ["Sql/login"],
            IsEraseDisabled = true,
        };
        evolve.Migrate();

        log.LogInformation("Schema migration completed.");
    }
}
