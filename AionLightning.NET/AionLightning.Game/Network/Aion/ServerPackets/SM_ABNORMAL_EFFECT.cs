using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Sends abnormal effect (buff/debuff) state for a creature. Opcode 0x32.
/// Player entries include effectorId per effect; NPC entries omit it.
/// Wire: D(effectedId) C(effectType) D(0) D(abnormals) D(0) C(0x7F) H(count)
///   then per effect [player: D(effectorId)] H(skillId) C(level) C(targetSlot) D(remainMs)
/// </summary>
public sealed class SM_ABNORMAL_EFFECT : AionServerPacket
{
    private readonly int                 _effectedId;
    private readonly bool                _isPlayer;
    private readonly List<AbnormalState> _effects;

    /// <summary>Sends count=0 to clear all effects — used on zone entry or death.</summary>
    public SM_ABNORMAL_EFFECT(int effectedId, bool isPlayer) : base(0x32)
    {
        _effectedId = effectedId;
        _isPlayer   = isPlayer;
        _effects    = new List<AbnormalState>();
    }

    /// <summary>Sends the given active effects to the client.</summary>
    public SM_ABNORMAL_EFFECT(int effectedId, bool isPlayer, List<AbnormalState> effects) : base(0x32)
    {
        _effectedId = effectedId;
        _isPlayer   = isPlayer;
        _effects    = effects.Where(e => !e.IsExpired).ToList();
    }

    public override void Write(ref PacketWriter w)
    {
        byte effectType = _isPlayer ? (byte)2 : (byte)1;
        int abnormals = _effects.Aggregate(0, (acc, e) => acc | (int)e.CcFlags);
        w.WriteD(_effectedId);
        w.WriteC(effectType);
        w.WriteD(0);      // time
        w.WriteD(abnormals);
        w.WriteD(0);      // unk
        w.WriteC(0x7F);   // slots (127 = max)
        w.WriteH(_effects.Count);
        foreach (var e in _effects)
        {
            if (_isPlayer)
                w.WriteD(e.EffectorId);
            w.WriteH(e.SkillId);
            w.WriteC((byte)e.SkillLevel);
            w.WriteC(0);  // targetSlot — not parsed from XML yet, defaults to 0
            w.WriteD(e.RemainingMs);
        }
    }
}
