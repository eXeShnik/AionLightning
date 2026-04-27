using System.Net.Sockets;
using AionLightning.Commons.Network;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.Aion;

public sealed class AionConnectionFactory : IConnectionFactory<LoginConnection>
{
    private readonly ILogger<LoginConnection> _log;

    public AionConnectionFactory(ILogger<LoginConnection> log)
    {
        _log = log;
    }

    public LoginConnection Create(Socket socket, CancellationToken ct)
    {
        return new LoginConnection(socket, _log);
    }
}
