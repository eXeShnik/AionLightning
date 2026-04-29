using AionLightning.Login.Model;
using Dapper;
using MySqlConnector;

namespace AionLightning.Login.Dao;

public sealed class BannedIpDaoImpl : IBannedIpDao
{
    private readonly MySqlDataSource _db;

    public BannedIpDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<ISet<BannedIP>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<BannedIP>(
            "SELECT id AS Id, mask AS Mask, time_end AS TimeEnd FROM banned_ip");
        return rows.ToHashSet();
    }

    public async Task<bool> InsertAsync(BannedIP ban, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        try
        {
            var rows = await conn.ExecuteAsync(
                "INSERT INTO banned_ip (mask, time_end) VALUES (@Mask, @TimeEnd)", ban);
            return rows > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task CleanExpiredAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "DELETE FROM banned_ip WHERE time_end IS NOT NULL AND time_end < NOW()");
    }
}
