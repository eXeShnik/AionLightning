using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class HouseObjectCooldownsDaoImpl : IHouseObjectCooldownsDao
{
    private readonly MySqlDataSource _db;

    public HouseObjectCooldownsDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<Dictionary<int, long>> LoadAsync(int playerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<(int object_id, long reuse_time)>(
            "SELECT object_id, reuse_time FROM house_object_cooldowns WHERE player_id = @playerId",
            new { playerId });
        return rows.ToDictionary(r => r.object_id, r => r.reuse_time);
    }

    public async Task UpsertAsync(int playerId, int objectId, long reuseTimeMs, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO house_object_cooldowns (player_id, object_id, reuse_time)
            VALUES (@playerId, @objectId, @reuseTimeMs)
            ON DUPLICATE KEY UPDATE reuse_time = @reuseTimeMs
            """, new { playerId, objectId, reuseTimeMs });
    }

    public async Task DeleteAsync(int playerId, int objectId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "DELETE FROM house_object_cooldowns WHERE player_id = @playerId AND object_id = @objectId",
            new { playerId, objectId });
    }
}
