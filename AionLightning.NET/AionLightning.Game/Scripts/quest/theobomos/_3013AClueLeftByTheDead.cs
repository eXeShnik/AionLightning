// Port of Java data/scripts/system/handlers/quest/theobomos/_3013AClueLeftByTheDead.java.
// Talk to Metatron (798132), who requires holding the clue item (182208008, sourced from a
// side-quest drop declared in quest_data.xml) before offering the quest; confirming consumes it and
// advances var 0->1; report to Kubold (798146) to flip to REWARD; turn in at Kubold.
// Skip vs Java: Metatron's QUEST_SELECT case falls through (no break) into
// CHECK_USER_HAS_QUEST_ITEM when var != 0 - omitted as a dead/unintended path (var only ever
// leaves 0 once, at which point the player has already moved on to Kubold).
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Theobomos;

public sealed class _3013AClueLeftByTheDead : QuestHandlerBase
{
    private const int QuestIdConst = 3013;
    private const int MetatronNpc  = 798132;
    private const int KuboldNpc    = 798146;
    private const int ClueItemId   = 182208008;

    private readonly IItemDao _itemDao;

    public _3013AClueLeftByTheDead(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(MetatronNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(MetatronNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KuboldNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != MetatronNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == MetatronNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                {
                    long itemCount = player.Inventory.FindByItemId(ClueItemId)?.Count ?? 0;
                    return itemCount >= 1
                        ? await SendQuestDialogAsync(conn, targetObjId, 1011, ct)
                        : await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, ClueItemId, 1, ct);
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
            }
            else if (targetId == KuboldNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                {
                    // Java bug: original calls updateQuestStatus() before setStatus(REWARD), so the
                    // broadcast packet still reports START. Status is flipped first here instead.
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == KuboldNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
