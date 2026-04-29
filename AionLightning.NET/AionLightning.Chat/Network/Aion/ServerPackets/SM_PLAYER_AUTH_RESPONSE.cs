using AionLightning.Commons.Network;

namespace AionLightning.Chat.Network.Aion.ServerPackets;

public sealed class SM_PLAYER_AUTH_RESPONSE : AionServerPacket
{
    public SM_PLAYER_AUTH_RESPONSE() : base(0x02) { }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(0x40);
        w.WriteH(0x01);
        w.WriteD(0x00);
        w.WriteH(0x0822);
    }
}
