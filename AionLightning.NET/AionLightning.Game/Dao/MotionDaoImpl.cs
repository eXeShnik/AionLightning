using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class MotionDaoImpl : IMotionDao
{
    private readonly MySqlDataSource _db;

    public MotionDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<IReadOnlyDictionary<byte, short>> LoadByPlayerIdAsync(int playerId, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText = "SELECT `slot`, `motion_id` FROM `player_motions` WHERE `player_id` = @PlayerId";
        cmd.Parameters.AddWithValue("@PlayerId", playerId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var result = new Dictionary<byte, short>();
        while (await reader.ReadAsync(ct))
            result[reader.GetByte(0)] = reader.GetInt16(1);
        return result;
    }

    public async Task UpsertAsync(int playerId, byte slot, short motionId, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText =
            "INSERT INTO `player_motions` (`player_id`, `slot`, `motion_id`) VALUES (@PlayerId, @Slot, @MotionId) " +
            "ON DUPLICATE KEY UPDATE `motion_id` = @MotionId";
        cmd.Parameters.AddWithValue("@PlayerId", playerId);
        cmd.Parameters.AddWithValue("@Slot",     slot);
        cmd.Parameters.AddWithValue("@MotionId", motionId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteAsync(int playerId, byte slot, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText =
            "DELETE FROM `player_motions` WHERE `player_id` = @PlayerId AND `slot` = @Slot";
        cmd.Parameters.AddWithValue("@PlayerId", playerId);
        cmd.Parameters.AddWithValue("@Slot",     slot);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
