using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Controllers;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Services;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client teleports back out of a house. Java clientpackets.CM_HOUSE_TELEPORT_BACK uses the player's
/// generic battle-return coords/map (a non-house-specific "return from instance" state that isn't ported
/// on <c>Player</c> — see migration_plan.md). Instead, this port treats "teleport back" as leaving
/// whichever house currently occupies the player's scope, reusing <see cref="HouseController.MoveOutsideAsync"/>.
/// Opcode 0x13D.
/// </summary>
public sealed class CM_HOUSE_TELEPORT_BACK : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly HousingService _housingService;
    private readonly IDataManager _dataManager;
    private readonly HouseController _houseController;
    private readonly IOptions<HousingOptions> _housingOptions;

    public CM_HOUSE_TELEPORT_BACK(GsClientConnection conn, HousingService housingService, IDataManager dataManager,
        HouseController houseController, IOptions<HousingOptions> housingOptions)
    {
        _conn = conn;
        _housingService = housingService;
        _dataManager = dataManager;
        _houseController = houseController;
        _housingOptions = housingOptions;
    }

    public override void Read(ref PacketReader r) { }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (!_housingOptions.Value.Enable)
            return;

        var player = _conn.ActivePlayer;
        if (player is null) return;

        var house = _housingService.GetHousesInScope(player.Position).FirstOrDefault();
        if (house is null) return;

        var address = _dataManager.Housing.GetAddress(house.Address);
        await _houseController.MoveOutsideAsync(player, house, address, ct);
    }
}
