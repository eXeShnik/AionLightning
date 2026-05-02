using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Broadcasts a physical auto-attack result to all players in range. Opcode 0x36.</summary>
public sealed class SM_ATTACK : AionServerPacket
{
    // Java AttackStatus enum IDs (AttackStatus.java)
    public enum HitResult { Normal = 10, Critical = 202, Dodge = 0, Parry = 2, CritParry = 194, Block = 4, CritBlock = 196 }

    public readonly record struct HitEntry(int Damage, HitResult Result);

    // Counter-skill flags written to the packet header (Java SM_ATTACK.writeImpl switch — uses first entry)
    private static short CounterFlag(HitResult r) => r switch
    {
        HitResult.Block    or HitResult.CritBlock => 32,
        HitResult.Parry    or HitResult.CritParry => 64,
        HitResult.Dodge                           => 128,
        _                                         => 0,
    };

    private readonly int _attackerObjectId;
    private readonly byte _attackno;
    private readonly short _time;
    private readonly byte _type;
    private readonly int _targetObjectId;
    private readonly byte _targetHpPercent;
    private readonly byte _attackerHpPercent;
    private readonly HitEntry[] _hits;

    public SM_ATTACK(Creature attacker, Creature target, byte attackno, short time, byte type,
        HitEntry[] hits) : base(0x36)
    {
        _attackerObjectId  = attacker.ObjectId;
        _attackno          = attackno;
        _time              = time;
        _type              = type;
        _targetObjectId    = target.ObjectId;
        _targetHpPercent   = target.MaxHp > 0 ? (byte)(100 * target.CurrentHp / target.MaxHp) : (byte)0;
        _attackerHpPercent = attacker.MaxHp > 0 ? (byte)(100 * attacker.CurrentHp / attacker.MaxHp) : (byte)0;
        _hits              = hits;
    }

    public SM_ATTACK(Creature attacker, Creature target, byte attackno, short time, byte type, int damage,
        HitResult result = HitResult.Normal)
        : this(attacker, target, attackno, time, type, [new HitEntry(damage, result)]) { }

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
        w.WriteH(CounterFlag(_hits[0].Result)); // counter-skill flag based on first entry
        w.WriteH(0); // unk
        w.WriteC((byte)_hits.Length);
        foreach (var h in _hits)
        {
            w.WriteD(h.Damage);
            w.WriteC((byte)h.Result); // Java AttackStatus getId()
            w.WriteC(0);              // shield type 0 = no shield (no extra fields)
        }
        w.WriteC(0); // unk tail (outside loop, Java SM_ATTACK line 145)
    }
}
