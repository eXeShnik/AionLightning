using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Templates.Quest;
using AionLightning.Game.Model.Templates.Quest.Script;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.QuestEngine.Handlers.Templates;

/// <summary>
/// Data-driven "report to N NPCs in sequence" quest handler (Java
/// <c>questEngine.handlers.template.ReportToMany</c> port) — covers &lt;report_to_many&gt; entries.
/// Each &lt;npc_infos&gt; child pins one NPC to a specific step (<c>var</c>); the player must visit
/// them in ascending order before an end NPC accepts the final turn-in.
/// </summary>
/// <remarks>
/// Not ported in this phase: the single entry using <c>start_item_id</c> as an alternate start
/// trigger (Java's onItemUseEvent path — <see cref="Network.Aion.ClientPackets.CM_USE_ITEM"/> has
/// no generic "fire a quest dialog event" hook, only hardcoded per-item-type dispatch), and the
/// per-step "movie" follow-up dialog (no SM_MOVIE-equivalent packet exists yet — same gap already
/// noted for MonsterHuntScriptEntry/ItemCollectingScriptEntry's unused Movie fields). Both are
/// parsed for data completeness and logged (see <see cref="DataHolders.QuestScriptData"/>) but
/// otherwise behave as if absent.
/// </remarks>
public sealed class ReportToManyHandler : QuestHandlerBase
{
    private readonly int _startItemId;
    private readonly HashSet<int> _startNpcs;
    private readonly HashSet<int> _endNpcs;
    private readonly Dictionary<int, ReportToManyNpcInfo> _npcInfoByNpc;
    private readonly int _startDialog;
    private readonly int _endDialog;
    private readonly int _maxVar;
    private readonly IItemDao _itemDao;

    public ReportToManyHandler(ReportToManyScriptEntry data, IDataManager dataManager,
        IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(data.Id, dataManager, questDao, rewardService)
    {
        _startItemId = data.StartItemId;
        _startNpcs   = data.StartNpcIds;
        _endNpcs     = data.EndNpcIds;
        _startDialog = data.StartDialogId != 0 ? data.StartDialogId : 1011;
        _endDialog   = data.EndDialogId   != 0 ? data.EndDialogId   : 2375;
        _maxVar      = data.MaxVar;
        _itemDao     = itemDao;

        _npcInfoByNpc = new Dictionary<int, ReportToManyNpcInfo>();
        foreach (var info in data.NpcInfos)
            _npcInfoByNpc[info.NpcId] = info; // last-wins on duplicate npc id (Java FastMap.put parity)
    }

    public override void Register(QuestEngine engine)
    {
        foreach (int npcId in _startNpcs)
        {
            var npc = engine.RegisterQuestNpc(npcId);
            npc.OnQuestStart.Add(QuestId);
            npc.OnTalk.Add(QuestId);
        }

        foreach (int npcId in _npcInfoByNpc.Keys)
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);

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
                    DialogAction.QUEST_SELECT => await SendQuestDialogAsync(conn, targetObjId, _startDialog, ct),
                    _ => await SendQuestStartDialogAsync(env, conn, ct),
                };

            case QuestStatus.START:
                return await HandleStartAsync(env, conn, entry!, template, targetId, targetObjId, dialog, ct);

            case QuestStatus.REWARD:
                if (!_endNpcs.Contains(targetId)) return false;

                if (dialog == DialogAction.USE_OBJECT
                    && _npcInfoByNpc.TryGetValue(targetId, out var rewardNpcInfo) && rewardNpcInfo.QuestDialog != 0)
                    return await SendQuestDialogAsync(conn, targetObjId, rewardNpcInfo.QuestDialog, ct);

                return await SendQuestEndDialogAsync(env, conn, ct);

            default:
                return false;
        }
    }

    /// <summary>
    /// Advances the report-to sequence one step at a time (Java's onDialogEvent START branch).
    /// While <c>var &lt;= maxVar</c> the player is still mid-sequence; once every step-npc has been
    /// visited (<c>var &gt; maxVar</c>), only the end npc(s) can finish the quest.
    /// </summary>
    private async ValueTask<bool> HandleStartAsync(QuestEnv env, GsClientConnection conn, QuestEntry entry,
        QuestTemplate template, int targetId, int targetObjId, DialogAction dialog, CancellationToken ct)
    {
        int var = entry.GetVar(0);

        if (var <= _maxVar)
        {
            if (!_npcInfoByNpc.TryGetValue(targetId, out var npcInfo) || npcInfo.Var != var) return false;

            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, npcInfo.QuestDialog, ct);

            int closeDialog = npcInfo.CloseDialog != 0 ? npcInfo.CloseDialog : 10000 + npcInfo.Var;
            if (env.DialogId != closeDialog) return false; // movie-continuation dialog id not ported — see remarks

            bool needsItemCheck = dialog is DialogAction.CHECK_USER_HAS_QUEST_ITEM or DialogAction.CHECK_USER_HAS_QUEST_ITEM_SIMPLE;
            if (needsItemCheck && !QuestService.IsRewardReady(entry, template, env.Player))
                return await SendQuestDialogAsync(conn, targetObjId, 10, ct);

            if (var == _maxVar)
            {
                entry.Status = QuestStatus.REWARD;
                if (closeDialog is 1009 or 20002 or 34)
                {
                    // Java returns here without persisting (sendQuestDialog(env, 5) directly) —
                    // always persisting the REWARD transition first avoids losing it on relog/crash.
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
            }
            else
            {
                entry.SetVar(0, var + 1);
            }

            await UpdateQuestStatusAsync(conn, entry, ct);
            return await SendQuestDialogAsync(conn, targetObjId, 10, ct);
        }

        if (!_endNpcs.Contains(targetId)) return false;

        return dialog switch
        {
            DialogAction.QUEST_SELECT        => await SendQuestDialogAsync(conn, targetObjId, _endDialog, ct),
            DialogAction.SELECT_QUEST_REWARD => await TryCompleteAsync(env, conn, entry, ct),
            _ => false,
        };
    }

    /// <summary>Consumes the optional start item (if any) and transitions straight to REWARD + grant in one click.</summary>
    private async ValueTask<bool> TryCompleteAsync(QuestEnv env, GsClientConnection conn, QuestEntry entry, CancellationToken ct)
    {
        if (_startItemId != 0)
        {
            var item = env.Player.Inventory.FindByItemId(_startItemId);
            if (item is null || item.Count < 1) return false;
            await RemoveOneAsync(env.Player, item, conn, ct);
        }

        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return await SendQuestEndDialogAsync(env, conn, ct);
    }

    private async ValueTask RemoveOneAsync(Player player, Item item, GsClientConnection conn, CancellationToken ct)
    {
        item.Count -= 1;
        if (item.Count <= 0)
        {
            player.Inventory.Remove(item.UniqueId);
            await _itemDao.DeleteAsync(item.UniqueId, ct);
            await conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
        }
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
    }
}
