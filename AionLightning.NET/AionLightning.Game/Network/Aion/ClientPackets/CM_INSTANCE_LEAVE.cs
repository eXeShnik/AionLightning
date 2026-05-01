using AionLightning.Commons.Network;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client leaves an instance. Teleports player to the race-specific exit location. Opcode 0xCC.</summary>
public sealed class CM_INSTANCE_LEAVE : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly GameWorld                _world;
    private readonly IDataManager             _dataManager;
    private readonly PlayerConnectionRegistry _connRegistry;

    public CM_INSTANCE_LEAVE(GsClientConnection conn, GameWorld world,
        IDataManager dataManager, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _world        = world;
        _dataManager  = dataManager;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r) { }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var exit = _dataManager.InstanceExits.GetExit(player.Position.WorldId, player.Race);
        if (exit is null) return;

        int oldWorldId = player.Position.WorldId;
        var deletePacket = new SM_DELETE(player.ObjectId, time: 11);
        foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
            if (other.ActivePlayer?.Position.WorldId == oldWorldId)
                try { await other.SendAsync(deletePacket, ct); } catch { }

        player.Position = new Position(exit.Value.X, exit.Value.Y, exit.Value.Z,
            exit.Value.Heading, exit.Value.ExitWorldId);
        await _conn.SendAsync(new SM_TELEPORT_LOC(player.Position, portAnimation: 0), ct);
        _ = SchedulePostTeleportAsync(player, oldWorldId, ct);
    }

    private async Task SchedulePostTeleportAsync(Player player, int oldWorldId, CancellationToken ct)
    {
        try
        {
            await Task.Delay(2200, ct);

            if (oldWorldId != player.Position.WorldId)
            {
                await _conn.SendAsync(new SM_CHANNEL_INFO(), ct);
                await _conn.SendAsync(new SM_PLAYER_SPAWN(player), ct);
            }
            else
            {
                var equipment = player.Inventory.All.Where(i => i.IsEquipped).ToList();
                await _conn.SendAsync(new SM_PLAYER_INFO(player, player.Appearance, enemy: false, equipment), ct);
                await _conn.SendAsync(new SM_STATS_INFO(player), ct);
                await _conn.SendAsync(SM_MOTION.OwnList(player.ActiveMotions), ct);

                int newWorldId = player.Position.WorldId;
                var peerInfo   = new SM_PLAYER_INFO(player, player.Appearance, enemy: false, equipment);
                foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
                    if (other.ActivePlayer?.Position.WorldId == newWorldId)
                        try { await other.SendAsync(peerInfo, ct); } catch { }
            }
        }
        catch (OperationCanceledException) { }
    }
}
