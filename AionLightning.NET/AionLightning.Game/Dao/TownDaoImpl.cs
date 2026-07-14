using AionLightning.Game.Model;
using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

/// <summary>Java dao.MySQL5TownDAO, ported with an upsert instead of Java's select-then-insert-missing
/// pass (same net effect — mirrors BaseDaoImpl's own convention).</summary>
public sealed class TownDaoImpl : ITownDao
{
    private readonly MySqlDataSource _db;

    public TownDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<List<TownRow>> LoadAsync(Race race, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Row>(
            "SELECT id, level, points, level_up_date FROM towns WHERE race = @race",
            new { race = race.ToString() });
        return rows.Select(r => new TownRow(r.id, r.level, r.points, r.level_up_date)).ToList();
    }

    public async Task UpsertAsync(int id, int level, int points, Race race, DateTime levelUpDate, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO towns (id, level, points, race, level_up_date) VALUES (@id, @level, @points, @race, @levelUpDate)
            ON DUPLICATE KEY UPDATE level = @level, points = @points, level_up_date = @levelUpDate
            """,
            new { id, level, points, race = race.ToString(), levelUpDate });
    }

    private sealed record Row(int id, int level, int points, DateTime level_up_date);
}
