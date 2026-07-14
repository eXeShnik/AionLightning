using AionLightning.Game.Model.House;

namespace AionLightning.Game.Dao;

/// <summary>Port of Java <c>dao.HouseBidsDAO</c>.</summary>
public interface IHouseBidsDao
{
    Task<List<PlayerHouseBid>> LoadBidsAsync(CancellationToken ct = default);
    Task<bool> AddBidAsync(int playerId, int houseId, long bidOffer, DateTime time, CancellationToken ct = default);
    Task ChangeBidAsync(int playerId, int houseId, long newBidOffer, DateTime time, CancellationToken ct = default);
    Task DeleteHouseBidsAsync(int houseId, CancellationToken ct = default);
}
