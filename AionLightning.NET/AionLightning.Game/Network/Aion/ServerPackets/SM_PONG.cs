using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_PONG : AionServerPacket
{
    public SM_PONG() : base(0x8E) { }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(0x00);
        w.WriteC(0x00);
    }
}
