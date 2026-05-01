using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client requests item tuning (optional socket assignment). Opcode 0x189.
/// tuningScrollId == 0: free initial tuning (only when OptionalSocket == −1).
/// tuningScrollId != 0: scroll-based re-tuning (stub — TuningAction not yet ported).
/// Mirrors Java CM_TUNE: assigns a random value 0..OptionSlotBonus to OptionalSocket
/// and persists; broadcasts SM_INVENTORY_ADD_ITEM with updated item.
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

        // Scroll-based re-tuning: stub (TuningAction not yet ported)
        if (_tuningScrollId != 0) return;

        // Find item in inventory or equipped slots
        var item = player.Inventory.Get(_itemObjectId)
                ?? player.Inventory.All.FirstOrDefault(i => i.IsEquipped && i.UniqueId == _itemObjectId);
        if (item is null) return;

        // Already tuned — initial free tuning only allowed once
        if (item.OptionalSocket != -1) return;

        var template = _dataManager.Items.GetTemplate(item.ItemId);
        if (template is null || template.OptionSlotBonus <= 0) return;

        // Assign random value 0..OptionSlotBonus (mirrors Java Rnd.get(0, getOptionSlotBonus()))
        item.OptionalSocket = Random.Shared.Next(0, template.OptionSlotBonus + 1);

        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
        await _conn.SendAsync(SM_SYSTEM_MESSAGE.TuningComplete(), ct);
    }
}
