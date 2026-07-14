using AionLightning.Game.Model.Item;
using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class ItemDaoImpl : IItemDao
{
    private readonly MySqlDataSource _db;

    public ItemDaoImpl(MySqlDataSource db) => _db = db;

    public async ValueTask<IReadOnlyList<Item>> FindByPlayerIdAsync(int playerId, CancellationToken ct)
        => await LoadItemsAsync(playerId, storageType: 0, ct);

    public async ValueTask<IReadOnlyList<Item>> FindWarehouseItemsAsync(int playerId, CancellationToken ct)
        => await LoadItemsAsync(playerId, storageType: 1, ct);

    private async ValueTask<IReadOnlyList<Item>> LoadItemsAsync(int playerId, byte storageType, CancellationToken ct)
    {
        await using var conn   = await _db.OpenConnectionAsync(ct);
        await using var cmd    = conn.CreateCommand();
        cmd.CommandText = @"SELECT unique_id, item_id, count, slot, storage_type, enchant_level, godstone_item_id, optional_socket, skin_item_id, fusioned_item_id, dye_color, is_equipped
                            FROM player_items
                            WHERE player_id = @PlayerId AND storage_type = @StorageType";
        cmd.Parameters.AddWithValue("@PlayerId",    playerId);
        cmd.Parameters.AddWithValue("@StorageType", storageType);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<Item>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(new Item
            {
                UniqueId       = reader.GetInt64(0),
                ItemId         = reader.GetInt32(1),
                Count          = reader.GetInt64(2),
                Slot           = reader.GetInt64(3),
                StorageType    = reader.GetByte(4),
                EnchantLevel   = reader.GetByte(5),
                GodStoneItemId = reader.GetInt32(6),
                OptionalSocket = reader.GetInt32(7),
                SkinItemId     = reader.GetInt32(8),
                FusionedItemId = reader.GetInt32(9),
                DyeColor       = reader.GetInt32(10),
                IsEquipped     = reader.GetBoolean(11),
            });
        }
        return list;
    }

    public ValueTask SaveAllAsync(int playerId, IEnumerable<Item> items, CancellationToken ct)
        => ReplaceItemsAsync(playerId, items, storageType: 0, ct);

    public ValueTask SaveWarehouseAsync(int playerId, IEnumerable<Item> items, CancellationToken ct)
        => ReplaceItemsAsync(playerId, items, storageType: 1, ct);

    private async ValueTask ReplaceItemsAsync(int playerId, IEnumerable<Item> items, byte storageType, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var tx   = await conn.BeginTransactionAsync(ct);

        await using (var del = conn.CreateCommand())
        {
            del.Transaction = tx;
            del.CommandText = "DELETE FROM player_items WHERE player_id = @PlayerId AND storage_type = @StorageType";
            del.Parameters.AddWithValue("@PlayerId",    playerId);
            del.Parameters.AddWithValue("@StorageType", storageType);
            await del.ExecuteNonQueryAsync(ct);
        }

        foreach (var item in items)
        {
            await using var ins = conn.CreateCommand();
            ins.Transaction = tx;
            ins.CommandText = @"INSERT INTO player_items
                                    (unique_id, player_id, item_id, count, slot, storage_type, enchant_level, godstone_item_id, optional_socket, skin_item_id, fusioned_item_id, dye_color, is_equipped)
                                VALUES (@UniqueId, @PlayerId, @ItemId, @Count, @Slot, @StorageType, @EnchantLevel, @GodStoneItemId, @OptionalSocket, @SkinItemId, @FusionedItemId, @DyeColor, @IsEquipped)";
            ins.Parameters.AddWithValue("@UniqueId",        item.UniqueId);
            ins.Parameters.AddWithValue("@PlayerId",        playerId);
            ins.Parameters.AddWithValue("@ItemId",          item.ItemId);
            ins.Parameters.AddWithValue("@Count",           item.Count);
            ins.Parameters.AddWithValue("@Slot",            item.Slot);
            ins.Parameters.AddWithValue("@StorageType",     storageType);
            ins.Parameters.AddWithValue("@EnchantLevel",    item.EnchantLevel);
            ins.Parameters.AddWithValue("@GodStoneItemId",  item.GodStoneItemId);
            ins.Parameters.AddWithValue("@OptionalSocket",  item.OptionalSocket);
            ins.Parameters.AddWithValue("@SkinItemId",      item.SkinItemId);
            ins.Parameters.AddWithValue("@FusionedItemId",  item.FusionedItemId);
            ins.Parameters.AddWithValue("@DyeColor",        item.DyeColor);
            ins.Parameters.AddWithValue("@IsEquipped",      item.IsEquipped ? 1 : 0);
            await ins.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async ValueTask<IReadOnlyList<Item>> FindAccountWarehouseAsync(int accountId, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText = "SELECT unique_id, item_id, count, slot, enchant_level FROM account_warehouse_items WHERE account_id = @AccountId";
        cmd.Parameters.AddWithValue("@AccountId", accountId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<Item>();
        while (await reader.ReadAsync(ct))
            list.Add(new Item
            {
                UniqueId     = reader.GetInt64(0),
                ItemId       = reader.GetInt32(1),
                Count        = reader.GetInt64(2),
                Slot         = reader.GetInt64(3),
                EnchantLevel = reader.GetByte(4),
                StorageType  = 2,
            });
        return list;
    }

    public async ValueTask SaveAccountWarehouseAsync(int accountId, IEnumerable<Item> items, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var tx   = await conn.BeginTransactionAsync(ct);

        await using (var del = conn.CreateCommand())
        {
            del.Transaction = tx;
            del.CommandText = "DELETE FROM account_warehouse_items WHERE account_id = @AccountId";
            del.Parameters.AddWithValue("@AccountId", accountId);
            await del.ExecuteNonQueryAsync(ct);
        }

        foreach (var item in items)
        {
            await using var ins = conn.CreateCommand();
            ins.Transaction = tx;
            ins.CommandText = @"INSERT INTO account_warehouse_items
                                    (unique_id, account_id, item_id, count, slot, enchant_level)
                                VALUES (@UniqueId, @AccountId, @ItemId, @Count, @Slot, @EnchantLevel)";
            ins.Parameters.AddWithValue("@UniqueId",     item.UniqueId);
            ins.Parameters.AddWithValue("@AccountId",    accountId);
            ins.Parameters.AddWithValue("@ItemId",       item.ItemId);
            ins.Parameters.AddWithValue("@Count",        item.Count);
            ins.Parameters.AddWithValue("@Slot",         item.Slot);
            ins.Parameters.AddWithValue("@EnchantLevel", item.EnchantLevel);
            await ins.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async ValueTask DeleteAsync(long uniqueId, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM player_items WHERE unique_id = @UniqueId";
        cmd.Parameters.AddWithValue("@UniqueId", uniqueId);
        await cmd.ExecuteNonQueryAsync(ct);
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
