using AionLightning.Commons.Network;
using AionLightning.Game.Model.House;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests the house-auction bid list. Opcode 0x1B8.</summary>
public sealed class CM_GET_HOUSE_BIDS : AionClientPacket
{
    private const int PageSize = 181; // Java's ListSplitter chunk size

    private readonly GsClientConnection _conn;
    private readonly HousingBidService _bidService;

    public CM_GET_HOUSE_BIDS(GsClientConnection conn, HousingBidService bidService)
    {
        _conn = conn;
        _bidService = bidService;
    }

    public override void Read(ref PacketReader r) { }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var playerBid = _bidService.GetLastPlayerBid(player.ObjectId);

        var sellHouse = player.Houses.FirstOrDefault(h => h.Status == HouseStatus.SellWait);
        var sellEntry = sellHouse is not null ? _bidService.GetHouseBid(sellHouse.Id) : null;

        // note: Java's per-race bid-list filtering is not ported (see HousingBidService class doc) —
        // every active listing is sent regardless of the requesting player's race.
        var houseBids = _bidService.GetHouseBidEntries();
        int seconds = _bidService.GetSecondsTillAuction();

        var pages = houseBids.Chunk(PageSize).ToList();
        if (pages.Count == 0)
            pages.Add([]);

        for (int i = 0; i < pages.Count; i++)
        {
            bool isFirst = i == 0;
            bool isLast = i == pages.Count - 1;
            var playerData = isLast ? playerBid : null;

            await _conn.SendAsync(new SM_HOUSE_BIDS(isFirst, isLast, playerData, pages[i], sellEntry, seconds,
                entry => _bidService.CanBidHouse(player, entry.LandId)), ct);
        }
    }
}
