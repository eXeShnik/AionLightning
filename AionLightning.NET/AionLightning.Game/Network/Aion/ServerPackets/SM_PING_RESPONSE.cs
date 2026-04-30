using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_PING_RESPONSE : AionServerPacket
{
    public SM_PING_RESPONSE() : base(0x80) { }

    public override void Write(ref PacketWriter w) => w.WriteC(0x04);
}
