using System;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Commons.Services;
using AionLightning.Login.Configs;
using AionLightning.Login.Controller;
using AionLightning.Login.Dao;
using AionLightning.Login.Network;
using AionLightning.Login.Network.Ncrypt;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login;

public class LoginServer : IHostedService
{
    private readonly ILogger<LoginServer> _log;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly NetConnector _netConnector;

    public LoginServer(ILogger<LoginServer> log, IHostApplicationLifetime appLifetime, NetConnector netConnector)
    {
        _log = log;
        _appLifetime = appLifetime;
        _netConnector = netConnector;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _appLifetime.ApplicationStarted.Register(OnStarted);
        _appLifetime.ApplicationStopping.Register(OnStopping);
        _appLifetime.ApplicationStopped.Register(OnStopped);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void OnStarted()
    {
        _log.LogInformation("Login Server starting...");

        var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // AEInfos.PrintSection("Configs");
        // Config.Load();

        // AEInfos.PrintSection("DataBase");
        // DatabaseFactory.Init(Config.DATABASE_CONFIG);
        // DAOManager.Init();

        // AEInfos.PrintSection("Threads");
        ThreadPoolManager.GetInstance();

        // AEInfos.PrintSection("CronService");
        // CronService.Init(new ThreadPoolManagerRunnableRunner());

        // AEInfos.PrintSection("Tasks");
        // TaskFromDBManager.GetInstance();

        // AEInfos.PrintSection("DeadLockDetector");
        // new DeadLockDetector(60).Start();

        _log.LogInformation("KeyGen Init");
        try
        {
            KeyGen.Init();
        }
        catch (Exception e)
        {
            _log.LogError(e, "Failed to generate blowfish keys.");
            Environment.Exit(1); // Use a non-zero exit code for errors
        }

        _log.LogInformation("BannedIp Load");
        BannedIpController.GetInstance().Load();

        // AEInfos.PrintSection("PremiumController");
        // PremiumController.GetInstance().Load();
        // DAOManager.GetDAO<BannedMacDAO>().Cleanup();

        _log.LogInformation("GameServers Load");
        GameServerTable.Load();

        _log.LogInformation("NetWork Connect");
        _netConnector.Connect();

        // AEInfos.PrintSection("Services");
        // PlayerTransferService.GetInstance();

        // AEInfos.PrintSection("Shutdown");
        // Shutdown is handled by the host

        _log.LogInformation($"Login Server started in {(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start) / 1000} seconds.");

        _log.LogInformation("Waiting for connections...");
    }

    private void OnStopping()
    {
        _log.LogInformation("Login Server stopping...");
        // Shutdown.GetInstance().Run();
    }

    private void OnStopped()
    {
        _log.LogInformation("Login Server stopped.");
    }
}
