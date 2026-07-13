// Port of Java data/scripts/system/handlers/quest/argent_manor/_30459MasteroftheRing.java (Ritsu).
// Talk to the start npc (799546) to accept; at 204108: SETPRO1 advances var 0->1, then the
// collect-item check (CHECK_USER_HAS_QUEST_ITEM_SIMPLE) flips to REWARD (dialog 5); turn in at
// 204108.
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

namespace Quest.ArgentManor;

public sealed class _30459MasteroftheRing : QuestHandlerBase
{
    private const int QuestIdConst = 30459;
    private const int StartNpc     = 799546;
    private const int ReportNpc    = 204108;

    private readonly IItemDao _itemDao;

    public _30459MasteroftheRing(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ReportNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId != ReportNpc) return false;
            return dialog switch
            {
                DialogAction.QUEST_SELECT when var == 0             => await SendQuestDialogAsync(conn, targetObjId, 1352, ct),
                DialogAction.QUEST_SELECT when var == 1             => await SendQuestDialogAsync(conn, targetObjId, 2375, ct),
                DialogAction.SETPRO1 when var == 0                   => await DefaultCloseDialogAsync(env, conn, 0, 1, ct),
                DialogAction.CHECK_USER_HAS_QUEST_ITEM_SIMPLE        => await CheckQuestItemsAsync(env, conn, _itemDao, 1, 1, reward: true, checkOkId: 5, checkFailId: 0, ct),
                _                                                    => false,
            };
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != ReportNpc) return false;
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
