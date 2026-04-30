using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class RecipeDaoImpl : IRecipeDao
{
    private readonly MySqlDataSource _db;

    public RecipeDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<IReadOnlyList<int>> LoadByPlayerIdAsync(int playerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var ids = await conn.QueryAsync<int>(
            "SELECT recipe_id FROM player_recipes WHERE player_id = @playerId",
            new { playerId });
        return ids.ToList();
    }

    public async Task AddRecipeAsync(int playerId, int recipeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "INSERT IGNORE INTO player_recipes (player_id, recipe_id) VALUES (@playerId, @recipeId)",
            new { playerId, recipeId });
    }

    public async Task DeleteRecipeAsync(int playerId, int recipeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "DELETE FROM player_recipes WHERE player_id = @playerId AND recipe_id = @recipeId",
            new { playerId, recipeId });
    }
}
