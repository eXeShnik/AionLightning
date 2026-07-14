using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_MOVE : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ZoneService _zoneService;
    private readonly FallDamageService _fallDamageService;

    private float _x, _y, _z;
    private byte _heading, _type;
    private float _x2, _y2, _z2;
    private float _vx, _vy, _vz;

    public CM_MOVE(GsClientConnection conn, GameWorld world, PlayerConnectionRegistry connRegistry,
        ZoneService zoneService, FallDamageService fallDamageService)
    {
        _conn              = conn;
        _world             = world;
        _connRegistry      = connRegistry;
        _zoneService       = zoneService;
        _fallDamageService = fallDamageService;
    }

    public override void Read(ref PacketReader r)
    {
        _x       = r.ReadF();
        _y       = r.ReadF();
        _z       = r.ReadF();
        _heading = (byte)r.ReadC();
        _type    = (byte)r.ReadC();

        if ((_type & MovementMask.StartMove) != 0)
        {
            if ((_type & MovementMask.Mouse) == 0)
            {
                _vx = r.ReadF(); _vy = r.ReadF(); _vz = r.ReadF();
                _x2 = _vx + _x; _y2 = _vy + _y; _z2 = _vz + _z;
            }
            else
            {
                _x2 = r.ReadF(); _y2 = r.ReadF(); _z2 = r.ReadF();
            }
        }
        // GLIDE and VEHICLE flags skipped in M5
        // note: Java reads a glideFlag byte here when (type & MovementMask.Glide) != 0, then in runImpl
        // calls FlyController.switchToGliding()/onStopGliding(false) based on that bit every move tick.
        // Left unwired since this Read() doesn't parse the GLIDE sub-field yet — see FlyController.SwitchToGliding's doc.
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        // Java CM_MOVE: reject movement while rooted, stunned, sleeping, etc. (CANT_MOVE_STATE + isUnderFear)
        if ((player.ActiveCcFlags & AbnormalCcFlags.CantMove) != 0) return;

        // Update player state
        player.Position      = player.Position with { X = _x, Y = _y, Z = _z, Heading = _heading };
        player.MovementMask  = _type;
        player.VectorX       = _vx; player.VectorY = _vy; player.VectorZ = _vz;
        player.TargetX2      = _x2; player.TargetY2 = _y2; player.TargetZ2 = _z2;

        await _zoneService.UpdateZonesAsync(player, _conn, ct);

        // Java CM_MOVE: updateFalling/stopFalling run on every movement tick (not just start/stop
        // transitions), so this must happen before the broadcast early-return below.
        if ((_type & MovementMask.Fall) == MovementMask.Fall)
            await _fallDamageService.UpdateFallingAsync(player, _z, _conn, ct);
        else
            await _fallDamageService.StopFallingAsync(player, _z, _conn, ct);

        // Only broadcast on movement-state transitions (start or stop).
        // Mid-movement position fixes are NOT broadcast — observing clients interpolate from the start packet.
        if ((_type & MovementMask.StartMove) == 0 && _type != 0) return;

        var scope      = player.Position;
        var movePacket = new SM_MOVE(player);
        foreach (var otherConn in _connRegistry.GetAllExcept(player.ObjectId))
            if (otherConn.ActivePlayer is { } op && op.Position.SameScope(scope))
                try { await otherConn.SendAsync(movePacket, ct); } catch { }
    }
}
