using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Tells the client it has been removed from a group. Opcode 0xF7.</summary>
public sealed class SM_LEAVE_GROUP_MEMBER : AionServerPacket
{
    public SM_LEAVE_GROUP_MEMBER() : base(0xF7) { }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(0x00);
        w.WriteC(0x00);
        w.WriteD(0x3F);
        w.WriteD(0x00);
        w.WriteH(0x00);
    }
}
