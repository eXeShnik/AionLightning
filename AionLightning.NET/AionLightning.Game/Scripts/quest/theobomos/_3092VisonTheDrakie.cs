// Port of Java data/scripts/system/handlers/quest/theobomos/_3092VisonTheDrakie.java.
// Collect 25x Bloodwing Meat (182208066) and lure Vison (798214); take the meat to Tityus
// (798191), who checks/consumes the collect_items and finishes the quest.
// Java's onDialogEvent has a switch fallthrough at Vison: clicking QUEST_SELECT with fewer than
// 25 meat falls through into the SETPRO1 case body (no `break`/`return` on the failing branch),
// silently calling defaultCloseDialog(0,1) anyway. That's preserved faithfully here — it's a soft
// gate that only makes the check more lenient, not a dead/broken branch, so it doesn't block
// completion and isn't "fixed". Java's registerOnLogOut(questId) has no matching onLogOutEvent
// override in the source (and no onLogOut hook exists in this port) — a no-op registration,
// omitted.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Theobomos;

public sealed class _3092VisonTheDrakie : QuestHandlerBase
{
    private const int QuestIdConst = 3092;
    private const int TityusNpc    = 798191;
    private const int VisonNpc     = 798214;
    private const int MeatItemId   = 182208066;

    private readonly IItemDao _itemDao;

    public _3092VisonTheDrakie(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(TityusNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TityusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(VisonNpc).OnTalk.Add(QuestId);
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
            if (targetId != TityusNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == VisonNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (entry.GetVar(0) == 0)
                    {
                        long meat = player.Inventory.FindByItemId(MeatItemId)?.Count ?? 0;
                        if (meat >= 25)
                            return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    }
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == TityusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, reward: true, checkOkId: 5, checkFailId: 2716, ct);
                if (env.DialogId == (int)DialogAction.FINISH_DIALOG)
                    return await DefaultCloseDialogAsync(env, conn, 1, 1, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TityusNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
