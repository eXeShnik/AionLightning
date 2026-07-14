using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Services;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client places a house-auction bid. Opcode 0x1BF.</summary>
public sealed class CM_PLACE_BID : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly HousingBidService _bidService;
    private readonly IOptions<HousingOptions> _housingOptions;

    private int _listIndex;
    private long _bidOffer;

    public CM_PLACE_BID(GsClientConnection conn, HousingBidService bidService, IOptions<HousingOptions> housingOptions)
    {
        _conn = conn;
        _bidService = bidService;
        _housingOptions = housingOptions;
    }

    public override void Read(ref PacketReader r)
    {
        _listIndex = r.ReadD();
        _bidOffer = r.ReadQ();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (!_housingOptions.Value.Enable)
            return;

        var player = _conn.ActivePlayer;
        if (player is null) return;

        await _bidService.PlaceBidAsync(player, _conn, _listIndex, _bidOffer, ct);
    }
}
