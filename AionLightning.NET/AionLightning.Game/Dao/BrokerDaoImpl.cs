using AionLightning.Game.Model.Broker;
using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class BrokerDaoImpl : IBrokerDao
{
    private readonly MySqlDataSource _db;

    public BrokerDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<IReadOnlyList<BrokerItem>> LoadAllAsync(CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText = @"SELECT id, item_id, seller_id, seller_name, creator_name,
                                   item_count, price, race, enchant_level,
                                   is_settled, is_sold, is_canceled, expire_time, settle_time
                            FROM broker_items";
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var list = new List<BrokerItem>();
        while (await r.ReadAsync(ct))
            list.Add(Map(r));
        return list;
    }

    public async Task<int> InsertAsync(BrokerItem item, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO broker_items
            (item_id, seller_id, seller_name, creator_name, item_count, price, race, enchant_level,
             is_settled, is_sold, is_canceled, expire_time, settle_time)
            VALUES (@ItemId,@SellerId,@SellerName,@CreatorName,@ItemCount,@Price,@Race,@EnchantLevel,
                    0,0,0,@ExpireTime,NULL);
            SELECT LAST_INSERT_ID();";
        cmd.Parameters.AddWithValue("@ItemId",       item.ItemId);
        cmd.Parameters.AddWithValue("@SellerId",     item.SellerId);
        cmd.Parameters.AddWithValue("@SellerName",   item.SellerName);
        cmd.Parameters.AddWithValue("@CreatorName",  item.CreatorName);
        cmd.Parameters.AddWithValue("@ItemCount",    item.ItemCount);
        cmd.Parameters.AddWithValue("@Price",        item.Price);
        cmd.Parameters.AddWithValue("@Race",         item.Race);
        cmd.Parameters.AddWithValue("@EnchantLevel", item.EnchantLevel);
        cmd.Parameters.AddWithValue("@ExpireTime",   item.ExpireTime);
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }

    public async Task MarkSoldAsync(int id, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText = "UPDATE broker_items SET is_sold=1, is_settled=1, settle_time=NOW() WHERE id=@Id";
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task MarkCanceledAsync(int id, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText = "UPDATE broker_items SET is_canceled=1 WHERE id=@Id";
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task MarkSettledAsync(int id, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText = "UPDATE broker_items SET is_settled=1, settle_time=NOW() WHERE id=@Id";
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM broker_items WHERE id=@Id";
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static BrokerItem Map(MySqlDataReader r) => new()
    {
        Id           = r.GetInt32(0),
        ItemId       = r.GetInt32(1),
        SellerId     = r.GetInt32(2),
        SellerName   = r.GetString(3),
        CreatorName  = r.GetString(4),
        ItemCount    = r.GetInt64(5),
        Price        = r.GetInt64(6),
        Race         = r.GetInt32(7),
        EnchantLevel = r.GetByte(8),
        IsSettled    = r.GetBoolean(9),
        IsSold       = r.GetBoolean(10),
        IsCanceled   = r.GetBoolean(11),
        ExpireTime   = r.GetDateTime(12),
        SettleTime   = r.IsDBNull(13) ? null : r.GetDateTime(13),
    };
}
