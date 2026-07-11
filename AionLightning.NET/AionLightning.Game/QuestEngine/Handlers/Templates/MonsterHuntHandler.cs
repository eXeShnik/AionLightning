using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Templates.Quest;
using AionLightning.Game.Model.Templates.Quest.Script;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.QuestEngine.Handlers.Templates;

/// <summary>
/// Data-driven "kill N of each monster group, turn in" quest handler (Java
/// <c>questEngine.handlers.template.MonsterHunt</c> port) — covers &lt;monster_hunt&gt; entries.
/// Each &lt;monster&gt; child is an independent kill-goal group; a group's progress is packed into
/// one quest var, or a contiguous span of vars when its goal exceeds 63 (6-bit slot capacity).
/// </summary>
/// <remarks>
/// Not ported in this phase: Java's <c>aggro_start_npcs</c>-driven auto-start
/// (<c>onAddAggroListEvent</c>) and <c>invasion_world</c>-driven auto-start (<c>onEnterWorldEvent</c>,
/// which needs RiftService/VortexService) — <see cref="IQuestHandler"/> has no aggro-list or
/// enter-world event hooks yet, and only ~6 of 1,607 monster_hunt entries in the shipped data use
/// either attribute. The REWARD-state aggro dialog gating (Java lines checking
/// <c>!aggroNpcs.isEmpty()</c>) IS ported below since it is pure dialog logic requiring no new event.
/// Also not ported: <c>CustomConfig.QUESTDATA_MONSTER_KILLS</c> npc_seq-to-quest_kill matching — an
/// optional legacy server customization (default off) that widens a group's npc id set from
/// quest_data.xml; this handler always uses the &lt;monster&gt; element's own npc_ids, matching
/// Java's default (config-disabled) code path.
/// </remarks>
public sealed class MonsterHuntHandler : QuestHandlerBase
{
    private readonly HashSet<int>       _startNpcs;
    private readonly HashSet<int>       _endNpcs;
    private readonly HashSet<int>       _aggroNpcs;
    private readonly List<MonsterEntry> _monsters;
    private readonly int                _startDialog;
    private readonly int                _endDialog;

    public MonsterHuntHandler(MonsterHuntScriptEntry data, IDataManager dataManager,
        IQuestDao questDao, QuestRewardService rewardService)
        : base(data.Id, dataManager, questDao, rewardService)
    {
        _startNpcs   = data.StartNpcIds;
        _endNpcs     = data.EndNpcIds;
        _aggroNpcs   = data.AggroStartNpcIds;
        _monsters    = data.Monsters;
        _startDialog = data.StartDialogId != 0 ? data.StartDialogId : 1011;
        _endDialog   = data.EndDialogId   != 0 ? data.EndDialogId   : 1352;
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

        var player       = env.Player;
        int targetId      = env.TargetId;
        int targetObjId   = env.Target?.ObjectId ?? 0;
        var entry         = player.Quests.Get(QuestId);
        var status        = entry?.Status ?? QuestStatus.NONE;
        var dialog        = DialogActionLookup.FromId(env.DialogId);

        switch (status)
        {
            case QuestStatus.NONE:
                if (!_startNpcs.Contains(targetId)) return false;
                if (player.Level < template.MinLevel) return false;

                return dialog switch
                {
                    DialogAction.QUEST_SELECT => await SendQuestDialogAsync(conn, targetObjId, _startDialog, ct),
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

                if (_aggroNpcs.Count > 0)
                {
                    // Defense/escort-style hunts show an intermediate "in progress" page (10002)
                    // instead of the normal turn-in preview (Java lines 140-153).
                    return dialog switch
                    {
                        DialogAction.QUEST_SELECT or DialogAction.USE_OBJECT
                            => await SendQuestDialogAsync(conn, targetObjId, 10002, ct),
                        DialogAction.SELECT_QUEST_REWARD => await SendQuestEndDialogAsync(env, conn, ct),
                        _ => false,
                    };
                }

                return dialog switch
                {
                    DialogAction.QUEST_SELECT        => await SendQuestDialogAsync(conn, targetObjId, _endDialog, ct),
                    DialogAction.SELECT_QUEST_REWARD => await SendQuestEndDialogAsync(env, conn, ct),
                    _ => false,
                };

            default:
                return false;
        }
    }

    /// <summary>
    /// Re-checks every monster group's kill goal and transitions to REWARD when all are satisfied
    /// (Java's pre-check at the top of onDialogEvent's START branch).
    /// </summary>
    private async ValueTask<bool> TryAdvanceToRewardAsync(GsClientConnection conn, QuestEntry entry, int targetObjId, CancellationToken ct)
    {
        if (!AllMonsterGroupsComplete(entry))
            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);

        await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
        return await SendQuestDialogAsync(conn, targetObjId, _endDialog, ct);
    }

    private bool AllMonsterGroupsComplete(QuestEntry entry)
    {
        foreach (var monster in _monsters)
            if (ReadGroupTotal(entry, monster.Var, monster.EndVar).Total < monster.EndVar)
                return false;
        return true;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        foreach (var monster in _monsters)
        {
            if (!monster.NpcIds.Contains(env.TargetId)) continue;

            var (currentTotal, varIdEnd) = ReadGroupTotal(entry, monster.Var, monster.EndVar);
            int total = currentTotal + 1;
            if (total > monster.EndVar) continue; // this group already capped; check remaining groups

            if (_aggroNpcs.Count > 0)
            {
                // Defense/escort hunts: the triggering kill completes the objective outright,
                // without per-group var bookkeeping (Java parity).
                entry.Status = QuestStatus.REWARD;
            }
            else
            {
                for (int slot = monster.Var; slot < varIdEnd; slot++)
                {
                    entry.SetVar(slot, total & 0x3F);
                    total >>= 6;
                }
            }

            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reads a monster group's current packed kill count across its 6-bit var span (Java's
    /// do/while total-decoding loop in onDialogEvent/onKillEvent). Returns the decoded total and
    /// the first var index past the span (i.e. how many slots, starting at <paramref name="varBase"/>,
    /// were needed to represent <paramref name="endVar"/>).
    /// </summary>
    private static (int Total, int VarIdEnd) ReadGroupTotal(QuestEntry entry, int varBase, int endVar)
    {
        int total = 0, varId = varBase, remaining = endVar;
        do
        {
            total += entry.GetVar(varId) << ((varId - varBase) * 6);
            remaining >>= 6;
            varId++;
        } while (remaining > 0);
        return (total, varId);
    }
}
