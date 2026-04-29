using AionLightning.Commons.Events;
using AionLightning.Commons.Network;
using AionLightning.Game.Events;
using AionLightning.Game.Network.Aion.ServerPackets;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_LEVEL_READY : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IEventBus _eventBus;

    public CM_LEVEL_READY(GsClientConnection conn, GameWorld world,
        PlayerConnectionRegistry connRegistry, IEventBus eventBus)
    {
        _conn         = conn;
        _world        = world;
        _connRegistry = connRegistry;
        _eventBus     = eventBus;
    }

    public override void Read(ref PacketReader r) { }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        // Send this player's info to themselves
        await _conn.SendAsync(new SM_PLAYER_INFO(player, player.Appearance, enemy: false), ct);

        // Introduce each already-online player to the newcomer and vice versa
        foreach (var otherConn in _connRegistry.GetAllExcept(player.ObjectId))
        {
            var other = otherConn.ActivePlayer;
            if (other is null) continue;

            // New player sees existing player
            await _conn.SendAsync(new SM_PLAYER_INFO(other, other.Appearance, enemy: false), ct);

            // Existing player sees new player
            await otherConn.SendAsync(new SM_PLAYER_INFO(player, player.Appearance, enemy: false), ct);
        }

        await _eventBus.PublishAsync(new PlayerEnteredWorldEvent(player), ct);
    }
}
