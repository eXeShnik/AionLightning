using AionLightning.Commons.Network;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using AionLightning.Game.Dao;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client takes an item from a loot window. Opcode 0x179.</summary>
public sealed class CM_LOOT_ITEM : AionClientPacket
{
    private const int KinahItemId = 182400001;

    private readonly GsClientConnection       _conn;
    private readonly LootService              _lootService;
    private readonly IItemDao                 _itemDao;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly QuestService             _questService;

    private int _targetObjectId;
    private byte _index;

    public CM_LOOT_ITEM(GsClientConnection conn, LootService lootService, IItemDao itemDao,
        PlayerConnectionRegistry connRegistry, QuestService questService)
    {
        _conn         = conn;
        _lootService  = lootService;
        _itemDao      = itemDao;
        _connRegistry = connRegistry;
        _questService = questService;
    }

    public override void Read(ref PacketReader r)
    {
        _targetObjectId = r.ReadD();
        _index          = (byte)r.ReadC();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var entry = _lootService.TakeLootAt(_targetObjectId, _index);
        if (entry is null) return;

        Item notifyItem;
        if (entry.ItemId == KinahItemId)
        {
            var kinahItem = player.Inventory.FindByItemId(KinahItemId);
            if (kinahItem is null)
            {
                long uid = await _itemDao.NextUniqueIdAsync(ct);
                kinahItem = new Item { UniqueId = uid, ItemId = KinahItemId, Count = entry.Count, Slot = -1 };
                player.Inventory.Add(kinahItem);
            }
            else
            {
                kinahItem.Count += entry.Count;
            }
            notifyItem = kinahItem;
        }
        else
        {
            var existing = player.Inventory.FindByItemId(entry.ItemId);
            if (existing is null && !player.Inventory.HasFreeSlot)
            {
                // Inventory full — put the item back so the player can try again after making room
                _lootService.ReturnLoot(_targetObjectId, _index, entry);
                await _conn.SendAsync(SM_SYSTEM_MESSAGE.InventoryFull(), ct);
                return;
            }

            if (existing is not null)
            {
                existing.Count += entry.Count;
                notifyItem = existing;
            }
            else
            {
                long uid = await _itemDao.NextUniqueIdAsync(ct);
                var item = new Item { UniqueId = uid, ItemId = entry.ItemId, Count = entry.Count, Slot = -1 };
                player.Inventory.Add(item);
                notifyItem = item;
            }
        }

        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([notifyItem]), ct);

        // Check quest collect-item progress after looting non-kinah items
        if (entry.ItemId != KinahItemId)
            await _questService.HandleItemAcquiredAsync(player, entry.ItemId, _conn, ct);

        // When loot is empty, broadcast close to all players in the zone so every open loot window closes
        if (_lootService.GetLoot(_targetObjectId) is null)
        {
            int worldId     = player.Position.WorldId;
            var closePacket = new SM_LOOT_STATUS(_targetObjectId, SM_LOOT_STATUS.State.Close);
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer?.Position.WorldId == worldId)
                    try { await conn.SendAsync(closePacket, ct); } catch { }
        }
    }
}
