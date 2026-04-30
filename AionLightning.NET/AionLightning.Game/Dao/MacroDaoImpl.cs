using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class MacroDaoImpl : IMacroDao
{
    private readonly MySqlDataSource _db;

    public MacroDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<IReadOnlyDictionary<int, string>> LoadByPlayerIdAsync(int playerId, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText = "SELECT `order`, `macro` FROM `player_macrosses` WHERE `player_id` = @PlayerId";
        cmd.Parameters.AddWithValue("@PlayerId", playerId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var result = new Dictionary<int, string>();
        while (await reader.ReadAsync(ct))
            result[reader.GetInt32(0)] = reader.GetString(1);
        return result;
    }

    public async Task UpsertAsync(int playerId, int position, string xml, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText =
            "INSERT INTO `player_macrosses` (`player_id`, `order`, `macro`) VALUES (@PlayerId, @Order, @Macro) " +
            "ON DUPLICATE KEY UPDATE `macro` = @Macro";
        cmd.Parameters.AddWithValue("@PlayerId", playerId);
        cmd.Parameters.AddWithValue("@Order",    position);
        cmd.Parameters.AddWithValue("@Macro",    xml);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteAsync(int playerId, int position, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM `player_macrosses` WHERE `player_id` = @PlayerId AND `order` = @Order";
        cmd.Parameters.AddWithValue("@PlayerId", playerId);
        cmd.Parameters.AddWithValue("@Order",    position);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
