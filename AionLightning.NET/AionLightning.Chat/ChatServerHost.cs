using System.Net;
using System.Net.Sockets;
using AionLightning.Chat.Configs.Options;
using AionLightning.Chat.Network.Aion;
using AionLightning.Chat.Network.Gs;
using AionLightning.Commons.Network;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Chat;

public sealed class ChatServerHost : BackgroundService
{
    private readonly ILogger<ChatServerHost> _log;
    private readonly ChatNetworkOptions _opts;
    private readonly IConnectionFactory<GsConnection> _gsFactory;
    private readonly IConnectionFactory<AionClientConnection> _aionFactory;

    public ChatServerHost(
        ILogger<ChatServerHost> log,
        IOptions<ChatNetworkOptions> opts,
        IConnectionFactory<GsConnection> gsFactory,
        IConnectionFactory<AionClientConnection> aionFactory)
    {
        _log      = log;
        _opts     = opts.Value;
        _gsFactory = gsFactory;
        _aionFactory = aionFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var gsTask   = RunListenerAsync(_opts.BindAddress, _opts.GameServerPort,   _gsFactory,   "GameServer", ct);
        var aionTask = RunListenerAsync(_opts.BindAddress, _opts.ClientPort, _aionFactory, "AionClient",  ct);
        await Task.WhenAll(gsTask, aionTask);
    }

    private async Task RunListenerAsync<TConn>(
        string bindAddress, int port,
        IConnectionFactory<TConn> factory, string label,
        CancellationToken ct) where TConn : AConnection
    {
        var endpoint = new IPEndPoint(IPAddress.Parse(bindAddress), port);
        var listener = new TcpListener(endpoint);
        listener.Start();
        _log.LogInformation("Chat {Label} listener on {Endpoint}", label, endpoint);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                Socket socket = await listener.AcceptSocketAsync(ct);
                var conn = factory.Create(socket, ct);
                _ = conn.RunAsync(ct).ContinueWith(
                    t => _log.LogError(t.Exception!.GetBaseException(), "Chat {Label} connection error", label),
                    ct, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            listener.Stop();
            _log.LogInformation("Chat {Label} listener stopped", label);
        }
    }
}
