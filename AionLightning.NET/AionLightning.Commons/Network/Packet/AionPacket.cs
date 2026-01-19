using AionLightning.Commons.Network.Packet;

namespace AionLightning.Commons.Network.Packet
{
    public abstract class AionPacket
    {
        private readonly PacketType _type;
        private readonly int _opcode;

        protected AionPacket(PacketType type, int opcode)
        {
            _type = type;
            _opcode = opcode;
        }

        public PacketType GetType()
        {
            return _type;
        }

        public int GetOpcode()
        {
            return _opcode;
        }
    }
}
