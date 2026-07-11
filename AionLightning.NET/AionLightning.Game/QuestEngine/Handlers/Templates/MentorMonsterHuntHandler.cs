using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Templates.Quest.Script;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.QuestEngine.Handlers.Templates;

/// <summary>
/// Data-driven kill-N-monsters hunt intended to be shared between an active mentor and their
/// mentee (Java <c>questEngine.handlers.template.MentorMonsterHunt</c>, which extends
/// <c>MonsterHunt</c> port) — covers &lt;mentor_monster_hunt&gt; entries (~34 on disk). Dialog flow
/// and kill-group bookkeeping otherwise mirror <see cref="MonsterHuntHandler"/> exactly (single var
/// per group here since observed <c>end_var</c> values never exceed 63, so no packed multi-slot
/// span is needed — same simpler style already used by <see cref="KillSpawnedHandler"/>).
/// </summary>
/// <remarks>
/// **Documented limitation**: Java's <c>onKillEvent</c> override gates kill-credit behind the
/// player's live mentor/mentee group relation (<c>player.isMentor()</c> + a same-group member
/// within level range and <c>GROUP_MAX_DISTANCE</c>, or the symmetric mentee-near-mentor check).
/// This port's <c>Player</c>/<c>PlayerGroup</c> model has no mentor/mentee concept at all yet (no
/// <c>IsMentor</c> flag, no mentor-pairing state) — see migration_plan.md, this is a pre-existing
/// gap in the player/group model, not something introduced by this template. Per the task's
/// "log/skip with a documented limitation" instruction, kill-credit here is granted unconditionally
/// (same as a plain <see cref="MonsterHuntHandler"/> hunt) rather than silently no-op'ing the whole
/// template; <c>min_mente_level</c>/<c>max_mente_level</c> are parsed onto
/// <see cref="MentorMonsterHuntScriptEntry"/> for data completeness but intentionally unused here.
/// </remarks>
public sealed class MentorMonsterHuntHandler : QuestHandlerBase
{
    private readonly HashSet<int>       _startNpcs;
    private readonly HashSet<int>       _endNpcs;
    private readonly List<MonsterEntry> _monsters;

    public MentorMonsterHuntHandler(MentorMonsterHuntScriptEntry data, IDataManager dataManager,
        IQuestDao questDao, QuestRewardService rewardService)
        : base(data.Id, dataManager, questDao, rewardService)
    {
        _startNpcs = data.StartNpcIds;
        _endNpcs   = data.EndNpcIds;
        _monsters  = data.Monsters;
    }

    public override void Register(QuestEngine engine)
    {
        foreach (int npcId in _startNpcs)
        {
            var npc = engine.RegisterQuestNpc(npcId);
            npc.OnQuestStart.Add(QuestId);
            npc.OnTalk.Add(QuestId);
        }

        foreach (var monster in _monsters)
            foreach (int npcId in monster.NpcIds)
                engine.RegisterQuestNpc(npcId).OnKill.Add(QuestId);

        foreach (int npcId in _endNpcs)
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var template = Template;
        if (template is null) return false;

        var player      = env.Player;
        int targetId     = env.TargetId;
        int targetObjId  = env.Target?.ObjectId ?? 0;
        var entry        = player.Quests.Get(QuestId);
        var status       = entry?.Status ?? QuestStatus.NONE;
        var dialog       = DialogActionLookup.FromId(env.DialogId);

        switch (status)
        {
            case QuestStatus.NONE:
                if (_startNpcs.Count > 0 && !_startNpcs.Contains(targetId)) return false;
                if (player.Level < template.MinLevel) return false;

                return dialog switch
                {
                    DialogAction.QUEST_SELECT => await SendQuestDialogAsync(conn, targetObjId, 1011, ct),
                    DialogAction.QUEST_ACCEPT or DialogAction.QUEST_ACCEPT_1
                        or DialogAction.QUEST_REFUSE or DialogAction.QUEST_REFUSE_1
                        or DialogAction.QUEST_REFUSE_2 or DialogAction.QUEST_REFUSE_SIMPLE
                        => await SendQuestStartDialogAsync(env, conn, ct),
                    _ => false,
                };

            case QuestStatus.START:
                if (!_endNpcs.Contains(targetId)) return false;
                if (dialog != DialogAction.QUEST_SELECT) return false;

                return await TryAdvanceToRewardAsync(conn, entry!, targetObjId, ct);

            case QuestStatus.REWARD:
                if (!_endNpcs.Contains(targetId)) return false;

                return dialog switch
                {
                    DialogAction.QUEST_SELECT        => await SendQuestDialogAsync(conn, targetObjId, 1352, ct),
                    DialogAction.SELECT_QUEST_REWARD => await SendQuestEndDialogAsync(env, conn, ct),
                    _ => false,
                };

            default:
                return false;
        }
    }

    private async ValueTask<bool> TryAdvanceToRewardAsync(GsClientConnection conn, QuestEntry entry, int targetObjId, CancellationToken ct)
    {
        if (!AllGroupsComplete(entry))
            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);

        await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
    }

    private bool AllGroupsComplete(QuestEntry entry)
    {
        foreach (var monster in _monsters)
            if (entry.GetVar(monster.Var) < monster.EndVar) return false;
        return true;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        // Mentor/mentee relation gate not enforced — see remarks above.
        foreach (var monster in _monsters)
        {
            if (!monster.NpcIds.Contains(env.TargetId)) continue;
            if (entry.GetVar(monster.Var) >= monster.EndVar) continue;

            entry.SetVar(monster.Var, entry.GetVar(monster.Var) + 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }

        return false;
    }
}
