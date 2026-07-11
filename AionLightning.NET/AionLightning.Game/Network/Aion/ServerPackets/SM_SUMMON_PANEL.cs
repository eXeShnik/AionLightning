using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Opens the summon panel UI on the master's client for a freshly-created summon. Opcode 0x99.
/// M381 Phase 1: stat fields are interim — sourced from the summon's NpcTemplate rather than a
/// summon_stats template (Phase 3 debt, see migration_plan.md).</summary>
public sealed class SM_SUMMON_PANEL : AionServerPacket
{
    private readonly Summon _summon;

    public SM_SUMMON_PANEL(Summon summon) : base(0x99) => _summon = summon;

    public override void Write(ref PacketWriter w)
    {
        var stats = _summon.Template.Stats;

        w.WriteD(_summon.ObjectId);
        w.WriteH(_summon.Level);
        w.WriteD(0); // unk
        w.WriteD(0); // unk
        w.WriteD(_summon.CurrentHp);
        w.WriteD(_summon.MaxHp);
        w.WriteD(stats?.MainHandAttack ?? 0);
        w.WriteH((short)(stats?.PDef ?? 0));
        w.WriteH(0);
        w.WriteH((short)(stats?.MResist ?? 0));
        w.WriteH(0); // unk
        w.WriteH(0); // unk
        w.WriteD(_summon.LiveTime); // life time
    }
}
