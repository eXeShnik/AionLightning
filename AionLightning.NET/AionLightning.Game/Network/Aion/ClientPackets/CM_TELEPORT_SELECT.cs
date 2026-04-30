using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Player selects a teleport destination from a teleporter NPC. Opcode 0x176.</summary>
public sealed class CM_TELEPORT_SELECT : AionClientPacket
{
    private const int KinahItemId = 182400001;

    private readonly GsClientConnection       _conn;
    private readonly GameWorld                _world;
    private readonly IDataManager             _dataManager;
    private readonly IItemDao                 _itemDao;
    private readonly PlayerConnectionRegistry _connRegistry;

    private int _targetObjectId;
    private int _locId;

    public CM_TELEPORT_SELECT(GsClientConnection conn, GameWorld world,
        IDataManager dataManager, IItemDao itemDao, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _world        = world;
        _dataManager  = dataManager;
        _itemDao      = itemDao;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        _targetObjectId = r.ReadD();
        _locId          = r.ReadD();
        r.ReadH(); // unk
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || player.IsAlreadyDead) return;

        var npc = _world.GetNpcByObjectId(_targetObjectId);
        if (npc is null) return;

        var dest = _dataManager.Teleports.GetDestination(npc.Template.NpcId, _locId);
        if (dest is null) return;

        var loc = _dataManager.Teleports.GetLocation(_locId);
        if (loc is null || (loc.X == 0f && loc.Y == 0f)) return; // flying-only locations lack coordinates

        // Deduct kinah; send as update (not "item acquired") so the client shows a balance change, not pickup UI
        if (dest.Price > 0)
        {
            var kinah = player.Inventory.FindByItemId(KinahItemId);
            if (kinah is null || kinah.Count < dest.Price) return;

            kinah.Count -= dest.Price;
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            // SM_INVENTORY_ADD_ITEM doubles as an update packet for existing stacks
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinah]), ct);
        }

        // Always notify old-zone peers of departure with jump-animation SM_DELETE (time=11),
        // regardless of whether the destination is on the same or a different map.
        int oldWorldId = player.Position.WorldId;
        {
            var deletePacket = new SM_DELETE(player.ObjectId, time: 11);
            foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
                if (other.ActivePlayer?.Position.WorldId == oldWorldId)
                    try { await other.SendAsync(deletePacket, ct); } catch { }
        }

        // Update server-side player position
        player.Position = new Position(loc.X, loc.Y, loc.Z, loc.Heading, loc.MapId);

        // Send teleport command to client; portAnimation=0 = jump/portal animation
        await _conn.SendAsync(new SM_TELEPORT_LOC(loc.MapId, loc.X, loc.Y, loc.Z, loc.Heading, portAnimation: 0), ct);

        // After the client-side animation (~2200ms), send the post-teleport spawn packets
        _ = SchedulePostTeleportAsync(player, oldWorldId, ct);
    }

    private async Task SchedulePostTeleportAsync(Player player, int oldWorldId, CancellationToken ct)
    {
        try
        {
            await Task.Delay(2200, ct);

            if (oldWorldId != player.Position.WorldId)
            {
                // Cross-map: tell client to render the new zone; CM_LEVEL_READY from client will follow
                // and broadcast SM_PLAYER_INFO to peers in the new zone.
                await _conn.SendAsync(new SM_CHANNEL_INFO(), ct);
                await _conn.SendAsync(new SM_PLAYER_SPAWN(player), ct);
            }
            else
            {
                // Same-map: no full zone reload — re-sync self and broadcast new position to zone peers.
                var equipment = player.Inventory.All.Where(i => i.IsEquipped).ToList();
                await _conn.SendAsync(new SM_PLAYER_INFO(player, player.Appearance, enemy: false, equipment), ct);
                await _conn.SendAsync(new SM_STATS_INFO(player), ct);
                await _conn.SendAsync(SM_MOTION.OwnList(), ct);

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
