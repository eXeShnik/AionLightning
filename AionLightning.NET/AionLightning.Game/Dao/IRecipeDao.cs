namespace AionLightning.Game.Dao;

public interface IRecipeDao
{
    Task<IReadOnlyList<int>> LoadByPlayerIdAsync(int playerId, CancellationToken ct = default);
    Task AddRecipeAsync(int playerId, int recipeId, CancellationToken ct = default);
    Task DeleteRecipeAsync(int playerId, int recipeId, CancellationToken ct = default);
}
