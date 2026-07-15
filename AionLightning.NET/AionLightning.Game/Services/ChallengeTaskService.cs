using System.Collections.Concurrent;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Challenge;
using AionLightning.Game.Model.Legion;
using AionLightning.Game.Model.Templates.Challenge;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Services;

/// <summary>
/// Port of Java services.ChallengeTaskService — legion and town challenge tasks: completing a task's
/// declared quests advances its progress, and reaching each quest's repeat cap (all of them =&gt; the
/// task itself completes) grants a reward toward the owning legion's or town's level/points.
///
/// Town tasks add a flat score to the town's activity points on every quest completion, plus the task's
/// own <see cref="ChallengeReward"/> (POINT type) once the task completes (<see cref="OnTownTaskFinishAsync"/>).
///
/// Legion tasks instead accrue score per-member (<see cref="LegionMember.ChallengeScore"/>); once the
/// task completes, every member (online or offline) is ranked by that score and mailed an item from the
/// task's <see cref="ContributionReward"/> tiers, then everyone's score resets to 0
/// (<see cref="DistributeContributionRewardsAsync"/>).
/// </summary>
public sealed class ChallengeTaskService(
    IDataManager dataManager,
    IChallengeTasksDao challengeTasksDao,
    TownService townService,
    LegionService legionService,
    ILegionDao legionDao,
    SystemMailService mailService,
    PlayerConnectionRegistry connRegistry,
    IOptions<ChallengeOptions> options,
    ILogger<ChallengeTaskService> log)
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, ChallengeTask>> _townTasks = new();
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, ChallengeTask>> _legionTasks = new();

    // --- Client-facing task list (Java showTaskList/buildTaskList) ---

    /// <summary>Java ChallengeTaskService.showTaskList — sends the owner's available task list (action 2)
    /// followed by each task's quest breakdown (action 7). A no-op while
    /// <see cref="ChallengeOptions.Enable"/> is false (default), matching Java's own
    /// CustomConfig.CHALLENGE_TASKS_ENABLED gate on this method specifically — progress tracking and
    /// reward payout via <see cref="OnChallengeQuestFinishAsync"/> are never gated by this flag.</summary>
    public async Task ShowTaskListAsync(GsClientConnection conn, Player player, ChallengeType type, int ownerId, CancellationToken ct = default)
    {
        if (!options.Value.Enable) return;

        var tasks = await GetTaskListAsync(player, type, ownerId, ct);
        await conn.SendAsync(SM_CHALLENGE_LIST.List(ownerId, type, player.ObjectId, tasks), ct);
        foreach (var task in tasks)
            await conn.SendAsync(SM_CHALLENGE_LIST.TaskDetail(ownerId, type, player.ObjectId, task), ct);
    }

    /// <summary>Java ChallengeTaskService.buildTaskList — the owner's already-tracked tasks (repeatable
    /// ones always included, one-shot ones only while incomplete) plus any newly-available task whose
    /// level/race/prerequisite/town-residence gates the player now satisfies (created and persisted with
    /// zero progress on the spot).</summary>
    public async Task<List<ChallengeTask>> GetTaskListAsync(Player player, ChallengeType type, int ownerId, CancellationToken ct = default)
    {
        var taskMap = type == ChallengeType.LEGION ? _legionTasks : _townTasks;
        var ownerTasks = await GetOrLoadOwnerTasksAsync(taskMap, ownerId, type, ct);

        int ownerLevel = type == ChallengeType.LEGION
            ? legionService.GetById(ownerId)?.Level ?? 0
            : townService.GetTownById(ownerId)?.Level ?? 0;
        int playerTownId = townService.GetTownResidence(player);

        var available = new List<ChallengeTask>();
        foreach (var task in ownerTasks.Values)
            if (task.Template.Repeat || !task.IsCompleted)
                available.Add(task);

        foreach (var template in dataManager.Challenges.Tasks.Values)
        {
            if (template.Type != type || template.Race != player.Race) continue;
            if (ownerTasks.ContainsKey(template.Id)) continue;
            if (ownerLevel < template.MinLevel || ownerLevel > template.MaxLevel) continue;
            if (template.TownResidence && playerTownId != ownerId) continue;

            if (!template.HasPrevTask)
            {
                available.Add(await CreateAndStoreTaskAsync(ownerTasks, ownerId, type, template, ct));
                continue;
            }

            if (ownerTasks.TryGetValue(template.PrevTask, out var prevTask) && prevTask.IsCompleted)
                available.Add(await CreateAndStoreTaskAsync(ownerTasks, ownerId, type, template, ct));
        }

        return available;
    }

    private async Task<ConcurrentDictionary<int, ChallengeTask>> GetOrLoadOwnerTasksAsync(
        ConcurrentDictionary<int, ConcurrentDictionary<int, ChallengeTask>> taskMap, int ownerId, ChallengeType type, CancellationToken ct)
    {
        if (taskMap.TryGetValue(ownerId, out var existing)) return existing;

        var rows = await challengeTasksDao.LoadAsync(ownerId, type, ct);
        var tasks = new ConcurrentDictionary<int, ChallengeTask>();
        foreach (var group in rows.GroupBy(r => r.TaskId))
        {
            if (dataManager.Challenges.GetTaskByTaskId(group.Key) is not { } template) continue;

            var quests = new Dictionary<int, ChallengeQuest>();
            var latestComplete = ChallengeTask.NeverCompleted;
            foreach (var questTemplate in template.Quests)
            {
                var row = group.FirstOrDefault(r => r.QuestId == questTemplate.Id);
                quests[questTemplate.Id] = new ChallengeQuest(questTemplate, row?.CompleteCount ?? 0);
                if (row is not null && row.CompleteTime > latestComplete)
                    latestComplete = row.CompleteTime;
            }

            tasks[group.Key] = new ChallengeTask(ownerId, template, quests, latestComplete);
        }

        return taskMap.GetOrAdd(ownerId, tasks);
    }

    private async Task<ChallengeTask> CreateAndStoreTaskAsync(
        ConcurrentDictionary<int, ChallengeTask> ownerTasks, int ownerId, ChallengeType type,
        ChallengeTaskTemplate template, CancellationToken ct)
    {
        var task = new ChallengeTask(ownerId, template);
        ownerTasks[task.TaskId] = task;

        foreach (var quest in task.Quests.Values)
            await challengeTasksDao.UpsertQuestProgressAsync(task.TaskId, quest.QuestId, ownerId, type, quest.CompleteCount, task.CompleteTime, ct);

        return task;
    }

    // --- Quest-completion hook (Java onChallengeQuestFinish) ---

    /// <summary>
    /// Java ChallengeTaskService.onChallengeQuestFinish — called from the quest-completion path
    /// (<see cref="QuestRewardService.GrantAndCompleteAsync"/>) for every completed quest. A no-op
    /// (cheap dictionary lookup) unless <paramref name="questId"/> actually belongs to a challenge task —
    /// this port has no ported QuestCategory field to gate on ahead of the call (see
    /// migration_plan.md), so the guard lives here instead.
    /// </summary>
    public async Task OnChallengeQuestFinishAsync(Player player, int questId, CancellationToken ct = default)
    {
        if (dataManager.Challenges.GetTaskByQuestId(questId) is not { } template) return;

        switch (template.Type)
        {
            case ChallengeType.TOWN:
                await OnTownTaskFinishAsync(player, template, questId, ct);
                break;
            case ChallengeType.LEGION:
                await OnLegionTaskFinishAsync(player, template, questId, ct);
                break;
        }
    }

    private async Task OnTownTaskFinishAsync(Player player, ChallengeTaskTemplate template, int questId, CancellationToken ct)
    {
        int townId = townService.GetTownIdByPosition(player.Position);
        var ownerTasks = await GetOrLoadOwnerTasksAsync(_townTasks, townId, ChallengeType.TOWN, ct);

        if (!ownerTasks.TryGetValue(template.Id, out var task) || !task.Quests.TryGetValue(questId, out var quest))
        {
            log.LogWarning(
                "Player {Name} tried to finish town challenge quest {QuestId} for task {TaskId} in town {TownId} without an active task",
                player.Name, questId, template.Id, townId);
            return;
        }

        if (quest.CompleteCount >= quest.MaxRepeats) return;
        if (task.IsCompleted) return;

        task.UpdateCompleteTime();
        quest.IncreaseCompleteCount();
        await challengeTasksDao.UpsertQuestProgressAsync(task.TaskId, questId, townId, ChallengeType.TOWN, quest.CompleteCount, task.CompleteTime, ct);

        bool leveledUp = await townService.IncreasePointsAsync(townId, quest.ScorePerQuest, ct);

        if (task.IsCompleted)
        {
            switch (template.Reward.Type)
            {
                case RewardType.POINT:
                    leveledUp |= await townService.IncreasePointsAsync(townId, template.Reward.Value, ct);
                    break;
                case RewardType.SPAWN:
                    // TODO: Java's own SPAWN reward branch was never implemented either — no-op.
                    break;
            }
        }

        if (!leveledUp) return;
        if (townService.GetTownById(townId) is not { } town) return;

        var levelUpPkt = SM_SYSTEM_MESSAGE.TownLevelUp(townId, town.Level);
        if (connRegistry.Get(player.ObjectId) is { } conn)
            try { await conn.SendAsync(levelUpPkt, ct); } catch { /* best-effort notify */ }
    }

    private async Task OnLegionTaskFinishAsync(Player player, ChallengeTaskTemplate template, int questId, CancellationToken ct)
    {
        // Java: player could take a challenge task and then leave (or switch) legions before finishing it.
        if (player.Legion is not { } legion) return;
        if (!legion.Members.TryGetValue(player.ObjectId, out var member)) return;

        var ownerTasks = await GetOrLoadOwnerTasksAsync(_legionTasks, legion.LegionId, ChallengeType.LEGION, ct);
        if (!ownerTasks.TryGetValue(template.Id, out var task) || !task.Quests.TryGetValue(questId, out var quest))
            return;

        if (quest.CompleteCount >= quest.MaxRepeats) return;

        member.ChallengeScore += quest.ScorePerQuest;
        await legionDao.UpdateChallengeScoreAsync(member.ObjectId, member.ChallengeScore, ct);

        if (task.IsCompleted) return;

        task.UpdateCompleteTime();
        quest.IncreaseCompleteCount();
        await challengeTasksDao.UpsertQuestProgressAsync(task.TaskId, questId, legion.LegionId, ChallengeType.LEGION, quest.CompleteCount, task.CompleteTime, ct);

        if (task.IsCompleted)
            await DistributeContributionRewardsAsync(legion, template, ct);
    }

    /// <summary>Java onLegionTaskFinish's winnersByPoints ranking + reward mail loop — every legion member
    /// (online or offline; all are already loaded in <see cref="Legion.Members"/>) is ranked by
    /// descending <see cref="LegionMember.ChallengeScore"/> (ties broken by object id for a deterministic
    /// order Java left to map-bucket iteration), then walked in that order against
    /// <see cref="ChallengeTaskTemplate.Contrib"/>'s ascending-number tiers: a member is mailed the first
    /// tier whose cumulative winner-count threshold hasn't been exhausted yet. Every member's score is
    /// then reset to 0, regardless of whether they placed in a tier.</summary>
    private async Task DistributeContributionRewardsAsync(Legion legion, ChallengeTaskTemplate template, CancellationToken ct)
    {
        var ranked = legion.Members.Values
            .OrderByDescending(m => m.ChallengeScore)
            .ThenBy(m => m.ObjectId)
            .ToList();

        int rewardsGranted = 0;
        foreach (var member in ranked)
        {
            foreach (var reward in template.Contrib)
            {
                if (rewardsGranted > reward.Number) continue;

                rewardsGranted++;
                await mailService.SendSystemMailAsync(member.ObjectId, "Legion reward", "", "",
                    attachedItemId: reward.RewardId, attachedItemCount: reward.ItemCount, ct: ct);
                break;
            }

            member.ChallengeScore = 0;
            await legionDao.UpdateChallengeScoreAsync(member.ObjectId, 0, ct);
        }
    }

    // --- Legion level-up gate (Java canRaiseLegionLevel) ---

    /// <summary>
    /// Java ChallengeTaskService.canRaiseLegionLevel — true if the legion has completed a challenge task
    /// gating the given level. note: not currently wired into a level-up flow —
    /// Network/Aion/ClientPackets/CM_LEGION.cs's HandleLevelUpAsync doesn't gate on this yet (Java's
    /// LegionConfig.ENABLE_GUILD_TASK_REQ has no ported config counterpart). Kept here for parity so a
    /// future level-up gate can call it directly.
    /// </summary>
    public async Task<bool> CanRaiseLegionLevelAsync(int legionId, int legionLevel, CancellationToken ct = default)
    {
        var tasks = await GetOrLoadOwnerTasksAsync(_legionTasks, legionId, ChallengeType.LEGION, ct);
        return tasks.Values.Any(t => t.Template.MinLevel == legionLevel && t.IsCompleted);
    }
}
