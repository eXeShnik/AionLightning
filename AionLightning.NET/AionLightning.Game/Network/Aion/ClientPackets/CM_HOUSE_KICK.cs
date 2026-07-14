using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// House owner kicks visitors out. Java clientpackets.CM_HOUSE_KICK. Opcode 0x2EA.
/// </summary>
public sealed class CM_HOUSE_KICK : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly HouseController _houseController;
    private readonly IOptions<HousingOptions> _housingOptions;
    private readonly ILogger<CM_HOUSE_KICK> _log;

    private int _option;

    public CM_HOUSE_KICK(GsClientConnection conn, HouseController houseController,
        IOptions<HousingOptions> housingOptions, ILogger<CM_HOUSE_KICK> log)
    {
        _conn = conn;
        _houseController = houseController;
        _housingOptions = housingOptions;
        _log = log;
    }

    public override void Read(ref PacketReader r)
    {
        _option = r.ReadC();
        r.ReadH(); // unk
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (!_housingOptions.Value.Enable)
            return;

        var player = _conn.ActivePlayer;
        if (player is null) return;

        var house = player.ActiveHouse;
        if (house is null)
        {
            _log.LogInformation("Player {Player} tried to kick visitors from a house they don't own", player.Name);
            return;
        }

        // note: Java's option 1 (kickFriends=false) vs option 2 (kickFriends=true) distinction collapses
        // to the same call here — the friend exemption isn't modeled (see HouseController.KickVisitorsAsync doc).
        if (_option == 1 || _option == 2)
            await _houseController.KickVisitorsAsync(house, onSettingsChange: false, ct);
    }
}
