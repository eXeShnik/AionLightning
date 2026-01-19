using AionLightning.LoginServer.Network.Aion;

namespace AionLightning.LoginServer.Network.Aion.Serverpackets
{
    public class SM_LOGIN_FAIL : AionServerPacket
    {
        private readonly AionAuthResponse _response;

        public SM_LOGIN_FAIL(AionAuthResponse response) : base(0x01)
        {
            _response = response;
        }

        protected override void WriteImpl(LoginConnection con)
        {
            WriteD(_response.GetMessageId());
        }
    }
}
