using System.Collections.Generic;
using AionLightning.Commons.Network;
using AionLightning.LoginServer.Network.Gameserver.Serverpackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.LoginServer.Network.Gameserver.Clientpackets
{
    public class CM_GS_AUTH : GsClientPacket
    {
        private readonly ILogger<CM_GS_AUTH> _logger;
        private string _password;
        private byte _gameServerId;
        private int _maxPlayers;
        private int _port;
        private List<IPRange> _ipRanges;
        private byte[] _defaultAddress;

        public CM_GS_AUTH(ByteBuffer buffer, GsConnection client, ILogger<CM_GS_AUTH> logger) : base(buffer, client, logger)
        {
            _logger = logger;
        }

        protected override void ReadImpl()
        {
            _gameServerId = (byte)ReadC();
            _password = ReadS();
            _maxPlayers = ReadD();
            _port = ReadH();

            int size = ReadD();
            _ipRanges = new List<IPRange>(size);
            for (int i = 0; i < size; i++)
            {
                _ipRanges.Add(new IPRange(ReadS(), ReadS()));
            }

            size = ReadD();
            _defaultAddress = ReadB(size);
        }

        protected override void RunImpl()
        {
            var response = GameServerTable.RegisterGameServer(GetClient() as GsConnection, _gameServerId, _defaultAddress, _ipRanges, _port, _maxPlayers, _password);

            (GetClient() as GsConnection).SendPacket(new SM_GS_AUTH_RESPONSE(response));

            if (response == GsAuthResponse.AUTHED)
            {
                (GetClient() as GsConnection).ConnectionState = GsConnection.State.AUTHED;
                _logger.LogInformation($"Gameserver {(_gameServerId)} is authed");
                (GetClient() as GsConnection).SendPacket(new SM_MACBAN_LIST());
            }
        }
    }
}
