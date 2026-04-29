using System.Net.Sockets;
using AionLightning.Commons.Network;
using Microsoft.Extensions.Logging;

namespace AionLightning.Chat.Network.Gs;

public sealed class GsConnectionFactory : IConnectionFactory<GsConnection>
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly GsPacketHandlerFactory _factory;

    public GsConnectionFactory(ILoggerFactory loggerFactory, GsPacketHandlerFactory factory)
    {
        _loggerFactory = loggerFactory;
        _factory = factory;
    }

    public GsConnection Create(Socket socket, CancellationToken ct)
        => new(socket, _loggerFactory.CreateLogger<GsConnection>(), _factory);
}
