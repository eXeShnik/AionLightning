using System.Net.Sockets;

namespace AionLightning.Commons.Network;

public interface IConnectionFactory<TConnection> where TConnection : AConnection
{
    TConnection Create(Socket socket, CancellationToken ct);
}
