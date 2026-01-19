using AionLightning.LoginServer.Network.Gameserver;

namespace AionLightning.LoginServer.Network.Gameserver.Serverpackets
{
    public class SM_PING : GsServerPacket
    {
        protected override void WriteImpl(GsConnection con)
        {
            WriteC(11);
        }
    }
}
