namespace AionLightning.Game.Dao;

public interface IPlayerTitleDao
{
    Task<IReadOnlyList<int>> LoadByPlayerIdAsync(int playerId, CancellationToken ct = default);
    Task AddTitleAsync(int playerId, int titleId, CancellationToken ct = default);
}
