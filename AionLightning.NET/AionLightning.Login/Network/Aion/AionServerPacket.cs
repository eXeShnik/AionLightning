using System.IO;
using AionLightning.Commons.Network.Packet;

namespace AionLightning.LoginServer.Network.Aion
{
    public abstract class AionServerPacket : BaseServerPacket
    {
        protected AionServerPacket(int opcode) : base(opcode)
        {
        }

        public void Write(LoginConnection con)
        {
            WriteH(0);
            WriteC(Opcode);
            WriteImpl(con);
        }

        protected abstract void WriteImpl(LoginConnection con);
    }
}
