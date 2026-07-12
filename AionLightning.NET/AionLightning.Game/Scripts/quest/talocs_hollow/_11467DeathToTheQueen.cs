// Port of Java data/scripts/system/handlers/quest/talocs_hollow/_11467DeathToTheQueen.java (Cheatkiller).
// Offered at 799527 (OnQuestStart, direct questId dispatch): accepting starts an 8min (480s) quest
// timer. Killing 215480 (the queen) at any point flips straight to REWARD, picking a reward tier
// from var 0's value at kill time (0/1/2/3 -> reward index 0/1/2/3). While running, the timer
// escalates var 0 twice more (0->1 at +240s, 1->2 at +240s) - matching Java exactly, there is no
// third re-arm (no var==2 branch in onQuestTimerEndEvent), so var 0 caps at 2 if the queen still
// isn't dead; the var==3 branch is dead code in Java (nothing ever sets var 0 past 2 via the timer)
// and is preserved here for fidelity. Turn in at 799503.
// Java quirk ported as-is: onKillEvent's changeQuestStep(env, var, var, true) writes `var` into
// quest-var-slot index `var` (not slot 0) - a no-op except when var==0. Since nothing else reads
// those other slots, this has no observable effect; the reward tier is still read from var 0
// (unchanged) in the REWARD branch below, so behavior matches Java's live server exactly.
// Skip vs Java: no OnDie/OnLogOut hook exists in this port, so onDieEvent (fails the quest back to
// NONE with a QUEST_FAILED_$1 message if the player dies while the timer is running) and
// onLogOutEvent (silently resets to NONE on disconnect) are both omitted - same simplification as
// eltnen._1033SatalocasHeart's onLogOutEvent skip. QuestService.questTimerEnd's cancel-on-kill also
// has no equivalent (fire-and-forget timer), but the timer callback still no-ops correctly once
// status is no longer START, same as reshanta._2758CarryTheFlame.
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

namespace Quest.TalocsHollow;

public sealed class _11467DeathToTheQueen : QuestHandlerBase
{
    private const int QuestIdConst = 11467;
    private const int StartNpc     = 799527;
    private const int TurnInNpc    = 799503;
    private const int QueenNpc     = 215480;

    public _11467DeathToTheQueen(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnQuestTimerEnd(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(QueenNpc).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if ((entry is null || entry.Status == QuestStatus.NONE) && targetId == StartNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_1)
                StartQuestTimer(env, conn, 480);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry is { Status: QuestStatus.REWARD } && targetId == TurnInNpc)
        {
            int var = entry.GetVar(0);
            int rewardIndex = var is 1 or 2 or 3 ? var : 0;

            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await FinishQuestAsync(conn, player, rewardIndex, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != QueenNpc) return false;

        int var = entry.GetVar(0);
        await ChangeQuestStepAsync(conn, entry, var, var, toReward: true, ct);
        return true;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var == 0)
        {
            StartQuestTimer(env, conn, 240);
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        }
        else if (var == 1)
        {
            StartQuestTimer(env, conn, 240);
            await ChangeQuestStepAsync(conn, entry, 1, 2, toReward: false, ct);
        }
        else if (var == 3)
        {
            await ChangeQuestStepAsync(conn, entry, 2, 3, toReward: false, ct);
        }
        return true;
    }
}
