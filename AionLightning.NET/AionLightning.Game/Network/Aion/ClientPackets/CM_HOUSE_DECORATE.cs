using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Controllers;
using AionLightning.Game.Dao;
using AionLightning.Game.Model.GameObjects;
using HouseModel = AionLightning.Game.Model.House.House;
using AionLightning.Game.Model.Templates.Housing;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Java clientpackets.CM_HOUSE_DECORATE — swaps which decoration part (a default building part, or a
/// player-owned custom part) is currently displayed for one (PartType, floor) slot. Only operates on the
/// player's own active house — Java resolves the same via <c>player.getHouseRegistry().getOwner()</c>,
/// which is only ever set to the player's own house registry (see HouseRegistry's class doc).
/// Opcode 0x2E9.
/// </summary>
public sealed class CM_HOUSE_DECORATE : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly HouseController _houseController;
    private readonly IPlayerRegisteredItemsDao _registeredItemsDao;
    private readonly IOptions<HousingOptions> _housingOptions;

    private int _objectId;
    private int _templateId;
    private int _lineNr; // Line number (starts from 1 in 3.0 and from 2 in 3.5) of part in house render/update packet

    public CM_HOUSE_DECORATE(GsClientConnection conn, HouseController houseController,
        IPlayerRegisteredItemsDao registeredItemsDao, IOptions<HousingOptions> housingOptions)
    {
        _conn = conn;
        _houseController = houseController;
        _registeredItemsDao = registeredItemsDao;
        _housingOptions = housingOptions;
    }

    public override void Read(ref PacketReader r)
    {
        _objectId = r.ReadD();
        _templateId = r.ReadD();
        _lineNr = r.ReadH();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (!_housingOptions.Value.Enable) return;

        var player = _conn.ActivePlayer;
        if (player is null) return;
        var house = player.ActiveHouse;
        if (house is null) return;

        var partType = PartTypeLineNumbers.GetForLineNr(_lineNr);
        if (partType is null) return;
        int floor = _lineNr - partType.Value.GetStartLineNr();

        if (_objectId == 0)
        {
            // Revert to the building's default part for this slot.
            var defaultDecor = house.Registry.GetDefaultPartByType(partType.Value, floor);
            if (defaultDecor is null || defaultDecor.IsUsed) return;
            await ApplyAsync(house, defaultDecor, floor, ct);
        }
        else
        {
            // Swap in a custom part from the player's un-placed decoration pool.
            var customDecor = house.Registry.GetCustomPartByObjId(_objectId);
            if (customDecor is null) return;
            await ApplyAsync(house, customDecor, floor, ct);

            // yes, in retail it's sent twice!
            await _conn.SendAsync(new SM_HOUSE_EDIT(4, 2, _objectId), ct);
        }

        await _conn.SendAsync(new SM_HOUSE_EDIT(4, 2, _objectId), ct);
        await _houseController.BroadcastAppearanceAsync(house, ct);
    }

    private async Task ApplyAsync(HouseModel house, HouseDecoration decor, int floor, CancellationToken ct)
    {
        var displaced = house.Registry.SetPartInUse(decor, floor);
        foreach (var removed in displaced)
        {
            await _registeredItemsDao.DeleteAsync(removed.ObjectId, ct);
            house.Registry.DiscardPart(removed.ObjectId);
        }

        if (decor.ObjectId != 0)
            await _registeredItemsDao.UpsertAsync(house.PlayerObjectId, RegisteredItemRowMapper.ForDecoration(decor), ct);
    }
}
