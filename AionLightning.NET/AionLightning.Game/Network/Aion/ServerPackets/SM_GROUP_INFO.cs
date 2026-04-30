using AionLightning.Commons.Network;
using AionLightning.Game.Model.Group;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Group metadata packet. Opcode 0x43.
/// Sent to each member when the group is created, a player joins, or loot rules change.
/// Contains only group-level data — per-member data is sent via SM_GROUP_MEMBER_INFO.
/// Wire format matches Java SM_GROUP_INFO.writeImpl().
/// </summary>
public sealed class SM_GROUP_INFO : AionServerPacket
{
    private readonly PlayerGroup? _group;

    public SM_GROUP_INFO(PlayerGroup group) : base(0x43) => _group = group;

    /// <summary>Dissolution signal — no active group.</summary>
    public SM_GROUP_INFO() : base(0x43) => _group = null;

    public override void Write(ref PacketWriter w)
    {
        if (_group is null)
        {
            // Minimal dissolution packet — groupId=0 signals the client that the group no longer exists.
            w.WriteD(0); // groupId
            w.WriteD(0); // leaderId
            w.WriteD(0); // leaderWorldId
            for (int i = 0; i < 9; i++) w.WriteD(0); // loot rule fields
            w.WriteD(2); // constant
            w.WriteC(0);
            w.WriteD(1); // TeamType.REGULAR
            w.WriteD(0); // TeamType.subType
            w.WriteH(0);
            w.WriteH(0);
            w.WriteS(string.Empty);
            return;
        }

        var leader = _group.Members.FirstOrDefault(m => m.ObjectId == _group.LeaderObjectId);

        w.WriteD(_group.GroupId);
        w.WriteD(_group.LeaderObjectId);
        w.WriteD(leader?.Position.WorldId ?? 0);  // leader world id
        w.WriteD(_group.LootDistribution);         // loot rule id (0=free-for-all)
        w.WriteD(0);                               // lootMisc
        w.WriteD(0);                               // commonItemAbove
        w.WriteD(0);                               // superiorItemAbove
        w.WriteD(0);                               // heroicItemAbove
        w.WriteD(0);                               // fabledItemAbove
        w.WriteD(0);                               // ethernalItemAbove
        w.WriteD(0);                               // autoDistribution id
        w.WriteD(2);                               // constant in Java
        w.WriteC(0);
        w.WriteD(1);                               // TeamType.REGULAR
        w.WriteD(0);                               // TeamType subType
        w.WriteH(0);
        w.WriteH(0);
        w.WriteS(string.Empty);
    }
}
