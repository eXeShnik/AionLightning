using System.Net.Sockets;
using AionLightning.Commons.Network;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.GameServer;

public sealed class GsConnectionFactory : IConnectionFactory<GsConnection>
{
    private readonly ILogger<GsConnection> _log;

    public GsConnectionFactory(ILogger<GsConnection> log)
    {
        _log = log;
    }

    public GsConnection Create(Socket socket, CancellationToken ct)
    {
        return new GsConnection(socket, _log);
    }
}
