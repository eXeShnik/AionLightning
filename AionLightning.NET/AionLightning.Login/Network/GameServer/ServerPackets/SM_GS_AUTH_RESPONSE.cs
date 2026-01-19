using AionLightning.LoginServer.Network.Gameserver;

namespace AionLightning.LoginServer.Network.Gameserver.Serverpackets
{
    public class SM_GS_AUTH_RESPONSE : GsServerPacket
    {
        private readonly GsAuthResponse _response;

        public SM_GS_AUTH_RESPONSE(GsAuthResponse response)
        {
            _response = response;
        }

        protected override void WriteImpl(GsConnection con)
        {
            WriteC(0);
            WriteC((int)_response);
            if (_response == GsAuthResponse.AUTHED)
            {
                WriteC(con.GameServerInfo.Id);
            }
        }
    }
}
