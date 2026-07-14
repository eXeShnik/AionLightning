using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.House;
using AionLightning.Game.Model.Templates.Housing;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client registers their own house for auction. Opcode 0x1B2.</summary>
public sealed class CM_REGISTER_HOUSE : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IItemDao _itemDao;
    private readonly IDataManager _dataManager;
    private readonly HousingBidService _bidService;
    private readonly IOptions<HousingOptions> _housingOptions;
    private readonly IOptions<HousingAuctionOptions> _auctionOptions;

    private const int KinahItemId = 182400001;

    private long _bidKinah;

    public CM_REGISTER_HOUSE(
        GsClientConnection conn,
        IItemDao itemDao,
        IDataManager dataManager,
        HousingBidService bidService,
        IOptions<HousingOptions> housingOptions,
        IOptions<HousingAuctionOptions> auctionOptions)
    {
        _conn = conn;
        _itemDao = itemDao;
        _dataManager = dataManager;
        _bidService = bidService;
        _housingOptions = housingOptions;
        _auctionOptions = auctionOptions;
    }

    public override void Read(ref PacketReader r)
    {
        _bidKinah = r.ReadQ();
        r.ReadQ(); // unk1, always 100000
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (!_housingOptions.Value.Enable)
            return;

        var player = _conn.ActivePlayer;
        if (player is null) return;

        var house = player.ActiveHouse;
        if (house is null)
            return; // should not happen

        var houseType = _dataManager.Housing.GetBuilding(house.BuildingId)?.Size;
        if (houseType == HouseType.STUDIO)
            return; // should not happen

        if (house.Status == HouseStatus.SellWait)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.HousingAuctionFailAlreadyRegistered(), ct);
            return;
        }

        if (!_bidService.IsRegisteringAllowed())
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.HousingCantAuctionTimeout(), ct);
            return;
        }

        if (!house.FeePaid)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.HousingCantAuctionOverdue(), ct);
            return;
        }

        long fee = (long)(_bidKinah * _auctionOptions.Value.RegisterFeePercent);

        var kinahItem = player.Inventory.FindByItemId(KinahItemId);
        if ((kinahItem?.Count ?? 0) < fee)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.NoEnoughKinah(), ct);
            return;
        }

        kinahItem!.Count -= fee;
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinahItem]), ct);

        await _bidService.AddToAuctionAsync(house, _bidKinah, ct);

        await _conn.SendAsync(SM_SYSTEM_MESSAGE.HousingAuctionMyHouse(house.Address), ct);
        // note: Java also called ((HouseController) house.getController()).updateAppearance() to refresh
        // the house's "for sale" sign in the world — no house spawn/controller layer exists in this port.
        await _conn.SendAsync(new SM_HOUSE_OWNER_INFO(player, house), ct);
    }
}
