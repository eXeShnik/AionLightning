using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client requests item remodeling (skin change). Opcode 0x138.
/// keepItem retains its stats but adopts the appearance of extractItem.
/// extractItem is consumed. Both must share the same equipment slot.
/// Mirrors Java ItemRemodelService.remodelItem.
/// </summary>
public sealed class CM_ITEM_REMODEL : AionClientPacket
{
    private const long RemodelCost = 1_000;
    private const int  KinahItemId = 182400001;

    private readonly GsClientConnection       _conn;
    private readonly IItemDao                 _itemDao;
    private readonly IDataManager             _dataManager;
    private readonly PlayerConnectionRegistry _connRegistry;

    private int _npcObjectId;
    private int _keepItemObjId;
    private int _extractItemObjId;

    public CM_ITEM_REMODEL(GsClientConnection conn, IItemDao itemDao,
        IDataManager dataManager, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _itemDao      = itemDao;
        _dataManager  = dataManager;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        _npcObjectId      = r.ReadD();
        _keepItemObjId    = r.ReadD();
        _extractItemObjId = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var keepItem    = player.Inventory.Get(_keepItemObjId);
        var extractItem = player.Inventory.Get(_extractItemObjId);
        if (keepItem is null || extractItem is null) return;

        if (player.Level < 10)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.RemodelLevelLimit(), ct);
            return;
        }

        var kinahItem = player.Inventory.FindByItemId(KinahItemId);
        if (kinahItem is null || kinahItem.Count < RemodelCost)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.RemodelNoKinah(), ct);
            return;
        }

        var keepTemplate    = _dataManager.Items.GetTemplate(keepItem.ItemId);
        // Use extractItem's current skin template for compatibility check (mirrors Java getItemSkinTemplate)
        int extractSkinId   = extractItem.SkinItemId != 0 ? extractItem.SkinItemId : extractItem.ItemId;
        var extractTemplate = _dataManager.Items.GetTemplate(extractSkinId);
        if (keepTemplate is null || extractTemplate is null) return;

        if (keepTemplate.Slot == 0 || keepTemplate.Slot != extractTemplate.Slot)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.RemodelNotCompatible(), ct);
            return;
        }

        // Deduct kinah
        kinahItem.Count -= RemodelCost;

        // Consume extract item
        extractItem.Count--;
        if (extractItem.Count <= 0)
        {
            player.Inventory.Remove(extractItem.UniqueId);
            await _itemDao.DeleteAsync(extractItem.UniqueId, ct);
            await _conn.SendAsync(new SM_DELETE_ITEM(extractItem.UniqueId), ct);
        }
        else
        {
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([extractItem]), ct);
        }

        // Apply skin
        keepItem.SkinItemId = extractSkinId;
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);

        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([keepItem, kinahItem]), ct);
        await _conn.SendAsync(SM_SYSTEM_MESSAGE.RemodelSuccess(), ct);

        // Broadcast updated appearance if keep item is equipped
        if (keepItem.IsEquipped)
        {
            int worldId    = player.Position.WorldId;
            var appearance = new SM_UPDATE_PLAYER_APPEARANCE(player.ObjectId, player.Inventory.All);
            try { await _conn.SendAsync(appearance, ct); } catch { }
            foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
                if (other.ActivePlayer?.Position.WorldId == worldId)
                    try { await other.SendAsync(appearance, ct); } catch { }
        }
    }
}
