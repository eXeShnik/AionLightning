using System.Net;
using System.Net.Sockets;
using AionLightning.Commons.Network;
using AionLightning.Commons.Network.Ncrypt;
using AionLightning.Login.Configs.Options;
using AionLightning.Login.Controller;
using AionLightning.Login.Network.Aion;
using AionLightning.Login.Network.GameServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Login;

public sealed class LoginServerHost : BackgroundService
{
    private readonly ILogger<LoginServerHost> _log;
    private readonly NetworkOptions _net;
    private readonly BannedIpController _bannedIpCtrl;
    private readonly IConnectionFactory<LoginConnection> _aionFactory;
    private readonly IConnectionFactory<GsConnection> _gsFactory;
    private readonly IConfiguration _config;

    public LoginServerHost(
        ILogger<LoginServerHost> log,
        IOptions<NetworkOptions> net,
        BannedIpController bannedIpCtrl,
        IConnectionFactory<LoginConnection> aionFactory,
        IConnectionFactory<GsConnection> gsFactory,
        IConfiguration config)
    {
        _log = log;
        _net = net.Value;
        _bannedIpCtrl = bannedIpCtrl;
        _aionFactory = aionFactory;
        _gsFactory = gsFactory;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _log.LogInformation("Initializing RSA key pairs…");
        KeyGen.Init(_log);

        _log.LogInformation("Loading game server table from config…");
        var entries = _config.GetSection("LoginServer:GameServers").Get<GameServerEntry[]>()
                      ?? Array.Empty<GameServerEntry>();
        GameServerTable.LoadFromConfig(entries, _log);

        _log.LogInformation("Loading banned IPs…");
        await _bannedIpCtrl.StartAsync(ct);

        var aionEndpoint = new IPEndPoint(IPAddress.Parse(_net.BindAddress), _net.ClientPort);
        var gsEndpoint   = new IPEndPoint(IPAddress.Parse(_net.BindAddress), _net.GameServerPort);

        var aionListener = new TcpListener(aionEndpoint);
        var gsListener   = new TcpListener(gsEndpoint);

        aionListener.Start();
        gsListener.Start();

        _log.LogInformation("Aion client listener on {EP}", aionEndpoint);
        _log.LogInformation("GameServer listener on {EP}", gsEndpoint);

        var aionTask = AcceptLoopAsync(aionListener, _aionFactory, "Aion", ct);
        var gsTask   = AcceptLoopAsync(gsListener,   _gsFactory,   "GS",   ct);

        try { await Task.WhenAll(aionTask, gsTask); }
        finally
        {
            aionListener.Stop();
            gsListener.Stop();
            _log.LogInformation("All listeners stopped");
        }
    }

    private async Task AcceptLoopAsync<T>(TcpListener listener, IConnectionFactory<T> factory,
        string label, CancellationToken ct) where T : AConnection
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                Socket socket = await listener.AcceptSocketAsync(ct);
                _log.LogInformation("{Label} connection from {EP}", label, socket.RemoteEndPoint);
                var conn = factory.Create(socket, ct);
                _ = conn.RunAsync(ct).ContinueWith(
                    t => _log.LogError(t.Exception!.GetBaseException(), "{Label} connection error", label),
                    CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            }
        }
        catch (OperationCanceledException) { }
    }
}
