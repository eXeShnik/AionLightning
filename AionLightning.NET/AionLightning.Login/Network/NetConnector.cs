using AionLightning.Login.Configs.Options;
using AionLightning.Login.Network.Aion;
using AionLightning.Login.Network.GameServer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Login.Network;

/// <summary>
/// Wires up TcpListener-based accept loops for both Aion-client and GameServer ports.
/// TODO M2: replace stub with real TcpListener accept loop using AionConnectionFactory / GsConnectionFactory.
/// </summary>
public sealed class NetConnector
{
    private readonly ILogger<NetConnector> _logger;
    private readonly NetworkOptions _net;

    public NetConnector(
        ILogger<NetConnector> logger,
        IOptions<NetworkOptions> net)
    {
        _logger = logger;
        _net = net.Value;
    }

    public void Connect()
    {
        _logger.LogInformation(
            "NetConnector ready (stub) — Aion port {ClientPort}, GS port {GsPort}",
            _net.ClientPort, _net.GameServerPort);
    }
}
