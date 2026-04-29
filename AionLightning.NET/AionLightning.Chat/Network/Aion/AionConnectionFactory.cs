using System.Net.Sockets;
using AionLightning.Commons.Network;
using Microsoft.Extensions.Logging;

namespace AionLightning.Chat.Network.Aion;

public sealed class AionConnectionFactory : IConnectionFactory<AionClientConnection>
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly AionPacketHandlerFactory _factory;

    public AionConnectionFactory(ILoggerFactory loggerFactory, AionPacketHandlerFactory factory)
    {
        _loggerFactory = loggerFactory;
        _factory = factory;
    }

    public AionClientConnection Create(Socket socket, CancellationToken ct)
        => new(socket, _loggerFactory.CreateLogger<AionClientConnection>(), _factory);
}
