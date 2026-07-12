// Port of Java data/scripts/system/handlers/quest/rider_quests/_14044ShardsOfMemory.java (pralinka).
// Zone-mission sub-quest of 14040: talk to 278501 (var0 0->1), 790001 (var0 1->2), use object 700355
// (var0 3, reward flip via UseQuestObjectAsync's step==nextStep==3/reward semantics), report to
// 279029 (var0 2->3, movie 271) and again for the turn-in (REWARD).
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

public sealed class _14044ShardsOfMemory : QuestHandlerBase
{
    private const int QuestIdConst = 14044;
    private const int Npc279029 = 279029;
    private const int Npc278501 = 278501;
    private const int Npc790001 = 790001;
    private const int UseObject700355 = 700355;

    public _14044ShardsOfMemory(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(Npc279029).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc278501).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc790001).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(UseObject700355).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14040, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (env.TargetId == Npc278501)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (env.TargetId == Npc279029)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 2 && await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    if (var != 2) return false;
                    await PlayQuestMovieAsync(conn, player, 271, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (env.TargetId == UseObject700355)
                return await UseQuestObjectAsync(env, conn, 3, 3, reward: true, dieObject: false, ct);

            if (env.TargetId == Npc790001)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 1 && await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (env.TargetId == Npc279029)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }
        return false;
    }
}
