using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Reports HP/MP/FP change (damage or healing) on a creature. Opcode 0x05.
/// </summary>
public sealed class SM_ATTACK_STATUS : AionServerPacket
{
    public enum AttackType
    {
        NaturalHp    = 3,
        UsedHp       = 4,
        Regular      = 5,
        AbsorbedHp   = 6,
        Damage       = 7,
        Hp           = 7,
        ProtectDmg   = 8,
        DelayDamage  = 10,
        FallDamage   = 17,
        HealMp       = 19,
        AbsorbedMp   = 20,
        Mp           = 21,
        NaturalMp    = 22,
        FpRings      = 23,
        Fp           = 25,
        NaturalFp    = 26,
    }

    public enum LogId
    {
        SpellAtk               = 1,
        Heal                   = 3,
        MpHeal                 = 4,
        SkillAtkDrainInstant   = 23,
        SpellAtkDrainInstant   = 24,
        Poison                 = 25,
        Bleed                  = 26,
        ProcAtkInstant         = 92,
        DelayedSpellAtk        = 95,
        SpellAtkDrain          = 130,
        FpHeal                 = 133,
        RegularHeal            = 170,
        Regular                = 181,
    }

    private readonly int _objectId;
    private readonly int _value;
    private readonly AttackType _type;
    private readonly int _hpPercent;
    private readonly int _skillId;
    private readonly int _logId;

    public SM_ATTACK_STATUS(Creature creature, AttackType type, int skillId, int value, LogId log = LogId.Regular)
        : base(0x05)
    {
        _objectId  = creature.ObjectId;
        _type      = type;
        _skillId   = skillId;
        _value     = value;
        _logId     = (int)log;
        _hpPercent = creature.MaxHp > 0 ? (creature.CurrentHp * 100) / creature.MaxHp : 0;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_objectId);
        if (_type is AttackType.Damage or AttackType.DelayDamage)
            w.WriteD(-_value);
        else
            w.WriteD(_value);
        w.WriteC((byte)_type);
        w.WriteC((byte)_hpPercent);
        w.WriteH(_skillId);
        w.WriteH(_logId);
    }
}
