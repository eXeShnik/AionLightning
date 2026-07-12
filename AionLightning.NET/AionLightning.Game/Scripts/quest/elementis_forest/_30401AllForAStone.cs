// Port of Java data/scripts/system/handlers/quest/elementis_forest/_30401AllForAStone.java (zhkchi).
// Talk to 799535 to start; the item-collect check (var/var, reward, give 2716) can be attempted
// directly at 799535; separately, 799582's SETPRO1 advances var 0->1. Turn in at 799535.
// Skip vs Java: qs.canRepeat() (no repeat modeling yet - treated as a one-time quest, same
// precedent as _2106VanarsFlattery).
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

namespace Quest.ElementisForest;

public sealed class _30401AllForAStone : QuestHandlerBase
{
    private const int QuestIdConst = 30401;
    private const int StoneNpc     = 799535;
    private const int OtherNpc     = 799582;

    private readonly IItemDao _itemDao;

    public _30401AllForAStone(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StoneNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StoneNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(OtherNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StoneNpc) return false;
            return dialog switch
            {
                DialogAction.QUEST_SELECT       => await SendQuestDialogAsync(conn, targetObjId, 1011, ct),
                DialogAction.SELECT_ACTION_1012 => await SendQuestDialogAsync(conn, targetObjId, 1012, ct),
                _                                => await SendQuestStartDialogAsync(env, conn, ct),
            };
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == StoneNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                        return await CheckQuestItemsAsync(env, conn, _itemDao, var, var, true, 5, 2716, ct);
                    case DialogAction.CHECK_USER_HAS_QUEST_ITEM_SIMPLE:
                        return await CheckQuestItemsAsync(env, conn, _itemDao, var, var, true, 5, checkFailId: 0, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }
            if (targetId == OtherNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.SELECT_ACTION_1353:
                        return await SendQuestDialogAsync(conn, targetObjId, 1353, ct);
                    case DialogAction.SETPRO1:
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StoneNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
