using AionLightning.Commons.Network;
using AionLightning.Game.Model.Legion;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends the full legion member list to the client. Opcode 0x9D.</summary>
public sealed class SM_LEGION_MEMBERLIST : AionServerPacket
{
    private readonly IEnumerable<LegionMember> _members;
    private readonly bool _isFirst;

    public SM_LEGION_MEMBERLIST(IEnumerable<LegionMember> members, bool isFirst = true) : base(0x9D)
    {
        _members = members;
        _isFirst  = isFirst;
    }

    public override void Write(ref PacketWriter w)
    {
        var list = _members.ToList();
        w.WriteC(_isFirst ? (byte)1 : (byte)0);
        w.WriteH((short)list.Count);
        foreach (var m in list)
        {
            w.WriteD(m.ObjectId);
            w.WriteS(m.Name);
            w.WriteC((byte)m.ClassId);
            w.WriteD(m.Level);
            w.WriteC((byte)m.Rank);
            w.WriteD(m.WorldId);
            w.WriteC(m.IsOnline ? (byte)1 : (byte)0);
            w.WriteS(m.SelfIntro);
            w.WriteS(m.Nickname);
            w.WriteD(0); // lastOnline
            w.WriteD(0);
            w.WriteD(0);
            w.WriteC(1);
            w.WriteC(0);
            w.WriteC(0);
            w.WriteC(0);
        }
    }
}
