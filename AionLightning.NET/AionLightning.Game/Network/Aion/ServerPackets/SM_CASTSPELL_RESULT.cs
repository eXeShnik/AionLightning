using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Skill-cast completion with per-target results — damage numbers, hit status,
/// HP percentages (Java SM_CASTSPELL_RESULT). Opcode 0x2B.
/// Covers the common path: dashStatus=0, spellStatus=0 (no CC displacement),
/// no shield/reflect sub-blocks. Extend when those systems need client feedback.
/// </summary>
public sealed class SM_CASTSPELL_RESULT : AionServerPacket
{
    /// <summary>One per-target result entry. AttackStatusId uses Java AttackStatus ids
    /// (10 = normal hit, 202 = critical — same table as SM_ATTACK.HitResult).</summary>
    public readonly record struct ResultHit(int TargetObjectId, byte TargetHpPercent, int Damage, byte AttackStatusId);

    private readonly int   _effectorObjectId;
    private readonly byte  _effectorHpPercent;
    private readonly int   _targetType;
    private readonly int   _targetObjectId;
    private readonly float _x, _y, _z;
    private readonly int   _spellId;
    private readonly int   _level;
    private readonly int   _cooldown;
    private readonly int   _hitTime;
    private readonly IReadOnlyList<ResultHit> _hits;

    public SM_CASTSPELL_RESULT(int effectorObjectId, byte effectorHpPercent,
        int targetType, int targetObjectId, float x, float y, float z,
        int spellId, int level, int cooldown, int hitTime,
        IReadOnlyList<ResultHit> hits) : base(0x2B)
    {
        _effectorObjectId  = effectorObjectId;
        _effectorHpPercent = effectorHpPercent;
        _targetType        = targetType;
        _targetObjectId    = targetObjectId;
        _x = x; _y = y; _z = z;
        _spellId           = spellId;
        _level             = level;
        _cooldown          = cooldown;
        _hitTime           = hitTime;
        _hits              = hits;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_effectorObjectId);
        w.WriteC((byte)_targetType);
        switch (_targetType)
        {
            case 0 or 3 or 4:
                w.WriteD(_targetObjectId);
                break;
            case 1:
                w.WriteF(_x); w.WriteF(_y); w.WriteF(_z);
                break;
            case 2:
                w.WriteF(_x); w.WriteF(_y); w.WriteF(_z);
                for (int i = 0; i < 8; i++) w.WriteF(0);
                break;
        }
        w.WriteH((short)_spellId);
        w.WriteC((byte)_level);
        w.WriteD(_cooldown);
        w.WriteH((short)_hitTime);
        w.WriteC(0);

        // 16 = no-damage (all dodged/resisted/empty), 32 = regular, 0 = chain/counter
        w.WriteH((short)(_hits.Count == 0 ? 16 : 32));

        w.WriteC(0); // dashStatus — none

        w.WriteH((short)_hits.Count);
        foreach (var hit in _hits)
        {
            w.WriteD(hit.TargetObjectId);
            w.WriteC(0); // effectResult: NORMAL
            w.WriteC(hit.TargetHpPercent);
            w.WriteC(_effectorHpPercent);
            w.WriteC(0); // spellStatus: none
            w.WriteC(0); // skillMoveType: default
            w.WriteH(0);
            w.WriteC(0); // carved signet count

            w.WriteC(1); // loop size
            w.WriteC(0); // mpheal-instant flag
            w.WriteD(hit.Damage);
            w.WriteC(hit.AttackStatusId);
            w.WriteC(0); // shieldDefense: none
        }
    }
}
