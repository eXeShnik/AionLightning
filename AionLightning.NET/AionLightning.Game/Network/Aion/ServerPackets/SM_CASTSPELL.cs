using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Plays the skill-casting animation on a creature. Opcode 0x21.
/// targetType: 0/3/4 = object; 1 = point; 2 = point + 8 extra floats.
/// </summary>
public sealed class SM_CASTSPELL : AionServerPacket
{
    private readonly int _attackerObjectId;
    private readonly int _spellId;
    private readonly int _level;
    private readonly int _targetType;
    private readonly int _targetObjectId;
    private readonly float _x, _y, _z;
    private readonly int _duration;
    private readonly bool _isCharge;

    public SM_CASTSPELL(int attackerObjectId, int spellId, int level,
        int targetType, int targetObjectId, int duration, bool isCharge = false)
        : base(0x21)
    {
        _attackerObjectId = attackerObjectId;
        _spellId          = spellId;
        _level            = level;
        _targetType       = targetType;
        _targetObjectId   = targetObjectId;
        _duration         = duration;
        _isCharge         = isCharge;
    }

    public SM_CASTSPELL(int attackerObjectId, int spellId, int level,
        int targetType, float x, float y, float z, int duration)
        : this(attackerObjectId, spellId, level, targetType, 0, duration)
    {
        _x = x; _y = y; _z = z;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_attackerObjectId);
        w.WriteH(_spellId);
        w.WriteC((byte)_level);
        w.WriteC((byte)_targetType);

        switch (_targetType)
        {
            case 0:
            case 3:
            case 4:
                w.WriteD(_targetObjectId);
                break;
            case 1:
                w.WriteF(_x); w.WriteF(_y); w.WriteF(_z);
                break;
            case 2:
                w.WriteF(_x); w.WriteF(_y); w.WriteF(_z);
                w.WriteZero(32); // 8 × D unk
                break;
        }

        w.WriteH(_duration);
        w.WriteC(0x00);
        w.WriteF(1.0f);
        w.WriteC(_isCharge ? (byte)0x01 : (byte)0x00);
    }
}
