namespace AionLightning.Commons.Network.Packet
{
    public abstract class BasePacket
    {
        public const string TYPE_PATTERN = "[{0}] 0x{1:X2} {2}";

        private readonly PacketType _packetType;
        private int _opcode;

        protected BasePacket(PacketType packetType, int opcode)
        {
            _packetType = packetType;
            _opcode = opcode;
        }

        protected BasePacket(PacketType packetType)
        {
            _packetType = packetType;
        }

        protected void SetOpcode(int opcode)
        {
            _opcode = opcode;
        }

        public int GetOpcode()
        {
            return _opcode;
        }

        public PacketType GetPacketType()
        {
            return _packetType;
        }

        public string GetPacketName()
        {
            return GetType().Name;
        }

        public override string ToString()
        {
            return string.Format(TYPE_PATTERN, _packetType.GetName(), GetOpcode(), GetPacketName());
        }
    }

    public enum PacketType
    {
        SERVER,
        CLIENT
    }

    public static class PacketTypeExtensions
    {
        public static string GetName(this PacketType packetType)
        {
            return packetType == PacketType.SERVER ? "S" : "C";
        }
    }
}
