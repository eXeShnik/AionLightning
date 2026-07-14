namespace AionLightning.Game.Model.House;

/// <summary>
/// Port of Java <c>model.house.PlayerHouseBid</c> — one persisted (player, house, bid, time) row from the
/// <c>house_bids</c> table, used only at startup to reconstruct in-memory <see cref="HouseBidEntry"/>
/// state (see <c>HousingBidService.LoadAsync</c>). Comparable by <see cref="Time"/> ascending, mirroring
/// Java's <c>compareTo</c>.
/// </summary>
public sealed class PlayerHouseBid(int playerId, int houseId, long bidOffer, DateTime time)
    : IComparable<PlayerHouseBid>
{
    public int PlayerId { get; } = playerId;
    public int HouseId { get; } = houseId;
    public long BidOffer { get; } = bidOffer;
    public DateTime Time { get; } = time;

    public int CompareTo(PlayerHouseBid? other) => Time.CompareTo(other?.Time);
}
