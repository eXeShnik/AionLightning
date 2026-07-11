using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Templates.Quest.Script;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.QuestEngine.Handlers.Templates;

/// <summary>
/// Data-driven "use skill X N times" quest handler (Java
/// <c>questEngine.handlers.template.SkillUse</c> port) — covers &lt;skill_use&gt; entries (30 on
/// disk). Each &lt;skill&gt; child is an independent use-count objective group, packed into one
/// quest var (or a contiguous span, for goals over 63 uses) exactly like &lt;monster_hunt&gt;'s
/// &lt;monster&gt; groups.
/// </summary>
/// <remarks>
/// The quest engine had no skill-cast event before this phase — added
/// <see cref="QuestEngine.OnSkillUseAsync"/> + <see cref="QuestEngine.RegisterSkillUse"/> (mirrors
/// the existing <c>OnItemGetAsync</c>/<c>RegisterItemGet</c> pattern) and wired one call into
/// <c>CM_CASTSPELL.cs</c> right after cooldown/cost checks pass (see that file for the exact call
/// site). Java's own REWARD-status click check (<c>onDialogEvent</c>'s <c>SELECT_QUEST_REWARD</c>
/// branch at STATUS.START, which transitions to REWARD without re-validating completion) is
/// tightened here to require every skill group's use-count objective be met first, matching the
/// gating convention already used by <c>MonsterHuntHandler</c>/<c>KillSpawnedHandler</c>.
/// </remarks>
public sealed class SkillUseHandler : QuestHandlerBase
{
    private readonly int _startNpc;
    private readonly int _endNpc;
    private readonly List<SkillUseGroupEntry> _skillGroups;

    public SkillUseHandler(SkillUseScriptEntry data, IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService)
        : base(data.Id, dataManager, questDao, rewardService)
    {
        _startNpc    = data.StartNpcId;
        _endNpc      = data.EndNpcId != 0 ? data.EndNpcId : data.StartNpcId;
        _skillGroups = data.Skills;
    }

    public override void Register(QuestEngine engine)
    {
        var startNpc = engine.RegisterQuestNpc(_startNpc);
        startNpc.OnQuestStart.Add(QuestId);
        startNpc.OnTalk.Add(QuestId);

        if (_endNpc != _startNpc)
            engine.RegisterQuestNpc(_endNpc).OnTalk.Add(QuestId);

        foreach (var group in _skillGroups)
            foreach (int skillId in group.SkillIds)
                engine.RegisterSkillUse(skillId, QuestId);
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
                if (targetId != _startNpc) return false;
                if (player.Level < template.MinLevel) return false;

                return dialog switch
                {
                    DialogAction.QUEST_SELECT => await SendQuestDialogAsync(conn, targetObjId, 4762, ct),
                    DialogAction.QUEST_ACCEPT or DialogAction.QUEST_ACCEPT_1
                        or DialogAction.QUEST_REFUSE or DialogAction.QUEST_REFUSE_1
                        or DialogAction.QUEST_REFUSE_2 or DialogAction.QUEST_REFUSE_SIMPLE
                        => await SendQuestStartDialogAsync(env, conn, ct),
                    _ => false,
                };

            case QuestStatus.START:
                if (targetId != _endNpc) return false;

                return dialog switch
                {
                    DialogAction.QUEST_SELECT        => await SendQuestDialogAsync(conn, targetObjId, 10002, ct),
                    DialogAction.SELECT_QUEST_REWARD => await TryAdvanceToRewardAsync(conn, entry!, targetObjId, ct),
                    _ => false,
                };

            case QuestStatus.REWARD:
                if (targetId != _endNpc) return false;
                return await SendQuestEndDialogAsync(env, conn, ct);

            default:
                return false;
        }
    }

    private async ValueTask<bool> TryAdvanceToRewardAsync(GsClientConnection conn, QuestEntry entry, int targetObjId, CancellationToken ct)
    {
        if (!AllGroupsComplete(entry))
            return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);

        await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
        return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
    }

    private bool AllGroupsComplete(QuestEntry entry)
    {
        foreach (var group in _skillGroups)
            if (ReadGroupTotal(entry, group.VarNum, group.EndVar).Total < group.EndVar) return false;
        return true;
    }

    public override async ValueTask<bool> OnSkillUseAsync(Player player, int skillId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        foreach (var group in _skillGroups)
        {
            if (!group.SkillIds.Contains(skillId)) continue;

            var (currentTotal, varIdEnd) = ReadGroupTotal(entry, group.VarNum, group.EndVar);
            int total = currentTotal + 1;
            if (total > group.EndVar) continue; // this group already capped; check remaining groups

            for (int slot = group.VarNum; slot < varIdEnd; slot++)
            {
                entry.SetVar(slot, total & 0x3F);
                total >>= 6;
            }

            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }

        return false;
    }

    /// <summary>Identical packed 6-bit var-span decoding as <c>MonsterHuntHandler.ReadGroupTotal</c>.</summary>
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
