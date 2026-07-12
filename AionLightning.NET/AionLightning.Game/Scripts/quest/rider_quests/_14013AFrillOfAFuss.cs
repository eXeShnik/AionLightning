// Port of Java data/scripts/system/handlers/quest/rider_quests/_14013AFrillOfAFuss.java (pralinka).
// Zone-mission sub-quest of 14010: talk to the quest npc (203129, var0 0->1), kill 5 Rakec
// (210126, var index 1), 7 Girakec (210200/210201, var index 2), then 1 Trandila (210202) flips
// straight to REWARD; turn in at the same npc.
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

public sealed class _14013AFrillOfAFuss : QuestHandlerBase
{
    private const int QuestIdConst = 14013;
    private const int QuestNpc     = 203129;
    private static readonly int[] _rakec   = [210126];
    private static readonly int[] _girakec = [210200, 210201];
    private const int TrandilaNpc = 210202;

    public _14013AFrillOfAFuss(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(QuestNpc).OnTalk.Add(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int mob in _rakec) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        foreach (int mob in _girakec) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(TrandilaNpc).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14010, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || env.TargetId != QuestNpc) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return entry.GetVar(0) == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;

        int targetId = env.TargetId;
        if (targetId == 210126)
            return await BumpVarAsync(conn, entry, 1, 0, 5, ct);
        if (targetId == 210200 || targetId == 210201)
            return await BumpVarAsync(conn, entry, 2, 0, 7, ct);
        if (targetId == TrandilaNpc)
            return await DefaultOnKillEventAsync(env, conn, TrandilaNpc, 1, reward: true, ct);
        return false;
    }

    private async ValueTask<bool> BumpVarAsync(GsClientConnection conn, QuestEntry entry, int varNum, int startVar, int endVar, CancellationToken ct)
    {
        int var = entry.GetVar(varNum);
        if (var < startVar || var >= endVar) return false;
        await ChangeQuestStepAsync(conn, entry, varNum, var + 1, toReward: false, ct);
        return true;
    }
}
