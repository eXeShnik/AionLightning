using AionLightning.Commons.Network;
using AionLightning.Game.Model.Legion;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Notifies legion members of an online-status or rank change. Opcode 0x71.</summary>
public sealed class SM_LEGION_UPDATE_MEMBER : AionServerPacket
{
    private readonly LegionMember _member;
    private readonly bool _isOnline;

    public SM_LEGION_UPDATE_MEMBER(LegionMember member, bool isOnline) : base(0x71)
    {
        _member   = member;
        _isOnline = isOnline;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_member.ObjectId);
        w.WriteC((byte)_member.Rank);
        w.WriteC((byte)_member.ClassId);
        w.WriteC(_member.Level);
        w.WriteD(_member.WorldId);
        w.WriteC(_isOnline ? (byte)1 : (byte)0);
        w.WriteD(0); // lastOnline (0 when online)
        w.WriteD(1); // unk 3.0
        w.WriteD(0); // msgId
        w.WriteS(""); // text
    }
}
