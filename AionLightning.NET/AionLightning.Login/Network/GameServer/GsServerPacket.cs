using System.IO;
using AionLightning.Commons.Network.Packet;
using AionLightning.LoginServer.Network.Gameserver;

namespace AionLightning.LoginServer.Network.Gameserver
{
    public abstract class GsServerPacket : BaseServerPacket
    {
        protected GsServerPacket() : base(0)
        {
        }

        public void Write(GsConnection con, MemoryStream stream)
        {
            SetStream(stream);
            WriteH(0);
            WriteImpl(con);
        }

        protected abstract void WriteImpl(GsConnection con);
    }
}
