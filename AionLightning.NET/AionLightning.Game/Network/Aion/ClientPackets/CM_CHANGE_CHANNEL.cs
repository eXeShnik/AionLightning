using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client changes the zone channel (instance). Opcode 0x14E.</summary>
public sealed class CM_CHANGE_CHANNEL : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly GameWorld                _world;

    private int _channel;

    public CM_CHANGE_CHANNEL(GsClientConnection conn, PlayerConnectionRegistry connRegistry, GameWorld world)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
        _world        = world;
    }

    public override void Read(ref PacketReader r) => _channel = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        // Broadcast despawn to others in current world before switching
        var deletePacket = new SM_DELETE(player.ObjectId, 0);
        int worldId = player.Position.WorldId;
        foreach (var other in _connRegistry.GetAll())
        {
            if (other == _conn) continue;
            if (other.ActivePlayer?.Position.WorldId != worldId) continue;
            try { await other.SendAsync(deletePacket, ct); } catch { }
        }

        // Java stores channel as instanceId = channel + 1 (1-based)
        player.Position = player.Position with { InstanceId = _channel + 1 };

        await _conn.SendAsync(new SM_CHANNEL_INFO(_channel, instanceCount: 2), ct);
        await _conn.SendAsync(new SM_PLAYER_SPAWN(player), ct);
    }
}
