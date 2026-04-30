using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Broadcasts a physical auto-attack result to all players in range. Opcode 0x36.</summary>
public sealed class SM_ATTACK : AionServerPacket
{
    // AttackStatus IDs per Java AttackStatus enum
    private const byte AttackStatusNormalHit = 10;

    private readonly int _attackerObjectId;
    private readonly byte _attackno;
    private readonly short _time;
    private readonly byte _type;  // 0, 1, 2
    private readonly int _targetObjectId;
    private readonly byte _targetHpPercent;
    private readonly byte _attackerHpPercent;
    private readonly int _damage;

    public SM_ATTACK(Creature attacker, Creature target, byte attackno, short time, byte type, int damage)
        : base(0x36)
    {
        _attackerObjectId  = attacker.ObjectId;
        _attackno          = attackno;
        _time              = time;
        _type              = type;
        _targetObjectId    = target.ObjectId;
        _targetHpPercent   = target.MaxHp > 0 ? (byte)(100 * target.CurrentHp / target.MaxHp) : (byte)0;
        _attackerHpPercent = attacker.MaxHp > 0 ? (byte)(100 * attacker.CurrentHp / attacker.MaxHp) : (byte)0;
        _damage            = damage;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_attackerObjectId);
        w.WriteC(_attackno);
        w.WriteH(_time);
        w.WriteC(0);
        w.WriteC(_type);
        w.WriteD(_targetObjectId);
        w.WriteC(_targetHpPercent);
        w.WriteC(_attackerHpPercent);
        w.WriteH(0); // counter-skill flag (0 = normal hit)
        w.WriteH(0); // unk
        w.WriteC(1); // attack list size
        // Single attack entry
        w.WriteD(_damage);
        w.WriteC(AttackStatusNormalHit);
        w.WriteC(0); // shield type 0 = no shield (no extra fields)
        w.WriteC(0); // unk tail
    }
}
