using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_QUIT_RESPONSE : AionServerPacket
{
    public SM_QUIT_RESPONSE() : base(0x62) { }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(1);    // 1=normal logout, 2=plastic surgery/gender switch
        w.WriteC(0x00);
        w.WriteC(0xFF);
        w.WriteC(0xFF);
        w.WriteC(0xFF);
        w.WriteC(0xFF);
    }
}
