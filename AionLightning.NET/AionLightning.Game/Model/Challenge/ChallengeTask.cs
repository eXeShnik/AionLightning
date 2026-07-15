using AionLightning.Game.Model.Templates.Challenge;

namespace AionLightning.Game.Model.Challenge;

/// <summary>
/// Port of Java model.challenge.ChallengeTask — a legion or town's runtime progress toward one
/// <see cref="ChallengeTaskTemplate"/>, tracked per-quest via <see cref="Quests"/>.
/// </summary>
public sealed class ChallengeTask
{
    public int TaskId  { get; }
    public int OwnerId { get; }
    public ChallengeTaskTemplate Template { get; }
    public Dictionary<int, ChallengeQuest> Quests { get; }
    public DateTime CompleteTime { get; private set; }

    /// <summary>Used when reconstructing a previously-persisted task (Java's DAO-loading constructor).</summary>
    public ChallengeTask(int ownerId, ChallengeTaskTemplate template, Dictionary<int, ChallengeQuest> quests, DateTime completeTime)
    {
        OwnerId      = ownerId;
        Template     = template;
        TaskId       = template.Id;
        Quests       = quests;
        CompleteTime = completeTime;
    }

    /// <summary>Java's <c>new Timestamp(1000)</c> sentinel for "never completed" — one second past the
    /// epoch rather than the epoch itself, since MySQL's TIMESTAMP column type can't hold 1970-01-01
    /// 00:00:00 UTC (its documented minimum is 00:00:01).</summary>
    public static readonly DateTime NeverCompleted = DateTime.UnixEpoch.AddSeconds(1);

    /// <summary>Used when a task first becomes available to an owner (Java's runtime-creation constructor).</summary>
    public ChallengeTask(int ownerId, ChallengeTaskTemplate template)
        : this(ownerId, template,
            template.Quests.ToDictionary(q => q.Id, q => new ChallengeQuest(q)),
            NeverCompleted)
    {
    }

    public int QuestsCount => Quests.Count;

    /// <summary>Java ChallengeTask.isCompleted() — every declared quest has reached its repeat cap.</summary>
    public bool IsCompleted => Quests.Values.All(q => q.CompleteCount >= q.MaxRepeats);

    public void UpdateCompleteTime() => CompleteTime = DateTime.UtcNow;
}
