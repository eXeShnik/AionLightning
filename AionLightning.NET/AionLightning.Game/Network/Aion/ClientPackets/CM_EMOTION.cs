using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_EMOTION : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly PlayerConnectionRegistry _connRegistry;

    private EmotionType _emotionType;
    private int _emotion;
    private int _targetObjectId;
    private float _x, _y, _z;
    private byte _heading;

    public CM_EMOTION(GsClientConnection conn, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        byte et = (byte)r.ReadC();
        _emotionType = Enum.IsDefined(typeof(EmotionType), et)
            ? (EmotionType)et
            : EmotionType.UNKNOWN;

        switch (_emotionType)
        {
            case EmotionType.EMOTE:
                _emotion        = r.ReadH();
                _targetObjectId = r.ReadD();
                break;
            case EmotionType.CHAIR_SIT:
            case EmotionType.CHAIR_UP:
                _x       = r.ReadF();
                _y       = r.ReadF();
                _z       = r.ReadF();
                _heading = (byte)r.ReadC();
                break;
            default:
                break;
        }
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;
        if (_emotionType == EmotionType.UNKNOWN) return;

        ApplyStateChange(player);

        int tgtId = _targetObjectId != 0
            ? _targetObjectId
            : player.Target?.ObjectId ?? 0;

        var packet = new SM_EMOTION(player, _emotionType, _emotion, tgtId, _x, _y, _z, _heading);

        // Send to self and broadcast to others in the same zone
        await _conn.SendAsync(packet, ct);
        int worldId = player.Position.WorldId;
        foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
            if (other.ActivePlayer?.Position.WorldId == worldId)
                await other.SendAsync(packet, ct);
    }

    private void ApplyStateChange(Player player)
    {
        switch (_emotionType)
        {
            case EmotionType.SIT:
                player.State |= CreatureState.Resting;
                break;
            case EmotionType.STAND:
                player.State &= ~CreatureState.Resting;
                break;
            case EmotionType.CHAIR_SIT:
                if (!player.State.HasFlag(CreatureState.WeaponEquipped))
                    player.State |= CreatureState.Chair;
                break;
            case EmotionType.CHAIR_UP:
                player.State &= ~CreatureState.Chair;
                break;
            case EmotionType.WALK:
                player.State |= CreatureState.Walking;
                break;
            case EmotionType.RUN:
                player.State &= ~CreatureState.Walking;
                break;
            case EmotionType.ATTACKMODE:
            case EmotionType.ATTACKMODE2:
                player.State |= CreatureState.WeaponEquipped;
                break;
            case EmotionType.NEUTRALMODE:
            case EmotionType.NEUTRALMODE2:
                player.State &= ~CreatureState.WeaponEquipped;
                break;
            case EmotionType.FLY:
                player.State |= CreatureState.Flying;
                break;
            case EmotionType.LAND:
            case EmotionType.LAND_FLYTELEPORT:
                player.State &= ~CreatureState.Flying;
                player.State &= ~CreatureState.Gliding;
                break;
            case EmotionType.POWERSHARD_ON:
                player.State |= CreatureState.Powershard;
                break;
            case EmotionType.POWERSHARD_OFF:
                player.State &= ~CreatureState.Powershard;
                break;
        }
    }
}
