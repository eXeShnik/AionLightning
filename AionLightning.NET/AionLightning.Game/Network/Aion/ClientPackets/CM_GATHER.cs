using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Player starts (action=0) or finishes (action!=0) gathering. Opcode 0xD1.</summary>
public sealed class CM_GATHER : AionClientPacket
{
    private const int RespawnSeconds = 300;

    private readonly GsClientConnection       _conn;
    private readonly GameWorld                _world;
    private readonly GatherService            _gatherService;
    private readonly SpawnService             _spawnService;
    private readonly IItemDao                 _itemDao;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ExperienceService        _expService;

    private int _action;

    public CM_GATHER(GsClientConnection conn, GameWorld world, GatherService gatherService,
        SpawnService spawnService, IItemDao itemDao, PlayerConnectionRegistry connRegistry,
        ExperienceService expService)
    {
        _conn          = conn;
        _world         = world;
        _gatherService = gatherService;
        _spawnService  = spawnService;
        _itemDao       = itemDao;
        _connRegistry  = connRegistry;
        _expService    = expService;
    }

    public override void Read(ref PacketReader r) => _action = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        // The player must have a Gatherable selected as their target
        var target = player.Target as Gatherable;
        if (target is null) return;

        if (_action == 0)
            await HandleStartAsync(player, target, ct);
        else
            await HandleFinishAsync(player, target, ct);
    }

    private async ValueTask HandleStartAsync(Player player, Gatherable target, CancellationToken ct)
    {
        if (target.IsGathered) return;

        // Roll material once here; the same result is stored in GatherService and used in HandleFinish
        // so the player cannot receive a different item than the one shown in the gather animation.
        var material = target.Template.PickMaterial();
        if (material is null) return;

        if (!_gatherService.StartGathering(player.ObjectId, target.ObjectId, material)) return;

        await _conn.SendAsync(new SM_USE_OBJECT(player.ObjectId, target.ObjectId, 3000, 1), ct);
        await _conn.SendAsync(new SM_GATHER_STATUS(player.ObjectId, target.ObjectId, SM_GATHER_STATUS.Status.Start), ct);
        await _conn.SendAsync(new SM_GATHER_UPDATE(target.Template, material, 100, 0, 0), ct);
    }

    private async ValueTask HandleFinishAsync(Player player, Gatherable target, CancellationToken ct)
    {
        var session = _gatherService.GetSession(player.ObjectId);
        if (session is null || session.GatherableObjectId != target.ObjectId) return;
        var material = session.Material;
        _gatherService.StopGathering(player.ObjectId);

        if (target.IsGathered) return;

        // Check inventory capacity before consuming the harvest charge
        var existing = player.Inventory.FindByItemId(material.ItemId);
        if (existing is null && !player.Inventory.HasFreeSlot)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.InventoryFull(), ct);
            return;
        }

        target.HarvestsRemaining--;

        Item gathered;
        if (existing is not null)
        {
            existing.Count++;
            gathered = existing;
        }
        else
        {
            long uid = await _itemDao.NextUniqueIdAsync(ct);
            gathered = new Item { UniqueId = uid, ItemId = material.ItemId, Count = 1, Slot = -1 };
            player.Inventory.Add(gathered);
        }
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([gathered]), ct);

        await _conn.SendAsync(new SM_GATHER_STATUS(player.ObjectId, target.ObjectId, SM_GATHER_STATUS.Status.Success), ct);
        await _conn.SendAsync(new SM_GATHER_UPDATE(target.Template, material, 100, 0, 7), ct);

        // Award gathering XP based on the node's required skill level; notify player
        await _expService.AddGatheringExpAsync(player, target.Template.SkillLevel, _conn, ct);
        await _conn.SendAsync(SM_SYSTEM_MESSAGE.GatheringSuccessExp(), ct);

        if (target.IsGathered)
        {
            _world.Remove(target);
            var deletePacket = new SM_DELETE(target.ObjectId, 0);
            int worldId = target.Position.WorldId;
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer?.Position.WorldId == worldId)
                    try { await conn.SendAsync(deletePacket, ct); } catch { }

            _spawnService.ScheduleGatherableRespawn(target, RespawnSeconds);
        }
    }
}
