using AionLightning.Game.Model.Templates.Challenge;

namespace AionLightning.Game.Dao;

/// <summary>One persisted quest-progress row (Java's `challenge_tasks` table, one row per
/// task/quest/owner combination).</summary>
public sealed record ChallengeTaskRow(int TaskId, int QuestId, int CompleteCount, DateTime CompleteTime);

/// <summary>Port of Java dao.ChallengeTasksDAO — persists per-quest progress toward a legion/town
/// challenge task (see Services/ChallengeTaskService.cs).</summary>
public interface IChallengeTasksDao
{
    Task<List<ChallengeTaskRow>> LoadAsync(int ownerId, ChallengeType type, CancellationToken ct = default);

    Task UpsertQuestProgressAsync(int taskId, int questId, int ownerId, ChallengeType type,
        int completeCount, DateTime completeTime, CancellationToken ct = default);
}
