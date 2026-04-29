using System.Net.Sockets;
using AionLightning.Commons.Network;
using AionLightning.Login.Network.Factories;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.Aion;

public sealed class AionConnectionFactory : IConnectionFactory<LoginConnection>
{
    private readonly ILogger<LoginConnection> _log;
    private readonly AionPacketHandlerFactory _factory;

    public AionConnectionFactory(ILogger<LoginConnection> log, AionPacketHandlerFactory factory)
    {
        _log = log;
        _factory = factory;
    }

    public LoginConnection Create(Socket socket, CancellationToken ct)
    {
        return new LoginConnection(socket, _log, _factory);
    }
}
