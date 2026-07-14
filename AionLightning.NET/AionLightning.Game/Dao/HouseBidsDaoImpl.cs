using AionLightning.Game.Model.House;
using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

/// <summary>Port of Java <c>mysql5.MySQL5HouseBidsDAO</c>.</summary>
public sealed class HouseBidsDaoImpl : IHouseBidsDao
{
    private readonly MySqlDataSource _db;

    public HouseBidsDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<List<PlayerHouseBid>> LoadBidsAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<BidRow>("SELECT player_id, house_id, bid, bid_time FROM house_bids");
        return rows.Select(r => new PlayerHouseBid(r.player_id, r.house_id, r.bid, r.bid_time)).ToList();
    }

    public async Task<bool> AddBidAsync(int playerId, int houseId, long bidOffer, DateTime time, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "INSERT INTO house_bids (player_id, house_id, bid, bid_time) VALUES (@playerId, @houseId, @bidOffer, @time)",
            new { playerId, houseId, bidOffer, time });
        return true;
    }

    public async Task ChangeBidAsync(int playerId, int houseId, long newBidOffer, DateTime time, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE house_bids SET bid = @newBidOffer, bid_time = @time WHERE player_id = @playerId AND house_id = @houseId",
            new { newBidOffer, time, playerId, houseId });
    }

    public async Task DeleteHouseBidsAsync(int houseId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync("DELETE FROM house_bids WHERE house_id = @houseId", new { houseId });
    }

    private sealed record BidRow(int player_id, int house_id, long bid, DateTime bid_time);
}
