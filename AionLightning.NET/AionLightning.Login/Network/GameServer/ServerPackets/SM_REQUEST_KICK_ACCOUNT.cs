using AionLightning.LoginServer.Network.Gameserver;

namespace AionLightning.LoginServer.Network.Gameserver.Serverpackets
{
    public class SM_REQUEST_KICK_ACCOUNT : GsServerPacket
    {
        private readonly int _accountId;

        public SM_REQUEST_KICK_ACCOUNT(int accountId)
        {
            _accountId = accountId;
        }

        protected override void WriteImpl(GsConnection con)
        {
            WriteC(2);
            WriteD(_accountId);
        }
    }
}
