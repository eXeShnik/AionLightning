using AionLightning.Game.Model.Item;

namespace AionLightning.Game.Dao;

public interface IManastoneDao
{
    Task<IReadOnlyList<Manastone>> LoadByItemIdsAsync(IEnumerable<long> itemUniqueIds, CancellationToken ct);
    Task InsertAsync(Manastone stone, CancellationToken ct);
    Task DeleteByItemAsync(long itemUniqueId, CancellationToken ct);
}
