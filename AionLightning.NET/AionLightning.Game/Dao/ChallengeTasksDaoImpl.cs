using AionLightning.Game.Model.Templates.Challenge;
using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

/// <summary>Port of Java dao.mysql5-style ChallengeTasksDAO implementation, backed by
/// V49__challenge_tasks.sql's `challenge_tasks` table.</summary>
public sealed class ChallengeTasksDaoImpl : IChallengeTasksDao
{
    private readonly MySqlDataSource _db;

    public ChallengeTasksDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<List<ChallengeTaskRow>> LoadAsync(int ownerId, ChallengeType type, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Row>(
            "SELECT task_id, quest_id, complete_count, complete_time FROM challenge_tasks WHERE owner_id = @ownerId AND owner_type = @ownerType",
            new { ownerId, ownerType = type.ToString() });
        return rows.Select(r => new ChallengeTaskRow(r.task_id, r.quest_id, r.complete_count, r.complete_time)).ToList();
    }

    public async Task UpsertQuestProgressAsync(int taskId, int questId, int ownerId, ChallengeType type,
        int completeCount, DateTime completeTime, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO challenge_tasks (task_id, quest_id, owner_id, owner_type, complete_count, complete_time)
            VALUES (@taskId, @questId, @ownerId, @ownerType, @completeCount, @completeTime)
            ON DUPLICATE KEY UPDATE complete_count = @completeCount, complete_time = @completeTime
            """,
            new { taskId, questId, ownerId, ownerType = type.ToString(), completeCount, completeTime });
    }

    private sealed record Row(int task_id, int quest_id, int complete_count, DateTime complete_time);
}
