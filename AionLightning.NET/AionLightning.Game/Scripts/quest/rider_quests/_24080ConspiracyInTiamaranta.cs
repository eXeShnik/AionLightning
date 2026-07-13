// Port of Java data/scripts/system/handlers/quest/rider_quests/_24080ConspiracyInTiamaranta.java (pralinka).
// Zone-mission sub-quest of 24071: Skafir (205864, var0 0->1), Pashilion (205964, 1->2), kill a
// 233868 (var0 4->5), Hindla (205928, collect-item check 2->4, then SET... turn to REWARD at var0==5
// via SETPRO6); turn in back at Skafir.
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

namespace Quest.RiderQuests;

public sealed class _24080ConspiracyInTiamaranta : QuestHandlerBase
{
    private const int QuestIdConst = 24080;
    private const int SkafirNpc    = 205864;
    private const int PashilionNpc = 205964;
    private const int HindlaNpc    = 205928;
    private const int MobId        = 233868;

    private readonly IItemDao _itemDao;

    public _24080ConspiracyInTiamaranta(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(SkafirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PashilionNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HindlaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MobId).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 24071, isZoneMission: true, ct);

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, MobId, 4, 5, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        int var0        = entry.GetVar(0);
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == SkafirNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == PashilionNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == HindlaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.QUEST_SELECT && var0 == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 2, 4, reward: false, checkOkId: 10000, checkFailId: 10001, ct);
                if (dialog == DialogAction.SETPRO6) return await DefaultCloseDialogAsync(env, conn, 5, 5, reward: true, sameNpc: false, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == SkafirNpc) return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
