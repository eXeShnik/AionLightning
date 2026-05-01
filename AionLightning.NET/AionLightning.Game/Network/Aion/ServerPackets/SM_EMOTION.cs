using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Broadcasts a creature emotion/state change. Opcode 0x25.
/// </summary>
public sealed class SM_EMOTION : AionServerPacket
{
    private readonly int _senderObjectId;
    private readonly EmotionType _emotionType;
    private readonly int _state;
    private readonly float _speed;
    private readonly int _emotion;
    private readonly int _targetObjectId;
    private readonly float _x, _y, _z;
    private readonly byte _heading;
    private readonly int _baseAttackSpeed;
    private readonly int _currentAttackSpeed;

    /// <summary>For static objects (doors, etc.) that are not Creatures.</summary>
    public SM_EMOTION(int objectId, EmotionType emotionType, int state)
        : base(0x25)
    {
        _senderObjectId = objectId;
        _emotionType    = emotionType;
        _state          = state;
    }

    public SM_EMOTION(Creature creature, EmotionType emotionType,
        int emotion = 0, int targetObjectId = 0,
        float x = 0, float y = 0, float z = 0, byte heading = 0)
        : base(0x25)
    {
        _senderObjectId    = creature.ObjectId;
        _emotionType       = emotionType;
        _state             = creature.StateValue;
        _speed             = creature.MovementSpeed;
        _emotion           = emotion;
        _targetObjectId    = targetObjectId;
        _x                 = x;
        _y                 = y;
        _z                 = z;
        _heading           = heading;
        _baseAttackSpeed   = creature.CurrentAttackSpeed;
        _currentAttackSpeed = creature.CurrentAttackSpeed;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_senderObjectId);
        w.WriteC((byte)_emotionType);
        w.WriteH(_state);
        w.WriteF(_speed);

        switch (_emotionType)
        {
            case EmotionType.LAND_FLYTELEPORT:
            case EmotionType.FLY:
            case EmotionType.LAND:
            case EmotionType.SELECT_TARGET:
            case EmotionType.JUMP:
            case EmotionType.SIT:
            case EmotionType.STAND:
            case EmotionType.ATTACKMODE:
            case EmotionType.NEUTRALMODE:
            case EmotionType.WALK:
            case EmotionType.RUN:
            case EmotionType.OPEN_PRIVATESHOP:
            case EmotionType.CLOSE_PRIVATESHOP:
            case EmotionType.POWERSHARD_ON:
            case EmotionType.POWERSHARD_OFF:
            case EmotionType.ATTACKMODE2:
            case EmotionType.NEUTRALMODE2:
            case EmotionType.START_FEEDING:
            case EmotionType.END_FEEDING:
            case EmotionType.WINDSTREAM_START_BOOST:
            case EmotionType.WINDSTREAM_END_BOOST:
            case EmotionType.WINDSTREAM_END:
            case EmotionType.WINDSTREAM_EXIT:
            case EmotionType.OPEN_DOOR:
            case EmotionType.CLOSE_DOOR:
            case EmotionType.WINDSTREAM_STRAFE:
                break;

            case EmotionType.DIE:
            case EmotionType.START_LOOT:
            case EmotionType.END_LOOT:
            case EmotionType.START_QUESTLOOT:
            case EmotionType.END_QUESTLOOT:
                w.WriteD(_targetObjectId);
                break;

            case EmotionType.CHAIR_SIT:
            case EmotionType.CHAIR_UP:
                w.WriteF(_x);
                w.WriteF(_y);
                w.WriteF(_z);
                w.WriteC(_heading);
                break;

            case EmotionType.START_FLYTELEPORT:
                w.WriteD(_emotion);
                break;

            case EmotionType.WINDSTREAM:
                w.WriteD(_emotion);
                w.WriteD(_targetObjectId);
                break;

            case EmotionType.RIDE:
            case EmotionType.RIDE_END:
                if (_targetObjectId != 0)
                    w.WriteD(_targetObjectId);
                w.WriteH(0);
                w.WriteC(0);
                w.WriteD(0x3F);
                w.WriteD(0x3F);
                w.WriteC(0x40);
                break;

            case EmotionType.RESURRECT:
                w.WriteD(0);
                break;

            case EmotionType.EMOTE:
                w.WriteD(_targetObjectId);
                w.WriteH(_emotion);
                w.WriteC(1);
                break;

            case EmotionType.START_EMOTE2:
                w.WriteH(_baseAttackSpeed);
                w.WriteH(_currentAttackSpeed);
                w.WriteC(0);
                break;

            case EmotionType.START_SPRINT:
                w.WriteD(0);
                break;

            default:
                if (_targetObjectId != 0)
                    w.WriteD(_targetObjectId);
                break;
        }
    }
}
