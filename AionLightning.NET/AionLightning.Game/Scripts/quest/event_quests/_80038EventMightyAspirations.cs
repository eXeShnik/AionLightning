// Port of Java data/scripts/system/handlers/quest/event_quests/_80038EventMightyAspirations.java (npc 799780).
// note: onBonusApply(LUNAR) is registered + ported for parity but never fires yet — the event-bonus
//   system (BonusType/EventService bonus dispatch) is not ported. Kept faithful (unreachable until it lands).
// note: Java QuestService.startEventQuest can re-run a COMPLETE event quest; there is no event-restart
//   primitive in this port, so StartMissionAsync is used — it only starts the quest when the player has
//   no entry yet. Re-runs past first completion (the onLevelUp item-count restart) are not modelled.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.EventQuests;

public sealed class _80038EventMightyAspirations : QuestHandlerBase
{
    private const int QuestIdConst    = 80038;
    private const int Npc             = 799780;
    private const int RestartItem     = 164002017; // Siel's Gift
    private const int RestartMinCount = 4;         // Java: count > 4

    private readonly IItemDao _itemDao;

    public _80038EventMightyAspirations(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Npc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Npc).OnTalk.Add(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        RegisterOnBonusApply(engine, "LUNAR");
    }

    public override async ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (env.QuestId != QuestId) return false;
        long count = env.Player.Inventory.FindByItemId(RestartItem)?.Count ?? 0;
        if (count > RestartMinCount)
            await StartMissionAsync(conn, env.Player, QuestStatus.START, ct);
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.NONE) return false;

        if (entry.Status == QuestStatus.START || (entry.Status == QuestStatus.COMPLETE && CollectItemCheck(player)))
        {
            if (env.TargetId == Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.QUEST_ACCEPT_1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 0, true, 5, 2716, ct);
                if (dialog == DialogAction.SELECTED_QUEST_NOREWARD)
                    return await SendQuestRewardDialogAsync(env, conn, 5, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return await SendQuestRewardDialogAsync(env, conn, 0, ct);
    }

    public override ValueTask<HandlerResult> OnBonusApplyAsync(QuestEnv env, string bonusType, GsClientConnection conn, CancellationToken ct)
    {
        if (bonusType != "LUNAR" || env.QuestId != QuestId)
            return ValueTask.FromResult(HandlerResult.Unknown);

        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not null && (entry.Status == QuestStatus.START || entry.Status == QuestStatus.COMPLETE) && entry.GetVar(0) == 0)
            return ValueTask.FromResult(HandlerResult.Success);
        return ValueTask.FromResult(HandlerResult.Failed);
    }

    // Java QuestService.collectItemCheck(env, false): non-consuming check that the player holds every
    // <collect_item> quest_data.xml lists for this quest.
    private bool CollectItemCheck(Player player)
    {
        var items = Template?.CollectItems?.Items;
        if (items is not { Count: > 0 }) return false;
        foreach (var req in items)
        {
            var held = player.Inventory.FindByItemId(req.ItemId);
            if (held is null || held.Count < req.Count) return false;
        }
        return true;
    }

    // Java sendQuestRewardDialog(env, 799780, reportDialogId): a REWARD-status turn-in shows
    // reportDialogId on USE_OBJECT (when non-zero), else finishes the quest.
    private async ValueTask<bool> SendQuestRewardDialogAsync(QuestEnv env, GsClientConnection conn, int reportDialogId, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not { Status: QuestStatus.REWARD } || env.TargetId != Npc) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;
        if (reportDialogId != 0 && DialogActionLookup.FromId(env.DialogId) == DialogAction.USE_OBJECT)
            return await SendQuestDialogAsync(conn, targetObjId, reportDialogId, ct);
        return await SendQuestEndDialogAsync(env, conn, ct);
    }
}
