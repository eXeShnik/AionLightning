using AionLightning.Login.Configs.Options;
using AionLightning.Login.Network.GameServer;
using AionLightning.Login.Network.GameServer.ServerPackets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Login;

public sealed class GsPingService : BackgroundService
{
    private readonly ILogger<GsPingService> _log;
    private readonly PingPongOptions _options;

    public GsPingService(ILogger<GsPingService> log, IOptions<PingPongOptions> options)
    {
        _log = log;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_options.Enabled) return;

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_options.DelayMs));
        while (await timer.WaitForNextTickAsync(ct))
        {
            foreach (var gsi in GameServerTable.GetGameServers())
            {
                var gs = gsi.GscHandler;
                if (gs?.State == GsConnection.GsState.AUTHED)
                    _ = gs.SendAsync(new SM_PING(), ct).AsTask();
            }
        }
    }
}
