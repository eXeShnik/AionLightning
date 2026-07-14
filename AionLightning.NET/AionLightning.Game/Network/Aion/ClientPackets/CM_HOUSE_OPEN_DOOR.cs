using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.House;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client opens/enters or leaves a house's door. Java clientpackets.CM_HOUSE_OPEN_DOOR. Opcode 0x1A0.
/// note: Java's geodata-collision branch (GeoDataConfig.GEO_ENABLE) is skipped entirely — no geodata
/// engine exists in this port (see migration_plan.md) — so the simple heading-offset fallback is always
/// used. Java's GM-only "show house door id" debug message and the GM access-level door bypass are also
/// skipped — no access-level model exists on <see cref="Player"/> yet.
/// </summary>
public sealed class CM_HOUSE_OPEN_DOOR : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly HousingService _housingService;
    private readonly IDataManager _dataManager;
    private readonly TeleportService _teleport;
    private readonly IOptions<HousingOptions> _housingOptions;

    private int _address;
    private bool _leave;

    public CM_HOUSE_OPEN_DOOR(GsClientConnection conn, HousingService housingService, IDataManager dataManager,
        TeleportService teleport, IOptions<HousingOptions> housingOptions)
    {
        _conn = conn;
        _housingService = housingService;
        _dataManager = dataManager;
        _teleport = teleport;
        _housingOptions = housingOptions;
    }

    public override void Read(ref PacketReader r)
    {
        _address = r.ReadD();
        _leave = r.ReadC() != 0;
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (!_housingOptions.Value.Enable)
            return;

        var player = _conn.ActivePlayer;
        if (player is null) return;

        var house = _housingService.GetHouseByAddress(_address);
        if (house is null) return;

        var address = _dataManager.Housing.GetAddress(house.Address);
        if (address is null) return;

        // Java's getDoorState() normalizes permissions on every read; this port's DoorState getter is a
        // plain, non-side-effecting property instead (see House.cs), so callers must normalize explicitly.
        house.NormalizePermissions(_dataManager.Housing.GetBuilding(house.BuildingId)?.Type);

        int worldId = house.Position?.WorldId ?? address.MapId;

        if (_leave)
        {
            if (address is { ExitMapId: { } exitMap, ExitX: { } ex, ExitY: { } ey, ExitZ: { } ez })
            {
                await _teleport.TeleportToAsync(player, exitMap, 0, ex, ey, ez, 0, portAnimation: 0, ct);
            }
            else
            {
                double radian = player.Position.Heading * 3 * Math.PI / 180.0;
                float x = player.Position.X + (float)(Math.Cos(radian) * 6);
                float y = player.Position.Y + (float)(Math.Sin(radian) * 6);
                await _teleport.TeleportToAsync(player, worldId, 0, x, y, player.Position.Z, 0, portAnimation: 0, ct);
            }
            return;
        }

        if (house.PlayerObjectId != player.ObjectId)
        {
            // note: Java also allows entry when the owner's friend list contains the requester; no
            // FriendList model exists yet (see HouseController.KickVisitorsAsync doc comment).
            bool allowed = house.DoorState == HousePermissions.DOOR_OPENED_FRIENDS
                && player.Legion is { } legion && legion.Members.ContainsKey(house.PlayerObjectId);
            if (!allowed)
            {
                await _conn.SendAsync(SM_SYSTEM_MESSAGE.HousingCantEnterNoRight2(), ct);
                return;
            }
        }

        double enterRadian = player.Position.Heading * 3 * Math.PI / 180.0;
        float enterX = player.Position.X + (float)(Math.Cos(enterRadian) * 6);
        float enterY = player.Position.Y + (float)(Math.Sin(enterRadian) * 6);
        await _teleport.TeleportToAsync(player, worldId, 0, enterX, enterY, address.Z, 0, portAnimation: 0, ct);
    }
}
