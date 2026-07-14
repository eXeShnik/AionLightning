using AionLightning.Commons.Network;
using AionLightning.Game.Controllers;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Zone;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_EMOTION : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly FlyController _flyController;
    private readonly ZoneService _zoneService;

    private EmotionType _emotionType;
    private int _emotion;
    private int _targetObjectId;
    private float _x, _y, _z;
    private byte _heading;

    public CM_EMOTION(GsClientConnection conn, PlayerConnectionRegistry connRegistry,
        FlyController flyController, ZoneService zoneService)
    {
        _conn           = conn;
        _connRegistry   = connRegistry;
        _flyController  = flyController;
        _zoneService    = zoneService;
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

        switch (_emotionType)
        {
            case EmotionType.FLY:
                // note: Java gates FLY behind a GM-bypass check (accessLevel < GM_FLIGHT_FREE) before the
                // zone/no-fly checks — no GM/access-level system is ported yet, so every player is gated below.
                if (!_zoneService.IsInsideZoneType(player, ZoneType.Fly))
                {
                    try { await _conn.SendAsync(SM_SYSTEM_MESSAGE.FlyingForbiddenHere(), ct); } catch { }
                    return;
                }
                if (player.UnderNoFly)
                {
                    try { await _conn.SendAsync(SM_SYSTEM_MESSAGE.CantFlyDueToNoFly(), ct); } catch { }
                    return;
                }
                await _flyController.StartFlyAsync(player, _conn, ct);
                break;

            case EmotionType.LAND:
            case EmotionType.LAND_FLYTELEPORT:
                await _flyController.EndFlyAsync(player, forceEndFly: false, _conn, ct);
                break;

            case EmotionType.WALK:
                // Java: cannot toggle walk while flying or gliding
                if (player.FlyState > 0) return;
                player.State |= CreatureState.Walking;
                break;

            default:
                ApplyStateChange(player);
                break;
        }

        int tgtId = _targetObjectId != 0
            ? _targetObjectId
            : player.Target?.ObjectId ?? 0;

        var packet = new SM_EMOTION(player, _emotionType, _emotion, tgtId, _x, _y, _z, _heading);

        // Send to self and broadcast to others in the same zone
        try { await _conn.SendAsync(packet, ct); } catch { }
        int worldId = player.Position.WorldId;
        foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
            if (other.ActivePlayer?.Position.WorldId == worldId)
                try { await other.SendAsync(packet, ct); } catch { }
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
            case EmotionType.POWERSHARD_ON:
                player.State |= CreatureState.Powershard;
                break;
            case EmotionType.POWERSHARD_OFF:
                player.State &= ~CreatureState.Powershard;
                break;
        }
    }
}
