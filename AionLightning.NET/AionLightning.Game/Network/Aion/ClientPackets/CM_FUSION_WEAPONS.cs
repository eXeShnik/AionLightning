using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client fuses two two-hand weapons at an NPC. Opcode 0x16C.
/// The primary (first) weapon retains its stats; the secondary (second) is consumed.
/// FusionedItemId on the primary records the secondary's itemId.
/// Mirrors Java ArmsfusionService.fusionWeapons (simplified: no fusion-stone transfer).
/// </summary>
public sealed class CM_FUSION_WEAPONS : AionClientPacket
{
    private const int KinahItemId = 182400001;

    private readonly GsClientConnection       _conn;
    private readonly IItemDao                 _itemDao;
    private readonly IDataManager             _dataManager;

    private int _unk;
    private int _firstItemObjId;
    private int _secondItemObjId;

    public CM_FUSION_WEAPONS(GsClientConnection conn, IItemDao itemDao, IDataManager dataManager)
    {
        _conn        = conn;
        _itemDao     = itemDao;
        _dataManager = dataManager;
    }

    public override void Read(ref PacketReader r)
    {
        _unk             = r.ReadD();
        _firstItemObjId  = r.ReadD();
        _secondItemObjId = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var first  = player.Inventory.Get(_firstItemObjId);
        var second = player.Inventory.Get(_secondItemObjId);
        if (first is null || second is null) return;

        var firstTpl  = _dataManager.Items.GetTemplate(first.ItemId);
        var secondTpl = _dataManager.Items.GetTemplate(second.ItemId);
        if (firstTpl is null || secondTpl is null) return;

        // Both must be two-hand weapons that support fusion
        if (!firstTpl.IsTwoHandWeapon || !firstTpl.IsWeapon)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.CompoundNotAvailable(), ct);
            return;
        }

        // Neither may already be fused
        if (first.FusionedItemId != 0)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.CompoundNotAvailable(), ct);
            return;
        }
        if (second.FusionedItemId != 0)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.CompoundNotAvailable(), ct);
            return;
        }

        // Same weapon type required
        if (firstTpl.WeaponTypeName != secondTpl.WeaponTypeName)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.CompoundDifferentType(), ct);
            return;
        }

        // Primary must have level >= secondary (mirrors Java check)
        if (secondTpl.Level > firstTpl.Level)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.CompoundMainRequireHigherLevel(), ct);
            return;
        }

        // Kinah cost: level² * 2 (simplified from Java formula)
        long cost = (long)firstTpl.Level * firstTpl.Level * 2;
        var kinahItem = player.Inventory.FindByItemId(KinahItemId);
        if (kinahItem is null || kinahItem.Count < cost)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.CompoundNotEnoughMoney(), ct);
            return;
        }

        // Deduct kinah
        kinahItem.Count -= cost;

        // Set fusion on primary
        first.FusionedItemId = second.ItemId;

        // Consume secondary
        second.Count--;
        if (second.Count <= 0)
        {
            player.Inventory.Remove(second.UniqueId);
            await _itemDao.DeleteAsync(second.UniqueId, ct);
            await _conn.SendAsync(new SM_DELETE_ITEM(second.UniqueId), ct);
        }
        else
        {
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([second]), ct);
        }

        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([first, kinahItem]), ct);
        await _conn.SendAsync(SM_SYSTEM_MESSAGE.CompoundSuccess(), ct);
    }
}
