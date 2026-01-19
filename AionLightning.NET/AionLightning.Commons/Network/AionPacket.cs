namespace AionLightning.Commons.Network
{
    public abstract class AionPacket
    {
        public int Opcode { get; }

        protected AionPacket(int opcode)
        {
            Opcode = opcode;
        }
    }
}
