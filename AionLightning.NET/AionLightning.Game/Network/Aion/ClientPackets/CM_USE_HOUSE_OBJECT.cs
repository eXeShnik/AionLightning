using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Java clientpackets.CM_USE_HOUSE_OBJECT — a player interacts with a placed house object (their own, or
/// one they're visiting). Java dispatches to the per-kind HouseObject subclass's onUse() (mailbox dialog,
/// warehouse open, cook-pot reward flow, house-NPC talk, etc.) — none of that per-kind business logic is
/// ported in this phase (see <see cref="Model.GameObjects.HouseObject"/>'s class doc); this handler covers
/// only the mechanics common to every kind: the reuse-cooldown gate and owner/visitor use-count tracking,
/// then acknowledges with the object's current state.
/// Opcode 0x1A2.
/// </summary>
public sealed class CM_USE_HOUSE_OBJECT : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly HousingService _housingService;
    private readonly IHouseObjectCooldownsDao _cooldownsDao;
    private readonly IPlayerRegisteredItemsDao _registeredItemsDao;
    private readonly IOptions<HousingOptions> _housingOptions;

    private int _itemObjectId;

    public CM_USE_HOUSE_OBJECT(GsClientConnection conn, HousingService housingService,
        IHouseObjectCooldownsDao cooldownsDao, IPlayerRegisteredItemsDao registeredItemsDao,
        IOptions<HousingOptions> housingOptions)
    {
        _conn = conn;
        _housingService = housingService;
        _cooldownsDao = cooldownsDao;
        _registeredItemsDao = registeredItemsDao;
        _housingOptions = housingOptions;
    }

    public override void Read(ref PacketReader r) => _itemObjectId = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (!_housingOptions.Value.Enable) return;

        var player = _conn.ActivePlayer;
        if (player is null) return;

        var obj = _housingService.FindHouseObject(_itemObjectId);
        if (obj is null) return;

        // note: Java also warns via SM_SYSTEM_MESSAGE (occupied-by-other / owner-only / cooldown) on each
        // rejection branch — skipped, matching this port's other unverified-4.6-string-id housing packets.
        if (!player.CanUseHouseObject(obj.ObjectId)) return;

        bool isOwner = player.ObjectId == obj.OwnerHouse.PlayerObjectId;
        if (isOwner) obj.OwnerUsedCount++;
        else obj.VisitorUsedCount++;
        obj.MarkDirty();

        int cooldownSeconds = obj.Template?.Cd ?? 0;
        if (cooldownSeconds > 0)
        {
            player.SetHouseObjectCooldown(obj.ObjectId, cooldownSeconds);
            await _cooldownsDao.UpsertAsync(player.ObjectId, obj.ObjectId, player.HouseObjectCooldowns[obj.ObjectId], ct);
        }

        await _registeredItemsDao.UpsertAsync(obj.OwnerHouse.PlayerObjectId, RegisteredItemRowMapper.ForObject(obj), ct);
        obj.PersistentState = Model.GameObjects.PersistentState.Updated;

        await _conn.SendAsync(new SM_HOUSE_OBJECT(obj, player.GetHouseObjectReuseDelay(obj.ObjectId)), ct);
    }
}
