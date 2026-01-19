using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Commons.Network
{
    public abstract class AionConnection
    {
        protected readonly ILogger _log;
        protected readonly Socket _socket;

        protected AionConnection(ILogger log, Socket socket)
        {
            _log = log;
            _socket = socket;
        }

        public abstract bool ProcessPacket(AionPacket packet);
        public abstract bool Close(bool force);
    }
}
