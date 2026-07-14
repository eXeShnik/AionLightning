using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.House;
using AionLightning.Game.Model.Templates.Housing;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client teleports to a house via a relationship crystal (own house / a specific player's / a random
/// friend-or-legion-mate's). Java clientpackets.CM_HOUSE_TELEPORT. Opcode 0x1BC.
/// </summary>
public sealed class CM_HOUSE_TELEPORT : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly HousingService _housingService;
    private readonly IDataManager _dataManager;
    private readonly InstanceService _instanceService;
    private readonly TeleportService _teleport;
    private readonly IOptions<HousingOptions> _housingOptions;

    private int _actionId;
    private int _playerId2;

    public CM_HOUSE_TELEPORT(GsClientConnection conn, HousingService housingService, IDataManager dataManager,
        InstanceService instanceService, TeleportService teleport, IOptions<HousingOptions> housingOptions)
    {
        _conn = conn;
        _housingService = housingService;
        _dataManager = dataManager;
        _instanceService = instanceService;
        _teleport = teleport;
        _housingOptions = housingOptions;
    }

    public override void Read(ref PacketReader r)
    {
        _actionId = r.ReadC();
        r.ReadD(); // playerId1 — Java resolves the acting player from this field; this port always acts
                   // on the connection's own player instead of trusting a client-supplied object id.
        _playerId2 = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (!_housingOptions.Value.Enable)
            return;

        var player = _conn.ActivePlayer;
        if (player is null) return;

        int targetPlayerId = _playerId2;

        if (_actionId == 1)
        {
            targetPlayerId = player.ObjectId;
        }
        else if (_actionId == 3)
        {
            // note: Java also scans the acting player's friend list for eligible targets; no FriendList
            // model is ported yet (see HouseController's kickVisitors doc comment), so only legion mates
            // are considered here.
            var candidates = new List<int>();
            if (player.Legion is { } legion)
            {
                foreach (var memberId in legion.Members.Keys)
                {
                    int memberAddress = _housingService.GetPlayerAddress(memberId);
                    if (memberAddress == 0) continue;

                    var memberHouse = _housingService.GetPlayerStudio(memberId) ?? _housingService.GetHouseByAddress(memberAddress);
                    if (memberHouse != null)
                    {
                        // Java's getDoorState() normalizes permissions on every read; this port's DoorState
                        // getter is a plain, non-side-effecting property instead (see House.cs).
                        memberHouse.NormalizePermissions(_dataManager.Housing.GetBuilding(memberHouse.BuildingId)?.Type);
                        if (memberHouse.DoorState == HousePermissions.DOOR_CLOSED
                            || _housingService.GetLevelRestrict(memberHouse) > player.Level)
                            continue; // closed doors | level restrict
                    }

                    candidates.Add(memberId);
                }
            }

            if (candidates.Count == 0)
            {
                await _conn.SendAsync(SM_SYSTEM_MESSAGE.NoRelationshipRecently(), ct);
                return;
            }
            targetPlayerId = candidates[Random.Shared.Next(candidates.Count)];
        }

        if (targetPlayerId == 0) return;

        var house = _housingService.GetPlayerStudio(targetPlayerId);
        HouseAddress? address;
        int instanceId;

        if (house != null)
        {
            address = _dataManager.Housing.GetAddress(house.Address);
            if (address is null) return;

            var instance = _instanceService.GetPersonalInstance(address.MapId, targetPlayerId)
                           ?? _instanceService.GetNextAvailableInstance(address.MapId, targetPlayerId);
            instanceId = instance.InstanceId;
            _instanceService.RegisterPlayerWithInstance(instance, player);
        }
        else
        {
            int addressId = _housingService.GetPlayerAddress(targetPlayerId);
            house = _housingService.GetHouseByAddress(addressId);
            if (house is null || _housingService.GetLevelRestrict(house) > player.Level) return;

            address = _dataManager.Housing.GetAddress(house.Address);
            if (address is null) return;
            instanceId = house.Position?.InstanceId ?? 0;
        }

        if (player.Target is { } target)
            await _conn.SendAsync(new SM_DIALOG_WINDOW(target.ObjectId, 0), ct);

        await _teleport.TeleportToAsync(player, address.MapId, instanceId, address.X, address.Y, address.Z, 0, portAnimation: 0, ct);
        await _conn.SendAsync(new SM_HOUSE_TELEPORT(house.Address, targetPlayerId), ct);
    }
}
