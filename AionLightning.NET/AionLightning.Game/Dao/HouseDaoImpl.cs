using AionLightning.Game.Model.House;
using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class HouseDaoImpl : IHouseDao
{
    private readonly MySqlDataSource _db;

    public HouseDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<List<House>> LoadAllAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<HouseRow>(
            """
            SELECT id, player_id, building_id, address, acquire_time, settings, status,
                   fee_paid, next_pay, sell_started, sign_notice
            FROM houses
            """);
        return rows.Select(ToHouse).ToList();
    }

    public async Task<House?> LoadByOwnerAsync(int playerObjectId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<HouseRow>(
            """
            SELECT id, player_id, building_id, address, acquire_time, settings, status,
                   fee_paid, next_pay, sell_started, sign_notice
            FROM houses WHERE player_id = @playerObjectId
            """, new { playerObjectId });
        return row is null ? null : ToHouse(row);
    }

    public async Task StoreAsync(House house, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO houses (id, player_id, building_id, address, acquire_time, settings, status,
                fee_paid, next_pay, sell_started, sign_notice)
            VALUES (@Id, @PlayerObjectId, @BuildingId, @Address, @AcquireTime, @Permissions, @Status,
                @FeePaid, @NextPay, @SellStarted, @SignNotice)
            ON DUPLICATE KEY UPDATE
                player_id = @PlayerObjectId, building_id = @BuildingId, address = @Address,
                acquire_time = @AcquireTime, settings = @Permissions, status = @Status,
                fee_paid = @FeePaid, next_pay = @NextPay, sell_started = @SellStarted, sign_notice = @SignNotice
            """, new
            {
                house.Id,
                house.PlayerObjectId,
                house.BuildingId,
                house.Address,
                AcquireTime = ToUnixMs(house.AcquiredTime),
                house.Permissions,
                Status = house.Status.ToString(),
                house.FeePaid,
                NextPay = ToUnixMsOrNull(house.NextPay),
                SellStarted = ToUnixMsOrNull(house.SellStarted),
                house.SignNotice,
            });
    }

    public async Task DeleteByOwnerAsync(int playerObjectId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync("DELETE FROM houses WHERE player_id = @playerObjectId", new { playerObjectId });
    }

    private static long ToUnixMs(DateTime dt) => new DateTimeOffset(dt, TimeSpan.Zero).ToUnixTimeMilliseconds();

    private static long? ToUnixMsOrNull(DateTime? dt) => dt.HasValue ? ToUnixMs(dt.Value) : null;

    private static DateTime FromUnixMs(long ms) => DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;

    private static DateTime? FromUnixMsOrNull(long? ms) => ms.HasValue ? FromUnixMs(ms.Value) : null;

    private static House ToHouse(HouseRow r) => new()
    {
        Id = r.id,
        PlayerObjectId = r.player_id,
        BuildingId = r.building_id,
        Address = r.address,
        AcquiredTime = FromUnixMs(r.acquire_time),
        Permissions = r.settings,
        Status = Enum.Parse<HouseStatus>(r.status),
        FeePaid = r.fee_paid,
        NextPay = FromUnixMsOrNull(r.next_pay),
        SellStarted = FromUnixMsOrNull(r.sell_started),
        SignNotice = r.sign_notice ?? new byte[House.NoticeLength],
    };

    private sealed record HouseRow(
        int id, int player_id, int building_id, int address, long acquire_time, int settings, string status,
        bool fee_paid, long? next_pay, long? sell_started, byte[]? sign_notice);
}
