using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class SkillDaoImpl : ISkillDao
{
    private readonly MySqlDataSource _db;

    public SkillDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<IReadOnlyList<(int SkillId, int SkillLevel)>> LoadByPlayerIdAsync(int playerId, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText = "SELECT `skill_id`, `skill_level` FROM `player_skills` WHERE `player_id` = @PlayerId";
        cmd.Parameters.AddWithValue("@PlayerId", playerId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var result = new List<(int, int)>();
        while (await reader.ReadAsync(ct))
            result.Add((reader.GetInt32(0), reader.GetInt16(1)));
        return result;
    }

    public async Task UpsertAsync(int playerId, int skillId, int skillLevel, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText =
            "INSERT INTO `player_skills` (`player_id`, `skill_id`, `skill_level`) VALUES (@PlayerId, @SkillId, @SkillLevel) " +
            "ON DUPLICATE KEY UPDATE `skill_level` = @SkillLevel";
        cmd.Parameters.AddWithValue("@PlayerId", playerId);
        cmd.Parameters.AddWithValue("@SkillId", skillId);
        cmd.Parameters.AddWithValue("@SkillLevel", (short)skillLevel);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
