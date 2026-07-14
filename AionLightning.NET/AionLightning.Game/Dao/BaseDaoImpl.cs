using AionLightning.Game.Model.Siege;
using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

/// <summary>
/// Java dao.MySQL5BaseDAO, ported with an upsert instead of the Java select-then-insert-missing-rows pass
/// (same net effect: every row exists after load/save) — mirrors SiegeDaoImpl's own pattern.
/// note: Java persisted <c>BaseLocation.getRace()</c>, typed as <c>model.Race</c> (ELYOS/ASMODIANS/...many
/// NPC sub-races, never a literal "BALAUR" member), into a column declared
/// <c>ENUM('BALAUR','ASMODIANS','ELYOS')</c> — the neutral/default owner (<c>Race.NPC</c>) could never
/// round-trip through that column. This port sidesteps the mismatch entirely by persisting
/// <see cref="SiegeRace"/> (ELYOS/ASMODIANS/BALAUR) directly, whose members line up exactly with the
/// migration's enum literals (see Sql/game/V45__bases.sql).
/// </summary>
public sealed class BaseDaoImpl : IBaseDao
{
    private readonly MySqlDataSource _db;

    public BaseDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<List<BaseRow>> LoadAllAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Row>("SELECT id, map_id, race FROM bases");
        return rows.Select(r => new BaseRow(r.id, r.map_id, Enum.Parse<SiegeRace>(r.race))).ToList();
    }

    public async Task UpsertAsync(int id, int mapId, SiegeRace race, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO bases (id, map_id, race) VALUES (@id, @mapId, @race)
            ON DUPLICATE KEY UPDATE map_id = @mapId, race = @race, last_time = CURRENT_TIMESTAMP
            """, new { id, mapId, race = race.ToString() });
    }

    private sealed record Row(int id, int map_id, string race);
}
