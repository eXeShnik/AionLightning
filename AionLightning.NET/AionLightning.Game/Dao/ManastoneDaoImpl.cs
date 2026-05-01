using AionLightning.Game.Model.Item;
using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class ManastoneDaoImpl : IManastoneDao
{
    private readonly MySqlDataSource _db;

    public ManastoneDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<IReadOnlyList<Manastone>> LoadByItemIdsAsync(IEnumerable<long> itemUniqueIds, CancellationToken ct)
    {
        var ids = itemUniqueIds.ToList();
        if (ids.Count == 0) return [];

        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<(long item_unique_id, int item_id, int slot)>(
            "SELECT item_unique_id, item_id, slot FROM item_stones WHERE item_unique_id IN @Ids",
            new { Ids = ids });
        return rows.Select(r => new Manastone { ItemUniqueId = r.item_unique_id, ItemId = r.item_id, Slot = r.slot })
                   .ToList();
    }

    public async Task InsertAsync(Manastone stone, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "INSERT INTO item_stones (item_unique_id, item_id, slot) VALUES (@ItemUniqueId, @ItemId, @Slot)",
            new { stone.ItemUniqueId, stone.ItemId, stone.Slot });
    }

    public async Task DeleteByItemAsync(long itemUniqueId, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "DELETE FROM item_stones WHERE item_unique_id = @ItemUniqueId",
            new { ItemUniqueId = itemUniqueId });
    }
}
