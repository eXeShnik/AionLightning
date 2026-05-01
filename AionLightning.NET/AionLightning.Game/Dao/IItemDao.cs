using AionLightning.Game.Model.Item;

namespace AionLightning.Game.Dao;

public interface IItemDao
{
    /// <summary>Loads inventory items (storage_type=0) for a player.</summary>
    ValueTask<IReadOnlyList<Item>> FindByPlayerIdAsync(int playerId, CancellationToken ct);

    /// <summary>Loads personal warehouse items (storage_type=1) for a player.</summary>
    ValueTask<IReadOnlyList<Item>> FindWarehouseItemsAsync(int playerId, CancellationToken ct);

    /// <summary>Replaces all inventory (storage_type=0) items for the player.</summary>
    ValueTask SaveAllAsync(int playerId, IEnumerable<Item> items, CancellationToken ct);

    /// <summary>Replaces all warehouse (storage_type=1) items for the player.</summary>
    ValueTask SaveWarehouseAsync(int playerId, IEnumerable<Item> items, CancellationToken ct);

    /// <summary>Loads account warehouse items shared across all characters on an account.</summary>
    ValueTask<IReadOnlyList<Item>> FindAccountWarehouseAsync(int accountId, CancellationToken ct);

    /// <summary>Replaces all account warehouse items for the account.</summary>
    ValueTask SaveAccountWarehouseAsync(int accountId, IEnumerable<Item> items, CancellationToken ct);

    ValueTask<long> NextUniqueIdAsync(CancellationToken ct);
    ValueTask DeleteAsync(long uniqueId, CancellationToken ct);
}
