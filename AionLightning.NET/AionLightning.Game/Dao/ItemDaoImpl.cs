using AionLightning.Game.Model.Item;
using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class ItemDaoImpl : IItemDao
{
    private readonly MySqlDataSource _db;

    public ItemDaoImpl(MySqlDataSource db) => _db = db;

    public async ValueTask<IReadOnlyList<Item>> FindByPlayerIdAsync(int playerId, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT unique_id, item_id, count, slot FROM player_items WHERE player_id = @PlayerId";
        cmd.Parameters.AddWithValue("@PlayerId", playerId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<Item>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(new Item
            {
                UniqueId = reader.GetInt64(0),
                ItemId   = reader.GetInt32(1),
                Count    = reader.GetInt64(2),
                Slot     = reader.GetInt32(3),
            });
        }
        return list;
    }

    public async ValueTask SaveAllAsync(int playerId, IEnumerable<Item> items, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var tx   = await conn.BeginTransactionAsync(ct);

        await using (var del = conn.CreateCommand())
        {
            del.Transaction = tx;
            del.CommandText = "DELETE FROM player_items WHERE player_id = @PlayerId";
            del.Parameters.AddWithValue("@PlayerId", playerId);
            await del.ExecuteNonQueryAsync(ct);
        }

        foreach (var item in items)
        {
            await using var ins = conn.CreateCommand();
            ins.Transaction = tx;
            ins.CommandText = @"INSERT INTO player_items (unique_id, player_id, item_id, count, slot)
                                VALUES (@UniqueId, @PlayerId, @ItemId, @Count, @Slot)";
            ins.Parameters.AddWithValue("@UniqueId", item.UniqueId);
            ins.Parameters.AddWithValue("@PlayerId", playerId);
            ins.Parameters.AddWithValue("@ItemId",   item.ItemId);
            ins.Parameters.AddWithValue("@Count",    item.Count);
            ins.Parameters.AddWithValue("@Slot",     item.Slot);
            await ins.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async ValueTask<long> NextUniqueIdAsync(CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO item_unique_id (stub) VALUES (NULL); SELECT LAST_INSERT_ID();";
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result);
    }
}
