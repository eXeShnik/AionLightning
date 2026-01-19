using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Commons.Network
{
    public class AcceptDispatcherImpl : Dispatcher
    {
        private readonly ILogger<AcceptDispatcherImpl> _log;
        private Socket _serverSocket;
        private readonly Acceptor _acceptor;

        public AcceptDispatcherImpl(string name, Acceptor acceptor, ILogger<AcceptDispatcherImpl> log, ILogger<Dispatcher> dispatcherLog) : base(name, null, dispatcherLog)
        {
            _log = log;
            _acceptor = acceptor;
        }

        public void Bind(string host, int port)
        {
            try
            {
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _serverSocket.Bind(new IPEndPoint(IPAddress.Parse(host), port));
                _serverSocket.Listen(5);
                _serverSocket.BeginAccept(AcceptCallback, null);
            }
            catch (Exception e)
            {
                _log.LogError(e, "Failed to bind server socket");
                throw;
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                var clientSocket = _serverSocket.EndAccept(ar);
                _acceptor.Accept(clientSocket);
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }
            catch (Exception e)
            {
                _log.LogError(e, "Error in accept callback");
            }
            finally
            {
                if (_serverSocket.IsBound)
                    _serverSocket.BeginAccept(AcceptCallback, null);
            }
        }

        public void Shutdown()
        {
            _serverSocket.Close();
        }

        public override void Dispatch()
        {
            if (_serverSocket.Poll(1000, SelectMode.SelectRead))
            {
                var clientSocket = _serverSocket.Accept();
                _acceptor.Accept(clientSocket);
            }
        }

        public override void CloseConnection(AConnection con)
        {
            throw new NotSupportedException("This method should never be called!");
        }

        protected override void Register(AConnection connection)
        {
            throw new NotSupportedException("This method should never be called!");
        }
    }
}
