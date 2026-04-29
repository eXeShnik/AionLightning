using AionLightning.Game.Model.Item;

namespace AionLightning.Game.Dao;

public interface IItemDao
{
    ValueTask<IReadOnlyList<Item>> FindByPlayerIdAsync(int playerId, CancellationToken ct);
    ValueTask SaveAllAsync(int playerId, IEnumerable<Item> items, CancellationToken ct);
    ValueTask<long> NextUniqueIdAsync(CancellationToken ct);
}
