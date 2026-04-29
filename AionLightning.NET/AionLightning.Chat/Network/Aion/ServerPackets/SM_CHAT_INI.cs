using AionLightning.Commons.Network;

namespace AionLightning.Chat.Network.Aion.ServerPackets;

public sealed class SM_CHAT_INI : AionServerPacket
{
    public SM_CHAT_INI() : base(0x31) { }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(0x40);
        w.WriteD(0x02);
        w.WriteH(0x00);
    }
}
