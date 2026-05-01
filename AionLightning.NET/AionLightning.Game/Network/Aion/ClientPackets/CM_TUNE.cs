using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client requests item tuning (optional socket assignment). Opcode 0x189.
/// tuningScrollId == 0: free initial tuning (only when OptionalSocket == −1).
/// tuningScrollId != 0: scroll-based re-tuning — consumes scroll, re-randomises OptionalSocket.
/// Mirrors Java CM_TUNE / TuningAction: assigns a random value 0..OptionSlotBonus.
/// </summary>
public sealed class CM_TUNE : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IItemDao           _itemDao;
    private readonly IDataManager       _dataManager;

    private int _itemObjectId;
    private int _tuningScrollId;

    public CM_TUNE(GsClientConnection conn, IItemDao itemDao, IDataManager dataManager)
    {
        _conn        = conn;
        _itemDao     = itemDao;
        _dataManager = dataManager;
    }

    public override void Read(ref PacketReader r)
    {
        _itemObjectId   = r.ReadD();
        _tuningScrollId = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        if (_tuningScrollId != 0)
        {
            await HandleScrollTuningAsync(player, ct);
            return;
        }

        // Free initial tuning — only when item has never been tuned
        var item = player.Inventory.Get(_itemObjectId)
                ?? player.Inventory.All.FirstOrDefault(i => i.IsEquipped && i.UniqueId == _itemObjectId);
        if (item is null) return;
        if (item.OptionalSocket != -1) return;

        var template = _dataManager.Items.GetTemplate(item.ItemId);
        if (template is null || template.OptionSlotBonus <= 0) return;

        item.OptionalSocket = Random.Shared.Next(0, template.OptionSlotBonus + 1);
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
        await _conn.SendAsync(SM_SYSTEM_MESSAGE.TuningComplete(), ct);
    }

    private async ValueTask HandleScrollTuningAsync(Model.Player player, CancellationToken ct)
    {
        var scroll = player.Inventory.Get(_tuningScrollId);
        if (scroll is null) return;

        var scrollTemplate = _dataManager.Items.GetTemplate(scroll.ItemId);
        if (scrollTemplate is null || !scrollTemplate.IsTuningScroll) return;

        var target = player.Inventory.Get(_itemObjectId)
                  ?? player.Inventory.All.FirstOrDefault(i => i.IsEquipped && i.UniqueId == _itemObjectId);
        if (target is null) return;

        var targetTemplate = _dataManager.Items.GetTemplate(target.ItemId);
        if (targetTemplate is null || targetTemplate.OptionSlotBonus <= 0) return;

        // Validate scroll can be used on this item type (mirrors Java TuningAction.UseTarget check)
        string scrollTarget = scrollTemplate.Actions?.Tuning?.Target ?? string.Empty;
        bool targetOk = scrollTarget == "EQUIPMENT"
                     || (scrollTarget == "WEAPON" && targetTemplate.IsWeapon)
                     || (scrollTarget == "ARMOR"  && targetTemplate.IsArmor);
        if (!targetOk) return;

        // Consume scroll
        scroll.Count--;
        if (scroll.Count <= 0)
        {
            player.Inventory.Remove(scroll.UniqueId);
            await _itemDao.DeleteAsync(scroll.UniqueId, ct);
            await _conn.SendAsync(new SM_DELETE_ITEM(scroll.UniqueId), ct);
        }
        else
        {
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([scroll]), ct);
        }

        // Re-assign OptionalSocket (reset then randomise — mirrors Java TuningAction.act())
        target.OptionalSocket = Random.Shared.Next(0, targetTemplate.OptionSlotBonus + 1);
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([target]), ct);
        await _conn.SendAsync(SM_SYSTEM_MESSAGE.TuningComplete(), ct);
    }
}
