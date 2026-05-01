using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class PlayerTitleDaoImpl : IPlayerTitleDao
{
    private readonly MySqlDataSource _db;

    public PlayerTitleDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<IReadOnlyList<int>> LoadByPlayerIdAsync(int playerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var ids = await conn.QueryAsync<int>(
            "SELECT title_id FROM player_titles WHERE player_id = @playerId",
            new { playerId });
        return ids.ToList();
    }

    public async Task AddTitleAsync(int playerId, int titleId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "INSERT IGNORE INTO player_titles (player_id, title_id) VALUES (@playerId, @titleId)",
            new { playerId, titleId });
    }
}
