using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace AionLightning.Login;

// Runs schema migrations synchronously in StartAsync so that LoginServerHost
// cannot begin accepting connections until the schema is ready.
public sealed class SchemaMigrationHost(
    IConfiguration config,
    ILogger<SchemaMigrationHost> log) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var cs = config.GetConnectionString("LoginDb")
            ?? throw new InvalidOperationException("ConnectionStrings:LoginDb is required");

        await using var conn = new MySqlConnection(cs);
        await conn.OpenAsync(ct);

        var sqlPath = Path.Combine(AppContext.BaseDirectory, "Sql", "login");
        var evolve = new EvolveDb.Evolve(conn, msg => log.LogInformation("Evolve: {Message}", msg))
        {
            Locations = [sqlPath],
            IsEraseDisabled = true,
        };
        evolve.Migrate();

        log.LogInformation("Schema migration completed.");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
