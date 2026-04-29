using System.Net.Sockets;
using AionLightning.Commons.Network;
using AionLightning.Login.Network.Factories;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.GameServer;

public sealed class GsConnectionFactory : IConnectionFactory<GsConnection>
{
    private readonly ILogger<GsConnection> _log;
    private readonly GsPacketHandlerFactory _factory;

    public GsConnectionFactory(ILogger<GsConnection> log, GsPacketHandlerFactory factory)
    {
        _log = log;
        _factory = factory;
    }

    public GsConnection Create(Socket socket, CancellationToken ct)
    {
        return new GsConnection(socket, _log, _factory);
    }
}
