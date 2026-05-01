using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client buys from or sells to an NPC shop. Opcode 0xF1.</summary>
public sealed class CM_BUY_ITEM : AionClientPacket
{
    private const int   KinahItemId       = 182400001;
    private const int   SellPriceDivisor  = 2;    // players get 50% of item price when selling
    private const float MaxInteractRange  = 10.0f; // lenient for latency; retail NPC interaction is ~5m

    private readonly GsClientConnection _conn;
    private readonly IItemDao _itemDao;
    private readonly IDataManager _dataManager;
    private readonly GameWorld _world;

    private int _sellerObjectId;
    private int _tradeActionId;
    private readonly List<(int ItemId, long Count)> _tradeEntries = new();
    private bool _invalid;

    public CM_BUY_ITEM(GsClientConnection conn, IItemDao itemDao, IDataManager dataManager, GameWorld world)
    {
        _conn        = conn;
        _itemDao     = itemDao;
        _dataManager = dataManager;
        _world       = world;
    }

    public override void Read(ref PacketReader r)
    {
        _sellerObjectId = r.ReadD();
        _tradeActionId  = r.ReadH();
        int amount      = r.ReadH();

        if (amount is < 0 or > 36) { _invalid = true; return; }

        for (int i = 0; i < amount; i++)
        {
            int  itemId = r.ReadD();
            long count  = r.ReadQ();
            if (count <= 0 || count > 20000 || itemId <= 0) { _invalid = true; return; }
            _tradeEntries.Add((itemId, count));
        }
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (_invalid) return;
        var player = _conn.ActivePlayer;
        if (player is null) return;

        switch (_tradeActionId)
        {
            case 13: // buy from normal NPC shop
                await BuyFromShopAsync(player, ct);
                break;
            case 1: // sell items to NPC shop
                await SellToShopAsync(player, ct);
                break;
        }
    }

    private async ValueTask BuyFromShopAsync(Player player, CancellationToken ct)
    {
        var npc = _world.GetNpcByObjectId(_sellerObjectId);
        if (npc is null) return;
        if (player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;

        // Single-pass: filter to items that can be received and accumulate cost.
        // CanReceive is evaluated in order so that each accepted item correctly
        // consumes a simulated slot before the next item is checked.
        var purchasePlan = new List<(int ItemId, long Count, long Cost, int MaxStack)>();
        var simulatedInventory = player.Inventory; // checks run against live inventory first
        int simulatedSlots = simulatedInventory.BagSlotUsed;

        foreach (var (itemId, count) in _tradeEntries)
        {
            if (!_dataManager.Shop.NpcSellsItem(npc.Template.NpcId, itemId)) continue;
            var template = _dataManager.Items.GetTemplate(itemId);
            if (template is null) continue;

            // Capacity check: stackable items that already have a stack don't consume a slot.
            bool hasExisting = simulatedInventory.FindByItemId(itemId) is not null;
            bool canStack    = hasExisting && template.MaxStackCount > 1;
            if (!canStack)
            {
                if (simulatedSlots >= simulatedInventory.Capacity) continue; // full
                simulatedSlots++;
            }

            purchasePlan.Add((itemId, count, template.Price * count, template.MaxStackCount));
        }

        if (purchasePlan.Count == 0) return;

        long totalCost = purchasePlan.Sum(p => p.Cost);

        // Check player has enough kinah
        var kinahItem = player.Inventory.FindByItemId(KinahItemId);
        long currentKinah = kinahItem?.Count ?? 0;
        if (currentKinah < totalCost)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.NoEnoughKinah(), ct);
            return;
        }

        // Deduct kinah in memory
        if (kinahItem is not null)
            kinahItem.Count -= totalCost;

        // Add purchased items (always fit because plan was validated above)
        var itemsAdded = new List<Item>();
        foreach (var (itemId, count, _, maxStack) in purchasePlan)
        {
            var existing = player.Inventory.FindByItemId(itemId);
            if (existing is not null && maxStack > 1)
            {
                existing.Count += count;
                itemsAdded.Add(existing);
            }
            else
            {
                long uid = await _itemDao.NextUniqueIdAsync(ct);
                var newItem = new Item { UniqueId = uid, ItemId = itemId, Count = count, Slot = -1 };
                player.Inventory.Add(newItem);
                itemsAdded.Add(newItem);
            }
        }

        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);

        if (kinahItem is not null)
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinahItem]), ct);
        if (itemsAdded.Count > 0)
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM(itemsAdded), ct);
    }

    private async ValueTask SellToShopAsync(Player player, CancellationToken ct)
    {
        var npc = _world.GetNpcByObjectId(_sellerObjectId);
        if (npc is null || player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;

        long kinahGained = 0;
        var itemsToDelete = new List<long>();
        var itemsToUpdate = new List<Item>();

        foreach (var (itemId, count) in _tradeEntries)
        {
            var template = _dataManager.Items.GetTemplate(itemId);
            long sellPrice = (template?.Price ?? 0) / SellPriceDivisor;

            var item = player.Inventory.FindByItemId(itemId);
            if (item is null || item.IsEquipped) continue;

            long sellCount = Math.Min(count, item.Count);
            kinahGained += sellPrice * sellCount;
            item.Count -= sellCount;

            if (item.Count <= 0)
            {
                player.Inventory.Remove(item.UniqueId);
                itemsToDelete.Add(item.UniqueId);
            }
            else
            {
                itemsToUpdate.Add(item);
            }
        }

        // Add kinah gained in memory
        Item? kinahItem = null;
        if (kinahGained > 0)
        {
            kinahItem = player.Inventory.FindByItemId(KinahItemId);
            if (kinahItem is null)
            {
                long uid = await _itemDao.NextUniqueIdAsync(ct);
                kinahItem = new Item { UniqueId = uid, ItemId = KinahItemId, Count = kinahGained, Slot = -1 };
                player.Inventory.Add(kinahItem);
            }
            else
            {
                kinahItem.Count += kinahGained;
            }
        }

        // Persist full inventory state (covers deletions, updates, and kinah in one shot)
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);

        // Notify client of removed items
        foreach (var uid in itemsToDelete)
            await _conn.SendAsync(new SM_DELETE_ITEM(uid), ct);

        if (itemsToUpdate.Count > 0)
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM(itemsToUpdate), ct);
        if (kinahItem is not null)
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinahItem]), ct);
    }
}
