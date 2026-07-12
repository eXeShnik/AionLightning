// Port of Java data/scripts/system/handlers/quest/rider_quests/_14023PlayingAroundAtTheTemple.java (pralinka).
// Zone-mission sub-quest of 14020: talk to 203965 (var0 0->1), report to 203967 (var0 1->2), a
// collect-item check flips straight to REWARD; turn in at 203965.
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

public sealed class _14023PlayingAroundAtTheTemple : QuestHandlerBase
{
    private const int QuestIdConst = 14023;
    private const int Npc203965 = 203965;
    private const int Npc203967 = 203967;

    private readonly IItemDao _itemDao;

    public _14023PlayingAroundAtTheTemple(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(Npc203965).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc203967).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14020, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (env.TargetId == Npc203965)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (env.TargetId == Npc203967)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    return false;
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    // Java's collect check shows no dialog on failure; checkFailId 0 (close) approximates that.
                    return var == 2 && await CheckQuestItemsAsync(env, conn, _itemDao, 2, 2, reward: true, checkOkId: 10, checkFailId: 0, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (env.TargetId == Npc203965)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
