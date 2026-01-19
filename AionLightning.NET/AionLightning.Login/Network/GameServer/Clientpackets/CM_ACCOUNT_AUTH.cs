using AionLightning.Commons.Network;
using AionLightning.LoginServer.Controller;
using AionLightning.LoginServer.Network.Aion;
using Microsoft.Extensions.Logging;

namespace AionLightning.LoginServer.Network.Gameserver.Clientpackets
{
    public class CM_ACCOUNT_AUTH : GsClientPacket
    {
        private SessionKey _sessionKey;

        public CM_ACCOUNT_AUTH(ByteBuffer buffer, GsConnection client, ILogger<CM_ACCOUNT_AUTH> logger) : base(buffer, client, logger)
        {
        }

        protected override void ReadImpl()
        {
            int accountId = ReadD();
            int loginOk = ReadD();
            int playOk1 = ReadD();
            int playOk2 = ReadD();

            _sessionKey = new SessionKey(accountId, loginOk, playOk1, playOk2);
        }

        protected override void RunImpl()
        {
            AccountController.CheckAuth(_sessionKey, GetClient() as GsConnection);
        }
    }
}
