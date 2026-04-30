namespace AionLightning.Game.Dao;

public interface IMacroDao
{
    Task<IReadOnlyDictionary<int, string>> LoadByPlayerIdAsync(int playerId, CancellationToken ct = default);
    Task UpsertAsync(int playerId, int position, string xml, CancellationToken ct = default);
    Task DeleteAsync(int playerId, int position, CancellationToken ct = default);
}
