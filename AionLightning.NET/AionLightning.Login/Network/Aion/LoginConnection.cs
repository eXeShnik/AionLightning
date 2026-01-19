using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using AionLightning.Commons.Network;
using AionLightning.Commons.Network.Packet;
using AionLightning.LoginServer.Model;
using AionLightning.LoginServer.Network.Aion.Serverpackets;
using AionLightning.LoginServer.Network.Factories;
using AionLightning.LoginServer.Network.Ncrypt;
using Microsoft.Extensions.Logging;

namespace AionLightning.LoginServer.Network.Aion
{
    public class LoginConnection : AConnection
    {
        private static readonly ILogger<LoginConnection> _log = new LoggerFactory().CreateLogger<LoginConnection>();
        private static readonly PacketProcessor<LoginConnection> _processor = new PacketProcessor<LoginConnection>(1, 8, 50, 3);
        private readonly Queue<AionServerPacket> _sendMsgQueue = new Queue<AionServerPacket>();
        public int SessionId { get; }
        public Account Account { get; set; }
        private CryptEngine _cryptEngine;
        private bool _joinedGs;
        private EncryptedRSAKeyPair _encryptedRSAKeyPair;
        public SessionKey SessionKey { get; set; }
        public State ConnectionState { get; set; }
        public GameServerInfo GameServerInfo { get; set; }

        public enum State
        {
            CONNECTED,
            AUTHED_GG,
            AUTHED_LOGIN,
            AUTHED_GS
        }

        public LoginConnection(Socket socket, IDispatcher dispatcher) : base(socket)
        {
            SessionId = GetHashCode();
            ConnectionState = State.CONNECTED;
            _cryptEngine = new CryptEngine();
            _encryptedRSAKeyPair = KeyGen.GetEncryptedRSAKeyPair();
            SendPacket(new SM_INIT(this));
        }

        public override bool Process(AionLightning.Commons.Network.ByteBuffer data)
        {
            if (!Decrypt(data))
                return false;

            var pck = AionPacketHandlerFactory.Handle(data, this);

            if (pck != null && pck.Read())
            {
                _processor.ExecutePacket(pck);
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
                packet.Write(this);
                var buffer = new AionLightning.Commons.Network.ByteBuffer(stream.ToArray());
                Encrypt(buffer);
                return buffer;
            }
        }

        public void SendPacket(AionServerPacket packet)
        {
            lock (_guard)
            {
                _sendMsgQueue.Enqueue(packet);
            }
            EnableWriteInterest();
        }

        public override void OnDisconnect()
        {
            _log.LogInformation($"{this} disconnected");
            if (Account != null)
            {
                Account.SessionKey = null;
                Account.GameServerInfo = null;
            }
        }

        public override string ToString()
        {
            return $"LoginConnection [IP: {IP}, State: {ConnectionState}]";
        }

        private bool Decrypt(AionLightning.Commons.Network.ByteBuffer buf)
        {
            return _cryptEngine.Decrypt(buf.GetBuffer(), 0, buf.Limit);
        }

        private void Encrypt(AionLightning.Commons.Network.ByteBuffer buf)
        {
            _cryptEngine.Encrypt(buf.GetBuffer(), 0, buf.Limit);
        }

        public void SetCrypt(CryptEngine crypt)
        {
            _cryptEngine = crypt;
        }

        public EncryptedRSAKeyPair GetEncryptedRSAKeyPair()
        {
            return _encryptedRSAKeyPair;
        }
    }
}
