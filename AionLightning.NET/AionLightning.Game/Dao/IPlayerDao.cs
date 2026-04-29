using AionLightning.Game.Model;

namespace AionLightning.Game.Dao;

public interface IPlayerDao
{
    Task<IReadOnlyList<Player>> FindByAccountIdAsync(int accountId, CancellationToken ct = default);
    Task<Player?> FindByObjectIdAsync(int objectId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task<int> InsertAsync(Player player, CancellationToken ct = default);
    Task UpdatePositionAsync(int playerId, Position pos, CancellationToken ct = default);
    Task UpdateOnlineAsync(int playerId, bool online, CancellationToken ct = default);
}
