using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client pays house rent <c>weekCount</c> weeks ahead. Java clientpackets.CM_HOUSE_PAY_RENT. Opcode 0x1BD.
/// </summary>
public sealed class CM_HOUSE_PAY_RENT : AionClientPacket
{
    private const int KinahItemId = 182400001;

    private readonly GsClientConnection _conn;
    private readonly IItemDao _itemDao;
    private readonly IHouseDao _houseDao;
    private readonly IDataManager _dataManager;
    private readonly IOptions<HousingOptions> _housingOptions;

    private int _weekCount;

    public CM_HOUSE_PAY_RENT(GsClientConnection conn, IItemDao itemDao, IHouseDao houseDao, IDataManager dataManager,
        IOptions<HousingOptions> housingOptions)
    {
        _conn = conn;
        _itemDao = itemDao;
        _houseDao = houseDao;
        _dataManager = dataManager;
        _housingOptions = housingOptions;
    }

    public override void Read(ref PacketReader r) => _weekCount = r.ReadC();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        // note: Java's disabled-branch reply (STR_MSG_F2P_CASH_HOUSE_FEE_FREE — "no fee owed while the
        // pay system is off") is skipped here to match the silent-no-op gating convention every other
        // housing packet in this port uses while HousingOptions.Enable is false (see CM_PLACE_BID,
        // CM_REGISTER_HOUSE) instead of sending an SM_HOUSE_* the P1 client-capture verification hasn't covered.
        if (!_housingOptions.Value.Enable)
            return;

        var player = _conn.ActivePlayer;
        if (player is null) return;

        var house = player.ActiveHouse;
        if (house is null) return; // should not happen

        var land = _dataManager.Housing.GetLandByAddress(house.Address);
        long toPay = (land?.MaintenanceFee ?? 0) * _weekCount;
        if (toPay <= 0) return;

        var kinahItem = player.Inventory.FindByItemId(KinahItemId);
        if ((kinahItem?.Count ?? 0) < toPay)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.NotEnoughMoney(), ct);
            return;
        }

        var period = MaintenanceTask.GetPeriod(MaintenanceTask.MaintenanceCron);
        var runTime = MaintenanceTask.GetNextRunTime(MaintenanceTask.MaintenanceCron);
        var baseline = house.NextPay ?? runTime.UtcDateTime;
        var payTime = baseline + period * _weekCount;

        if (runTime.UtcDateTime.AddDays(28) < payTime) // client cap: at most 4 weeks prepaid
            return;

        kinahItem!.Count -= toPay;
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinahItem]), ct);

        house.NextPay = payTime;
        house.FeePaid = true;
        await _houseDao.StoreAsync(house, ct);

        await _conn.SendAsync(new SM_HOUSE_PAY_RENT(_weekCount), ct);
    }
}
