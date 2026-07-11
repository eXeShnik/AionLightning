using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// The player's OWN buff/debuff bar with remaining durations (Java SM_ABNORMAL_STATE).
/// Opcode 0x31. The zone-visible sibling is SM_ABNORMAL_EFFECT (0x32).
/// Wire: D(abnormals) D(0) D(0) C(0x7F) H(count)
///   then per effect D(effectorId) H(skillId) C(level) C(targetSlot) D(remainMs)
/// </summary>
public sealed class SM_ABNORMAL_STATE : AionServerPacket
{
    private readonly List<AbnormalState> _effects;

    public SM_ABNORMAL_STATE(List<AbnormalState> effects) : base(0x31)
    {
        _effects = effects.Where(e => !e.IsExpired).ToList();
    }

    public override void Write(ref PacketWriter w)
    {
        int abnormals = _effects.Aggregate(0, (acc, e) => acc | (int)e.CcFlags);
        w.WriteD(abnormals);
        w.WriteD(0);
        w.WriteD(0);      // unk 4.5
        w.WriteC(0x7F);   // slots
        w.WriteH((short)_effects.Count);
        foreach (var e in _effects)
        {
            w.WriteD(e.EffectorId);
            w.WriteH((short)e.SkillId);
            w.WriteC((byte)e.SkillLevel);
            w.WriteC(0);  // targetSlot — matches SM_ABNORMAL_EFFECT default
            w.WriteD(e.RemainingMs);
        }
    }
}
