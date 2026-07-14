using AionLightning.Game.Model.Siege;
using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

/// <summary>Java dao.MySQL5SiegeDAO, ported with an upsert instead of the Java
/// select-then-insert-missing-rows pass (same net effect: every row exists after load/save).</summary>
public sealed class SiegeDaoImpl : ISiegeDao
{
    private readonly MySqlDataSource _db;

    public SiegeDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<List<SiegeLocationRow>> LoadAllAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Row>("SELECT id, race, legion_id FROM siege_locations");
        return rows.Select(r => new SiegeLocationRow(r.id, Enum.Parse<SiegeRace>(r.race), r.legion_id)).ToList();
    }

    public async Task UpsertAsync(int locationId, SiegeRace race, int legionId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO siege_locations (id, race, legion_id) VALUES (@locationId, @race, @legionId)
            ON DUPLICATE KEY UPDATE race = @race, legion_id = @legionId
            """, new { locationId, race = race.ToString(), legionId });
    }

    private sealed record Row(int id, string race, int legion_id);
}
