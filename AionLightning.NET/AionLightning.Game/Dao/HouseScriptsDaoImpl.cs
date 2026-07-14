using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class HouseScriptsDaoImpl : IHouseScriptsDao
{
    private readonly MySqlDataSource _db;

    public HouseScriptsDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<List<(int Position, string? Script)>> LoadAsync(int houseId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<(int index, string? script)>(
            "SELECT `index`, script FROM house_scripts WHERE house_id = @houseId",
            new { houseId });
        return rows.Select(r => (r.index, r.script)).ToList();
    }

    public async Task AddScriptAsync(int houseId, int position, string? script, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "INSERT INTO house_scripts (house_id, `index`, script) VALUES (@houseId, @position, @script)",
            new { houseId, position, script });
    }

    public async Task UpdateScriptAsync(int houseId, int position, string? script, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE house_scripts SET script = @script WHERE house_id = @houseId AND `index` = @position",
            new { houseId, position, script });
    }

    public async Task DeleteScriptAsync(int houseId, int position, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "DELETE FROM house_scripts WHERE house_id = @houseId AND `index` = @position",
            new { houseId, position });
    }
}
