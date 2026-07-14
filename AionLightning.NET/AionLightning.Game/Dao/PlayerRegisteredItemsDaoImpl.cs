using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class PlayerRegisteredItemsDaoImpl : IPlayerRegisteredItemsDao
{
    private readonly MySqlDataSource _db;

    public PlayerRegisteredItemsDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<List<PlayerRegisteredItemRow>> LoadByPlayerAsync(int playerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Row>(
            """
            SELECT item_unique_id, item_id, expire_time, color, color_expires, owner_use_count,
                   visitor_use_count, x, y, z, h, area, floor
            FROM player_registered_items WHERE player_id = @playerId
            """, new { playerId });
        return rows.Select(ToRecord).ToList();
    }

    public async Task UpsertAsync(int playerId, PlayerRegisteredItemRow row, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO player_registered_items
                (player_id, item_unique_id, item_id, expire_time, color, color_expires, owner_use_count,
                 visitor_use_count, x, y, z, h, area, floor)
            VALUES
                (@playerId, @ItemUniqueId, @ItemId, @ExpireTime, @Color, @ColorExpires, @OwnerUseCount,
                 @VisitorUseCount, @X, @Y, @Z, @H, @Area, @Floor)
            ON DUPLICATE KEY UPDATE
                expire_time = @ExpireTime, color = @Color, color_expires = @ColorExpires,
                owner_use_count = @OwnerUseCount, visitor_use_count = @VisitorUseCount,
                x = @X, y = @Y, z = @Z, h = @H, area = @Area, floor = @Floor
            """, new
            {
                playerId,
                row.ItemUniqueId,
                row.ItemId,
                row.ExpireTime,
                row.Color,
                row.ColorExpires,
                row.OwnerUseCount,
                row.VisitorUseCount,
                row.X,
                row.Y,
                row.Z,
                row.H,
                row.Area,
                row.Floor,
            });
    }

    public async Task DeleteAsync(int itemUniqueId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "DELETE FROM player_registered_items WHERE item_unique_id = @itemUniqueId", new { itemUniqueId });
    }

    public async Task DeleteAllForPlayerAsync(int playerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "DELETE FROM player_registered_items WHERE player_id = @playerId", new { playerId });
    }

    private static PlayerRegisteredItemRow ToRecord(Row r) => new(
        r.item_unique_id, r.item_id, r.expire_time, r.color, r.color_expires, r.owner_use_count,
        r.visitor_use_count, r.x, r.y, r.z, r.h, r.area, r.floor);

    private sealed record Row(
        int item_unique_id, int item_id, int? expire_time, int? color, int color_expires, int owner_use_count,
        int visitor_use_count, float x, float y, float z, short h, string area, int floor);
}
