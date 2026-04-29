using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_MOVE : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;

    private float _x, _y, _z;
    private byte _heading, _type;
    private float _x2, _y2, _z2;
    private float _vx, _vy, _vz;

    public CM_MOVE(GsClientConnection conn, GameWorld world, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _world        = world;
        _connRegistry = connRegistry;
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
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        // Update player state
        player.Position      = player.Position with { X = _x, Y = _y, Z = _z, Heading = _heading };
        player.MovementMask  = _type;
        player.VectorX       = _vx; player.VectorY = _vy; player.VectorZ = _vz;
        player.TargetX2      = _x2; player.TargetY2 = _y2; player.TargetZ2 = _z2;

        // Broadcast movement to all other online players
        var movePacket = new SM_MOVE(player);
        foreach (var otherConn in _connRegistry.GetAllExcept(player.ObjectId))
            await otherConn.SendAsync(movePacket, ct);
    }
}
