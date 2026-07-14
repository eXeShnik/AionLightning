using System.Text;
using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Controllers;
using AionLightning.Game.Dao;
using AionLightning.Game.Model.House;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client changes house door/notice/sign-message settings. Java clientpackets.CM_HOUSE_SETTINGS.
/// Opcode 0x2EB.
/// </summary>
public sealed class CM_HOUSE_SETTINGS : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly HousingService _housingService;
    private readonly IHouseDao _houseDao;
    private readonly HouseController _houseController;
    private readonly IOptions<HousingOptions> _housingOptions;

    private int _doorState;
    private int _displayOwner;
    private string _signNotice = string.Empty;

    public CM_HOUSE_SETTINGS(GsClientConnection conn, HousingService housingService, IHouseDao houseDao,
        HouseController houseController, IOptions<HousingOptions> housingOptions)
    {
        _conn = conn;
        _housingService = housingService;
        _houseDao = houseDao;
        _houseController = houseController;
        _housingOptions = housingOptions;
    }

    public override void Read(ref PacketReader r)
    {
        _doorState = r.ReadC();
        _displayOwner = r.ReadC();
        _signNotice = r.ReadS();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (!_housingOptions.Value.Enable)
            return;

        var player = _conn.ActivePlayer;
        if (player is null) return;

        var house = _housingService.GetPlayerStudio(player.ObjectId);
        if (house is null)
        {
            int address = _housingService.GetPlayerAddress(player.ObjectId);
            house = _housingService.GetHouseByAddress(address);
        }
        if (house is null) return;

        int doorPermission = HousePermissions.GetPacketDoorState(_doorState);
        house.Permissions = HousePermissions.SetDoorState(house.Permissions, doorPermission);
        house.Permissions = HousePermissions.SetNoticeState(house.Permissions, HousePermissions.GetNoticeState(_displayOwner));

        var noticeBytes = Encoding.Unicode.GetBytes(_signNotice);
        house.SignNotice = noticeBytes.Length > House.NoticeLength ? noticeBytes[..House.NoticeLength] : noticeBytes;

        await _houseDao.StoreAsync(house, ct);

        await _conn.SendAsync(new SM_HOUSE_ACQUIRE(player.ObjectId, house.Address, true), ct);
        // note: Java's updateAppearance() always sends SM_HOUSE_UPDATE regardless of world type; this port
        // reuses BroadcastAppearanceAsync (Java's broadcastAppearance()) instead, which already picks the
        // packet variant matching each observer's spawn style (see HouseController.SeeAsync).
        await _houseController.BroadcastAppearanceAsync(house, ct);

        // note: Java's per-visitor kick here evicts every non-owner visitor regardless of friend status
        // for FRIENDS/CLOSED (the friend exemption isn't modeled — see HouseController.KickVisitorsAsync doc).
        if (doorPermission == HousePermissions.DOOR_OPENED_ALL)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.HousingOrderOpenDoor(), ct);
        }
        else if (doorPermission == HousePermissions.DOOR_OPENED_FRIENDS)
        {
            await _houseController.KickVisitorsAsync(house, onSettingsChange: true, ct);
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.HousingOrderCloseDoorWithoutFriends(), ct);
        }
        else if (doorPermission == HousePermissions.DOOR_CLOSED)
        {
            await _houseController.KickVisitorsAsync(house, onSettingsChange: true, ct);
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.HousingOrderCloseDoorAll(), ct);
        }
    }
}
