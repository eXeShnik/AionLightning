using AionLightning.Game.Model.Quest;
using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class QuestDaoImpl : IQuestDao
{
    private readonly MySqlDataSource _db;

    public QuestDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<IReadOnlyList<QuestEntry>> LoadByPlayerIdAsync(int playerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<QuestRow>(
            "SELECT quest_id, status, step, complete_count FROM player_quests WHERE player_id = @playerId",
            new { playerId });
        return rows.Select(r => new QuestEntry
        {
            QuestId       = r.quest_id,
            Status        = (QuestStatus)r.status,
            Step          = r.step,
            CompleteCount = r.complete_count,
        }).ToList();
    }

    public async Task UpsertAsync(int playerId, QuestEntry entry, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO player_quests (player_id, quest_id, status, step, complete_count)
            VALUES (@playerId, @questId, @status, @step, @completeCount)
            ON DUPLICATE KEY UPDATE status = @status, step = @step, complete_count = @completeCount
            """,
            new
            {
                playerId,
                questId       = entry.QuestId,
                status        = (byte)entry.Status,
                step          = entry.Step,
                completeCount = entry.CompleteCount,
            });
    }

    public async Task SaveAllAsync(int playerId, IEnumerable<QuestEntry> quests, CancellationToken ct = default)
    {
        var list = quests.ToList();
        if (list.Count == 0) return;
        await using var conn = await _db.OpenConnectionAsync(ct);
        foreach (var entry in list)
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO player_quests (player_id, quest_id, status, step, complete_count)
                VALUES (@playerId, @questId, @status, @step, @completeCount)
                ON DUPLICATE KEY UPDATE status = @status, step = @step, complete_count = @completeCount
                """,
                new
                {
                    playerId,
                    questId       = entry.QuestId,
                    status        = (byte)entry.Status,
                    step          = entry.Step,
                    completeCount = entry.CompleteCount,
                });
        }
    }

    public async Task DeleteAsync(int playerId, int questId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "DELETE FROM player_quests WHERE player_id = @playerId AND quest_id = @questId",
            new { playerId, questId });
    }

    private sealed record QuestRow(int quest_id, byte status, int step, byte complete_count);
}
