// Port of Java data/scripts/system/handlers/quest/rider_quests/_14080TeeteringInTiamaranta.java (pralinka).
// Zone-mission sub-quest of 14071: talk to Ancanus (205842, var0 0->1), Pashilion (205964, var0
// 1->2), collect-check at Flavia (205921, var0 2->4), kill npc 233868 (var0 4->5), turn in dialog at
// Flavia flips straight to REWARD, turn in at Ancanus.
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

public sealed class _14080TeeteringInTiamaranta : QuestHandlerBase
{
    private const int QuestIdConst = 14080;
    private const int AncanusNpc   = 205842;
    private const int PashilionNpc = 205964;
    private const int FlaviaNpc    = 205921;
    private const int KillMobNpc   = 233868;

    private readonly IItemDao _itemDao;

    public _14080TeeteringInTiamaranta(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(AncanusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PashilionNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FlaviaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KillMobNpc).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 14071, isZoneMission: true, ct);

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, KillMobNpc, 4, 5, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == AncanusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == PashilionNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 1 && await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == FlaviaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    return false;
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 2, 4, reward: false, checkOkId: 10000, checkFailId: 10001, ct);
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, 5, 5, reward: true, sameNpc: false, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == AncanusNpc) return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
