using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Refreshes the summon panel's stat display and mode indicator. Opcode 0x9B.
/// M381 Phase 1: current/base stat pairs are identical (no summon_stats template, no gear/buffs on
/// summons yet) — several fields (mDef, mBoost, mAccuracy, mCritical, parry) have no NpcTemplate
/// analog and are reported as 0 until Phase 3 ports summon_stats.</summary>
public sealed class SM_SUMMON_UPDATE : AionServerPacket
{
    private readonly Summon _summon;

    public SM_SUMMON_UPDATE(Summon summon) : base(0x9B) => _summon = summon;

    public override void Write(ref PacketWriter w)
    {
        var stats = _summon.Template.Stats;

        w.WriteC(_summon.Level);
        w.WriteH((short)_summon.Mode);
        w.WriteD(0); // unk
        w.WriteD(0); // unk
        w.WriteD(_summon.CurrentHp);

        int maxHp = _summon.MaxHp;
        w.WriteD(maxHp);

        int mainHandPAttack = stats?.MainHandAttack ?? 0;
        w.WriteD(mainHandPAttack);

        int pDef = stats?.PDef ?? 0;
        w.WriteD(pDef);

        int mResist = stats?.MResist ?? 0;
        w.WriteH((short)mResist);

        int mDef = 0;
        w.WriteD(mDef);

        int accuracy = stats?.MainHandAccuracy ?? 0;
        w.WriteH((short)accuracy);

        // No dedicated crit stat on NpcTemplate — reuse "power" the same way NpcAiService treats it
        // as the NPC's crit-rating proxy elsewhere.
        int mainHandPCritical = stats?.Power ?? 0;
        w.WriteH((short)mainHandPCritical);

        int mBoost = 0;
        w.WriteH((short)mBoost);

        int suppression = stats?.MBResist ?? 0;
        w.WriteH((short)suppression);

        int mAccuracy = stats?.Accuracy ?? 0;
        w.WriteH((short)mAccuracy);

        int mCritical = 0;
        w.WriteH((short)mCritical);

        int parry = 0;
        w.WriteH((short)parry);

        int evasion = stats?.Evasion ?? 0;
        w.WriteH((short)evasion);

        // Base pairs — identical to current (interim: no equipment/buffs modify summon stats yet)
        w.WriteD(maxHp);
        w.WriteD(mainHandPAttack);
        w.WriteD(pDef);
        w.WriteH((short)mResist);
        w.WriteD(mDef);
        w.WriteH((short)accuracy);
        w.WriteH((short)mainHandPCritical);
        w.WriteH((short)mBoost);
        w.WriteH((short)suppression);
        w.WriteH((short)mAccuracy);
        w.WriteH((short)mCritical);
        w.WriteH((short)parry);
        w.WriteH((short)evasion);
    }
}
