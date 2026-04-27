using AionLightning.Login.Configs.Options;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace AionLightning.Login;

/// <summary>
/// Temporary M1 smoke host — proves config + DB connectivity. Replaced by real server in M2.
/// </summary>
public sealed class SmokeHost(
    MySqlDataSource ds,
    IOptions<NetworkOptions> net,
    ILogger<SmokeHost> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        log.LogInformation("M1 smoke: NetworkOptions loaded BindAddress={Bind} ClientPort={Port}",
            net.Value.BindAddress, net.Value.ClientPort);

        await using var conn = await ds.OpenConnectionAsync(ct);
        var result = await conn.QuerySingleAsync<int>("SELECT 1");
        log.LogInformation("M1 smoke: SELECT 1 -> {Result}", result);

        log.LogInformation("M1 smoke: ready. Press Ctrl-C to stop.");

        try { await Task.Delay(Timeout.Infinite, ct); }
        catch (OperationCanceledException) { /* expected on Ctrl-C */ }
    }
}
