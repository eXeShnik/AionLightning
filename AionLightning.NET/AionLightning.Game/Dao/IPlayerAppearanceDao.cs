using AionLightning.Game.Model;

namespace AionLightning.Game.Dao;

public interface IPlayerAppearanceDao
{
    Task<PlayerAppearance?> FindByPlayerIdAsync(int playerId, CancellationToken ct = default);
    Task InsertAsync(int playerId, PlayerAppearance appearance, CancellationToken ct = default);
    Task UpdateAsync(int playerId, PlayerAppearance appearance, CancellationToken ct = default);
}
