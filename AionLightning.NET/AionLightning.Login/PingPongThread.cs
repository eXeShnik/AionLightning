using System;
using System.Threading;
using AionLightning.Commons.Services;
using AionLightning.LoginServer.Network.Gameserver;
using AionLightning.LoginServer.Network.Gameserver.Serverpackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.LoginServer
{
    public class PingPongThread : IRunnable
    {
        private readonly ILogger<PingPongThread> _log = new LoggerFactory().CreateLogger<PingPongThread>();
        private readonly GsConnection _connection;
        public volatile bool Uptime = true;
        private readonly SM_PING _ping;
        private byte _requests;
        private int _serverPid = -1;
        private bool _killProcess;

        public PingPongThread(GsConnection connection)
        {
            _connection = connection;
            _ping = new SM_PING();
        }

        public void Run()
        {
            if (!Uptime)
            {
                if (_requests >= 2)
                {
                    _log.LogWarning($"Connection with GS[{_connection.GameServerInfo.Id}] lost.");
                    _connection.Close(true);
                }
                else
                {
                    _requests++;
                }
            }

            _connection.SendPacket(_ping);
            Uptime = false;
        }

        public void CloseMe()
        {
            Uptime = false;
            _requests = 3;
        }
    }
}
