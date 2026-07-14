using AionLightning.Game.Model.Templates.Housing;

namespace AionLightning.Game.Model.House;

/// <summary>
/// Port of Java <c>model.house.HouseBidEntry</c> — one active auction listing for a house (current top
/// bid + the metadata needed to render an <c>SM_HOUSE_BIDS</c> row). <see cref="EntryIndex"/> is the
/// client-visible list slot, assigned once by <c>HousingBidService</c> and stable for the life of the
/// listing (Java's <c>bidsByIndex</c> map key). Java resolved <c>landId</c>/<c>mapId</c>/<c>houseType</c>
/// lazily from the owning <see cref="House"/>'s <c>Land</c>/<c>Building</c> object references; this port's
/// <see cref="House"/> model only stores the raw ids, so the caller (HousingBidService) resolves those
/// via <c>IDataManager.Housing</c> once at construction time instead.
/// </summary>
public sealed class HouseBidEntry
{
    /// <summary>Java's <c>unk2</c> — always 100000, mirrored verbatim from the client protocol.</summary>
    public const long Unk2Value = 100000;

    public int EntryIndex { get; set; }
    public int LandId { get; }
    public int Address { get; }
    public int BuildingId { get; set; }
    public HouseType HouseType { get; }
    public long BidPrice { get; set; }
    public long Unk2 => Unk2Value;
    public int BidCount { get; private set; }
    public int MapId { get; }
    public int LastBiddingPlayer { get; set; }
    public long LastBidTime { get; set; }

    public HouseBidEntry(House house, int index, long initialBid, int landId, HouseType houseType, int mapId)
    {
        EntryIndex = index;
        LandId = landId;
        Address = house.Address;
        MapId = mapId;
        BuildingId = house.BuildingId;
        HouseType = houseType;
        BidPrice = initialBid;
        LastBiddingPlayer = 0;
        LastBidTime = 0;
    }

    private HouseBidEntry(HouseBidEntry other)
    {
        EntryIndex = other.EntryIndex;
        LandId = other.LandId;
        Address = other.Address;
        MapId = other.MapId;
        BuildingId = other.BuildingId;
        HouseType = other.HouseType;
        BidPrice = other.BidPrice;
        BidCount = other.BidCount;
        LastBiddingPlayer = other.LastBiddingPlayer;
        LastBidTime = other.LastBidTime;
    }

    public void IncrementBidCount() => BidCount++;

    /// <summary>Java <c>getRefundKinah()</c> — the partial refund (Java <c>HousingConfig.BID_REFUND_PERCENT</c>,
    /// default 0.3) paid back on top of the bid principal when a bid is cancelled/superseded.</summary>
    public long GetRefundKinah(float refundPercent) => (long)(BidPrice * refundPercent);

    /// <summary>Java <c>Clone()</c> — snapshots the current house-level bid state into a per-player entry
    /// stored in <c>playerBids</c> (see HousingBidService.PlaceBid/LoadAsync).</summary>
    public HouseBidEntry Clone() => new(this);
}
