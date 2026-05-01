using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client equips or unequips an item. Opcode 0xC4.</summary>
public sealed class CM_EQUIP_ITEM : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IItemDao           _itemDao;
    private readonly IDataManager       _dataManager;
    private readonly PlayerConnectionRegistry _connRegistry;

    private byte _action;   // 0 = equip, 1 = unequip
    private long _slot;
    private int _itemUniqueId;

    public CM_EQUIP_ITEM(GsClientConnection conn, IItemDao itemDao,
        IDataManager dataManager, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _itemDao      = itemDao;
        _dataManager  = dataManager;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        _action       = (byte)r.ReadC();
        _slot         = r.ReadQ();
        _itemUniqueId = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var item = player.Inventory.Get(_itemUniqueId);
        if (item is null) return;

        Item? displaced = null;
        if (_action == 0)
        {
            // Equip: if another item already occupies this slot, move it to the bag first
            displaced = player.Inventory.All
                .FirstOrDefault(i => i.UniqueId != item.UniqueId && i.IsEquipped && i.Slot == (int)_slot);
            if (displaced is not null)
            {
                displaced.Slot       = -1;
                displaced.IsEquipped = false;
            }

            item.Slot       = (int)_slot;
            item.IsEquipped = true;
        }
        else
        {
            // Unequip: move to bag
            item.Slot       = -1;
            item.IsEquipped = false;
        }

        // Update WeaponEquipped state and weapon combat stats
        var mainHandItem = player.Inventory.All.FirstOrDefault(i => i.IsEquipped && i.Slot == 1);
        if (mainHandItem is not null)
        {
            player.State |= CreatureState.WeaponEquipped;
            var weaponTpl = _dataManager.Items.GetTemplate(mainHandItem.ItemId);
            if (weaponTpl?.WeaponStats is { } ws)
            {
                player.MainHandMinDmg     = ws.MinDamage;
                player.MainHandMaxDmg     = ws.MaxDamage;
                player.CurrentAttackSpeed = ws.AttackSpeed > 0 ? ws.AttackSpeed : 1500;
            }
        }
        else
        {
            player.State &= ~CreatureState.WeaponEquipped;
            player.MainHandMinDmg     = 0;
            player.MainHandMaxDmg     = 0;
            player.CurrentAttackSpeed = 1500;
        }

        // Recalculate total physical defense from all currently equipped items
        player.PhysicalDefense = player.Inventory.All
            .Where(i => i.IsEquipped)
            .Sum(i => _dataManager.Items.GetTemplate(i.ItemId)?.PhysicalDefense ?? 0);

        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);

        // Notify self of updated item state(s)
        var changed = displaced is not null ? new[] { item, displaced } : new[] { item };
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM(changed), ct);

        // Refresh stats panel on the client (reflects any changes after equip toggle)
        var tpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
        await _conn.SendAsync(new SM_STATS_INFO(player, tpl, _dataManager.ExpTable), ct);

        // Broadcast appearance change to players in the same zone
        var appearance = new SM_UPDATE_PLAYER_APPEARANCE(player.ObjectId, player.Inventory.All);
        await _conn.SendAsync(appearance, ct);
        int worldId = player.Position.WorldId;
        foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
            if (other.ActivePlayer?.Position.WorldId == worldId)
                try { await other.SendAsync(appearance, ct); } catch { }
    }
}
