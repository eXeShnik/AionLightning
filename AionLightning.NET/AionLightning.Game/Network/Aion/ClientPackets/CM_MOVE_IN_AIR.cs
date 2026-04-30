using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Sent during fly-teleport movement. Updates server-side position only.
/// Java reference does not broadcast air-movement position fixes — observing clients
/// interpolate the trajectory from the original fly-teleport destination packet.
/// </summary>
public sealed class CM_MOVE_IN_AIR : AionClientPacket
{
    private readonly GsClientConnection _conn;

    private float _x, _y, _z;

    public CM_MOVE_IN_AIR(GsClientConnection conn)
    {
        _conn = conn;
    }

    public override void Read(ref PacketReader r)
    {
        r.ReadD();         // worldId (unused — player's WorldId is tracked server-side)
        _x = r.ReadF();
        _y = r.ReadF();
        _z = r.ReadF();
        r.ReadC();         // locationId (unused)
        r.ReadD();         // distance (unused)
    }

    public override ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is not null)
            player.Position = player.Position with { X = _x, Y = _y, Z = _z };
        return ValueTask.CompletedTask;
    }
}
