using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>
/// Weapon fusion (composite weapons). Mirrors Java <c>services.ArmsfusionService</c>: fuses two
/// compatible two-hand weapons by consuming the second into the first's <see cref="Item.FusionedItemId"/>,
/// and reverses that with <see cref="BreakFusionAsync"/>. The fused weapon's stat modifiers are then
/// added on top of the main weapon's own stats — see <see cref="EquipStatsCalculator"/>.
/// Kinah price uses the same simplified level²-based formula as the original port (no Java
/// <c>PricesService</c> race-tax/global-price data has been ported); fusion-stone transfer
/// (<c>ItemSocketService.copyFusionStones</c>/<c>removeAllFusionStone</c>) and the improvement/charge-way
/// (Conditioning vs Augmenting) compatibility check are not ported — no equivalent subsystem exists yet.
/// </summary>
public sealed class ArmsfusionService
{
    private const int KinahItemId = 182400001;

    private readonly IItemDao _itemDao;
    private readonly IDataManager _dataManager;
    private readonly ILogger<ArmsfusionService> _logger;

    public ArmsfusionService(IItemDao itemDao, IDataManager dataManager, ILogger<ArmsfusionService> logger)
    {
        _itemDao     = itemDao;
        _dataManager = dataManager;
        _logger      = logger;
    }

    /// <summary>
    /// Fuses <paramref name="fusionWeaponUid"/> into <paramref name="mainWeaponUid"/>: validates both are
    /// fusable two-hand weapons of the same weapon type, neither already fused, and the fusion weapon's
    /// level does not exceed the main weapon's, then charges kinah and consumes the fusion weapon.
    /// Returns false (no state change) on any validation failure — never dupes or loses an item.
    /// </summary>
    public async ValueTask<bool> FuseAsync(Player player, long mainWeaponUid, long fusionWeaponUid,
        GsClientConnection conn, CancellationToken ct)
    {
        var main   = player.Inventory.Get(mainWeaponUid);
        var fusion = player.Inventory.Get(fusionWeaponUid);
        if (main is null || fusion is null) return false;

        var mainTpl   = _dataManager.Items.GetTemplate(main.ItemId);
        var fusionTpl = _dataManager.Items.GetTemplate(fusion.ItemId);
        if (mainTpl is null || fusionTpl is null) return false;

        // Both items must be explicitly fusable (presence of <fusionaction> in item XML).
        if (!mainTpl.IsCanFuse || !fusionTpl.IsCanFuse)
        {
            _logger.LogWarning("[AUDIT] Client hack with item fusion, player: {Name}", player.Name);
            return false;
        }

        if (!mainTpl.IsTwoHandWeapon)
        {
            await conn.SendAsync(SM_SYSTEM_MESSAGE.CompoundNotAvailable(), ct);
            return false;
        }

        // Fusioned weapons must not already be fused.
        if (main.FusionedItemId != 0 || fusion.FusionedItemId != 0)
        {
            await conn.SendAsync(SM_SYSTEM_MESSAGE.CompoundNotAvailable(), ct);
            return false;
        }

        // Fusioned weapons must have the same weapon type.
        if (mainTpl.WeaponTypeName != fusionTpl.WeaponTypeName)
        {
            await conn.SendAsync(SM_SYSTEM_MESSAGE.CompoundDifferentType(), ct);
            return false;
        }

        // The fusion (secondary) weapon must have an inferior or equal level to the main weapon.
        if (fusionTpl.Level > mainTpl.Level)
        {
            await conn.SendAsync(SM_SYSTEM_MESSAGE.CompoundMainRequireHigherLevel(), ct);
            return false;
        }

        long cost = (long)mainTpl.Level * mainTpl.Level * 2;
        var kinah = player.Inventory.FindByItemId(KinahItemId);
        if (kinah is null || kinah.Count < cost)
        {
            await conn.SendAsync(SM_SYSTEM_MESSAGE.CompoundNotEnoughMoney(), ct);
            return false;
        }

        kinah.Count -= cost;
        main.FusionedItemId = fusion.ItemId;

        fusion.Count--;
        if (fusion.Count <= 0)
        {
            player.Inventory.Remove(fusion.UniqueId);
            await _itemDao.DeleteAsync(fusion.UniqueId, ct);
            await conn.SendAsync(new SM_DELETE_ITEM(fusion.UniqueId), ct);
        }

        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);

        var changed = fusion.Count > 0 ? new[] { main, fusion, kinah } : new[] { main, kinah };
        await conn.SendAsync(new SM_INVENTORY_ADD_ITEM(changed), ct);
        await conn.SendAsync(SM_SYSTEM_MESSAGE.CompoundSuccess(), ct);

        _logger.LogInformation("Player {Name} fused item {Fusion} into {Main}", player.Name, fusion.ItemId, main.ItemId);
        return true;
    }

    /// <summary>
    /// Clears the fusion on <paramref name="weaponUid"/>. Mirrors Java <c>breakWeapons</c>: the consumed
    /// fusion weapon is not restored to the inventory — breaking only removes the fused stats/skin, it
    /// does not refund the item that was fused in.
    /// </summary>
    public async ValueTask<bool> BreakFusionAsync(Player player, long weaponUid, GsClientConnection conn, CancellationToken ct)
    {
        var weapon = player.Inventory.Get(weaponUid);
        if (weapon is null) return false;

        if (weapon.FusionedItemId == 0)
        {
            await conn.SendAsync(SM_SYSTEM_MESSAGE.DecompoundNotAvailable(), ct);
            return false;
        }

        weapon.FusionedItemId = 0;
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);

        await conn.SendAsync(new SM_INVENTORY_ADD_ITEM([weapon]), ct);
        await conn.SendAsync(SM_SYSTEM_MESSAGE.DecompoundSuccess(), ct);

        _logger.LogInformation("Player {Name} broke fusion on item {ItemId}", player.Name, weapon.ItemId);
        return true;
    }
}
