using System.Net.Sockets;

namespace AionLightning.Commons.Network
{
    public interface IConnectionFactory
    {
        AConnection Create(Socket socket, IDispatcher dispatcher);
    }
}
