using System.Net.Sockets;

namespace AionLightning.Commons.Network
{
    public class Acceptor
    {
        private readonly IConnectionFactory _factory;
        private readonly NioServer _nioServer;

        public Acceptor(IConnectionFactory factory, NioServer nioServer)
        {
            _factory = factory;
            _nioServer = nioServer;
        }

        public void Accept(Socket socket)
        {
            var dispatcher = _nioServer.GetReadWriteDispatcher();
            var connection = _factory.Create(socket, dispatcher);

            if (connection == null)
            {
                return;
            }

            //dispatcher.Register(connection);
            connection.Initialized();
        }
    }
}
