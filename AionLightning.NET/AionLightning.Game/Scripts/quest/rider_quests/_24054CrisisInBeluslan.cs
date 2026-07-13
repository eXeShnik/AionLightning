// Port of Java data/scripts/system/handlers/quest/rider_quests/_24054CrisisInBeluslan.java (pralinka).
// Zone-mission sub-quest of 24040: Nerita (204702, var0 0->1), Fafner (802053, var0 1->2), kill 3
// of npc 702041 (var0 2->5), kill 1 of npc 233865 (var0 5->6), turn in at Nerita (SET_SUCCEED ->
// REWARD); close out at Vidar (204052).
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

public sealed class _24054CrisisInBeluslan : QuestHandlerBase
{
    private const int QuestIdConst = 24054;
    private const int NeritaNpc = 204702;
    private const int FafnerNpc = 802053;
    private const int VidarNpc  = 204052;
    private const int MobA = 702041;
    private const int MobB = 233865;

    public _24054CrisisInBeluslan(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in new[] { NeritaNpc, FafnerNpc, VidarNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MobA).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(MobB).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 24040, isZoneMission: true, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int targetId = env.TargetId;
        if (targetId == MobA) return await DefaultOnKillEventAsync(env, conn, MobA, 2, 5, ct);
        if (targetId == MobB) return await DefaultOnKillEventAsync(env, conn, MobB, 5, 6, ct);
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        int var0        = entry.GetVar(0);
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != VidarNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == NeritaNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var0 == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_SELECT && var0 == 6) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            if (dialog == DialogAction.SET_SUCCEED) return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: false, ct);
            return false;
        }
        if (targetId == FafnerNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var0 == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            return false;
        }
        return false;
    }
}
