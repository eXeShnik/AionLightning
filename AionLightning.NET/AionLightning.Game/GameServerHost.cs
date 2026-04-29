using System.Net;
using System.Net.Sockets;
using AionLightning.Commons.Events;
using AionLightning.Commons.Network;
using AionLightning.Commons.Scripting.Contracts;
using AionLightning.Commons.Services;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Cs;
using AionLightning.Game.Network.Ls;
using AionLightning.Game.Scripting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Game;

public sealed class GameServerHost : BackgroundService
{
    private readonly ILogger<GameServerHost> _log;
    private readonly ILoggerFactory _loggerFactory;
    private readonly LsConnectionOptions _lsOpts;
    private readonly CsConnectionOptions _csOpts;
    private readonly NetworkOptions _networkOpts;
    private readonly GameServerInfoOptions _gsInfo;
    private readonly LsPacketHandlerFactory _lsFactory;
    private readonly LsConnectionHolder _lsHolder;
    private readonly CsPacketHandlerFactory _csFactory;
    private readonly CsConnectionHolder _csHolder;
    private readonly IConnectionFactory<GsClientConnection> _aionFactory;
    private readonly ScriptService _scriptService;
    private readonly IEventBus _eventBus;

    public GameServerHost(
        ILogger<GameServerHost> log,
        ILoggerFactory loggerFactory,
        IOptions<LsConnectionOptions> lsOpts,
        IOptions<CsConnectionOptions> csOpts,
        IOptions<NetworkOptions> networkOpts,
        IOptions<GameServerInfoOptions> gsInfo,
        LsPacketHandlerFactory lsFactory,
        LsConnectionHolder lsHolder,
        CsPacketHandlerFactory csFactory,
        CsConnectionHolder csHolder,
        IConnectionFactory<GsClientConnection> aionFactory,
        ScriptService scriptService,
        IEventBus eventBus)
    {
        _log           = log;
        _loggerFactory = loggerFactory;
        _lsOpts        = lsOpts.Value;
        _csOpts        = csOpts.Value;
        _networkOpts   = networkOpts.Value;
        _gsInfo        = gsInfo.Value;
        _lsFactory     = lsFactory;
        _lsHolder      = lsHolder;
        _csFactory     = csFactory;
        _csHolder      = csHolder;
        _aionFactory   = aionFactory;
        _scriptService = scriptService;
        _eventBus      = eventBus;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await LoadScriptsAsync(ct);

        var lsTask   = RunLsConnectionLoopAsync(ct);
        var csTask   = RunCsConnectionLoopAsync(ct);
        var aionTask = RunAionListenerAsync(ct);
        await Task.WhenAll(lsTask, csTask, aionTask);
    }

    private async Task LoadScriptsAsync(CancellationToken ct)
    {
        var scriptsFolder = Path.Combine(AppContext.BaseDirectory, "Scripts");
        if (!Directory.Exists(scriptsFolder))
        {
            _log.LogInformation("Scripts folder not found at {Path} — scripting disabled", scriptsFolder);
            return;
        }

        IScriptHost host = new GameScriptHost(
            _eventBus,
            _loggerFactory.CreateLogger<IScript>());

        await _scriptService.LoadAllAsync(scriptsFolder, host, ct);

        // Hot-reload watcher (fire-and-forget per-reload)
        var watcher = new FolderListenerService(
            scriptsFolder,
            _loggerFactory.CreateLogger<FolderListenerService>(),
            "*.cs");

        watcher.Changed += async (_, e) =>
        {
            try { await _scriptService.ReloadAsync(e.FullPath, host, CancellationToken.None); }
            catch (Exception ex) { _log.LogError(ex, "Script reload failed: {Path}", e.FullPath); }
        };

        // Keep watcher alive for the lifetime of the host
        ct.Register(() => watcher.Dispose());
    }

    private async Task RunLsConnectionLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync(_lsOpts.Host, _lsOpts.Port, ct);

                var conn = new LsConnection(
                    tcp.Client,
                    _loggerFactory.CreateLogger<LsConnection>(),
                    _lsFactory,
                    _gsInfo);

                _lsHolder.Current = conn;
                _log.LogInformation("Connected to LoginServer at {Host}:{Port}", _lsOpts.Host, _lsOpts.Port);

                await conn.RunAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "LS connection lost — reconnecting in {Ms}ms", _lsOpts.ReconnectDelayMs);
            }
            finally
            {
                _lsHolder.Current = null;
            }

            if (!ct.IsCancellationRequested)
                await Task.Delay(_lsOpts.ReconnectDelayMs, ct).ConfigureAwait(false);
        }
    }

    private async Task RunCsConnectionLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync(_csOpts.Host, _csOpts.Port, ct);

                var conn = new CsConnection(
                    tcp.Client,
                    _loggerFactory.CreateLogger<CsConnection>(),
                    _csFactory,
                    _gsInfo,
                    _csOpts);

                _csHolder.Current = conn;
                _log.LogInformation("Connected to ChatServer at {Host}:{Port}", _csOpts.Host, _csOpts.Port);

                await conn.RunAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "CS connection lost — reconnecting in {Ms}ms", _csOpts.ReconnectDelayMs);
            }
            finally
            {
                _csHolder.Current = null;
            }

            if (!ct.IsCancellationRequested)
                await Task.Delay(_csOpts.ReconnectDelayMs, ct).ConfigureAwait(false);
        }
    }

    private async Task RunAionListenerAsync(CancellationToken ct)
    {
        var endpoint = new IPEndPoint(IPAddress.Parse(_networkOpts.BindAddress), _networkOpts.GamePort);
        var listener = new TcpListener(endpoint);
        listener.Start();
        _log.LogInformation("Aion GameServer listening on {Endpoint}", endpoint);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                Socket socket = await listener.AcceptSocketAsync(ct);
                var conn = _aionFactory.Create(socket, ct);
                _ = conn.RunAsync(ct).ContinueWith(
                    t => _log.LogError(t.Exception!.GetBaseException(), "Aion client connection error"),
                    ct, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            listener.Stop();
            _log.LogInformation("Aion listener stopped");
        }
    }
}
