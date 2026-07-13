// Port of Java data/scripts/system/handlers/quest/altgard/_2288PutYourMoneyWhereYourMouthIs.java
// (Atomics, reworked Gigi). Talk to the wager npc (203621) to start and arm a 600s farming timer;
// kill any of 7 registered mobs to advance var 1->4 (the timer reverts var to 0 if it expires
// mid-farm); turn in at the same npc.
using System.Linq;
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

namespace Quest.Altgard;

public sealed class _2288PutYourMoneyWhereYourMouthIs : QuestHandlerBase
{
    private const int QuestIdConst = 2288;
    private const int WagerNpc     = 203621;

    private static readonly int[] _mobs = [210564, 210584, 210581, 201047, 210436, 210437, 210440];

    public _2288PutYourMoneyWhereYourMouthIs(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(WagerNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(WagerNpc).OnTalk.Add(QuestId);
        foreach (int mob in _mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (!_mobs.Contains(env.TargetId)) return false;

        int var = entry.GetVar(0);
        if (var <= 0 || var > 3) return false;

        await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
        // Java also calls QuestService.questTimerEnd(env) once var reaches 4 (farming done) - no
        // timer-cancel API in this port; harmless, since OnQuestTimerEndAsync below only reverts
        // while var is in (0,3), which no longer holds once var == 4.
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (env.TargetId != WagerNpc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (entry.GetVar(0) == 4)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                }
                if (entry.GetVar(0) == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            if (dialog == DialogAction.SETPRO1)
            {
                // Java bug: qs.setQuestVarById(0, 1) here skips updateQuestStatus, dropping the
                // broadcast/persist (same class of bug fixed via ChangeQuestStepAsync as in
                // _1430ATeleportationExperiment). Ported with the fix applied.
                StartQuestTimer(env, conn, 600);
                await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var <= 0 || var >= 3) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: false, ct);
        return true;
    }
}
