using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using AionLightning.Commons.Network;
using AionLightning.Commons.Services;
using AionLightning.LoginServer.Configs;
using AionLightning.LoginServer.Network.Factories;
using Microsoft.Extensions.Logging;

namespace AionLightning.LoginServer.Network.Gameserver
{
    public class GsConnection : AConnection
    {
        private static readonly ILogger<GsConnection> _log = new LoggerFactory().CreateLogger<GsConnection>();

        public enum State
        {
            CONNECTED,
            AUTHED
        }

        private readonly Queue<GsServerPacket> _sendMsgQueue = new Queue<GsServerPacket>();
        public State ConnectionState { get; set; }
        public GameServerInfo GameServerInfo { get; set; }
        private PingPongThread _pingThread;

        public GsConnection(Socket socket, IDispatcher dispatcher) : base(socket)
        {
            ConnectionState = State.CONNECTED;
            if (Config.ENABLE_PINGPONG)
            {
                _pingThread = new PingPongThread(this);
                ThreadPoolManager.GetInstance().ScheduleAtFixedRate(_pingThread, 5000, 5000);
            }
        }

        public override bool Process(AionLightning.Commons.Network.ByteBuffer data)
        {
            var pck = GsPacketHandlerFactory.Handle(data, this);

            if (pck != null && pck.Read())
            {
                ThreadPoolManager.GetInstance().ExecuteLsPacket(pck);
            }

            return true;
        }

        public override AionLightning.Commons.Network.ByteBuffer Read()
        {
            throw new NotImplementedException();
        }

        public override AionLightning.Commons.Network.ByteBuffer Write()
        {
            lock (_guard)
            {
                if (_sendMsgQueue.Count == 0)
                    return null;

                var packet = _sendMsgQueue.Dequeue();
                var stream = new MemoryStream();
                packet.Write(this, stream);
                var buffer = new AionLightning.Commons.Network.ByteBuffer(stream.ToArray());
                return buffer;
            }
        }

        public void SendPacket(GsServerPacket packet)
        {
            lock (_guard)
            {
                _sendMsgQueue.Enqueue(packet);
            }
            EnableWriteInterest();
        }

        public override void OnDisconnect()
        {
            if (Config.ENABLE_PINGPONG)
            {
                _pingThread.CloseMe();
            }
            _log.LogInformation($"{this} disconnected");

            if (GameServerInfo != null)
            {
                GameServerInfo.GscHandler = null;
            }
        }

        public override string ToString()
        {
            return $"GsConnection [IP: {IP}, State: {ConnectionState}]";
        }
    }
}
