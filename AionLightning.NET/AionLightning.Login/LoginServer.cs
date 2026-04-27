using AionLightning.Commons.Network.Ncrypt;
using AionLightning.Login.Controller;
using AionLightning.Login.Network;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login;

public sealed class LoginServer : IHostedService
{
    private readonly ILogger<LoginServer> _log;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly NetConnector _netConnector;

    public LoginServer(ILogger<LoginServer> log, IHostApplicationLifetime lifetime, NetConnector netConnector)
    {
        _log = log;
        _lifetime = lifetime;
        _netConnector = netConnector;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _lifetime.ApplicationStarted.Register(OnStarted);
        _lifetime.ApplicationStopping.Register(OnStopping);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void OnStarted()
    {
        _log.LogInformation("LoginServer starting…");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        _log.LogInformation("Initializing RSA key pairs…");
        try
        {
            KeyGen.Init();
        }
        catch (Exception e)
        {
            _log.LogCritical(e, "Failed to generate RSA key pairs — aborting");
            _lifetime.StopApplication();
            return;
        }

        _log.LogInformation("Loading banned IPs… (TODO M3: inject BannedIpController via DI)");

        _log.LogInformation("Loading game server table…");
        GameServerTable.Load();

        _log.LogInformation("Starting network listeners…");
        _netConnector.Connect();

        _log.LogInformation("LoginServer ready in {Elapsed}ms — waiting for connections", sw.ElapsedMilliseconds);
    }

    private void OnStopping()
    {
        _log.LogInformation("LoginServer stopping…");
    }
}
