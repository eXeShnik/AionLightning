// Port of Java data/scripts/system/handlers/quest/sanctum/_1909ASongOfPraise.java (Mr. Poke / Nephis).
// Talk to Bercules (203739) to start; deliver at 203726 (var 0->1, gives item 182206001); turn in
// at 203099 (removes item 182206001, reward).
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

namespace Quest.Sanctum;

public sealed class _1909ASongOfPraise : QuestHandlerBase
{
    private const int QuestIdConst  = 1909;
    private const int StartNpc      = 203739;
    private const int DeliverNpc    = 203726;
    private const int RewardNpc     = 203099;
    private const int DeliveryItemId = 182206001;

    private readonly IItemDao _itemDao;

    public _1909ASongOfPraise(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DeliverNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RewardNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry  = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == DeliverNpc)
        {
            if (entry is { Status: QuestStatus.START } && entry.GetVar(0) == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: false, sameNpc: false,
                        giveItemId: DeliveryItemId, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == RewardNpc)
        {
            if (entry is null) return false;
            if (dialog == DialogAction.QUEST_SELECT && entry.Status == QuestStatus.START)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD && entry.Status is not (QuestStatus.COMPLETE or QuestStatus.NONE))
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2, reward: true, sameNpc: true,
                    giveItemId: 0, giveItemCount: 0, removeItemId: DeliveryItemId, removeItemCount: 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
