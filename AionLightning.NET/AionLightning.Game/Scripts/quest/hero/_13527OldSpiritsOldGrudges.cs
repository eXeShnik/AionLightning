// Port of Java data/scripts/system/handlers/quest/hero/_13527OldSpiritsOldGrudges.java.
// Accept at 801948 (QUEST_ACCEPT/QUEST_ACCEPT_SIMPLE starts a 1800s quest timer and the quest
// itself, no confirm dialog); kill 233302/233303/233304/233305 once each to set vars 1-4; a
// SET_SUCCEED trigger (any target) while START flips to REWARD; turn in at 801541.
//
// Java bug: the killEvent switch was missing a `break` after case 233304, so killing 233304 also
// fell through into case 233305's block and set var 4 to 1 immediately (single kill satisfying two
// slots). Ported here as four independent, non-fallthrough kill checks (one var each) instead.
// Java bug: onQuestTimerEndEvent's abandon guard used `&&` between the four "var != 1" checks, so
// it would only abandon a quest where *none* of the four kills had landed. Fixed to `||` (abandon
// unless all four are complete), matching a kill-4-things-within-the-timer objective's evident
// intent.
// Skip vs Java: the kill handler's "all four vars are 1 -> QuestService.questTimerEnd(env)" early
// cancellation of the pending timer task is dropped — no timer-cancellation API is exposed in this
// port (same limitation as quest.sarpan._21506This_End_Up), so the timer simply runs to expiry;
// OnQuestTimerEndAsync below no-ops in that case since the (fixed) guard condition is false.
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

namespace Quest.Hero;

public sealed class _13527OldSpiritsOldGrudges : QuestHandlerBase
{
    private const int QuestIdConst  = 13527;
    private const int StartNpc      = 801948;
    private const int EndNpc        = 801541;
    private const int Mob1          = 233302;
    private const int Mob2          = 233303;
    private const int Mob3          = 233304;
    private const int Mob4          = 233305;
    private const int TimerSeconds  = 1800;

    public _13527OldSpiritsOldGrudges(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(EndNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Mob1).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Mob2).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Mob3).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Mob4).OnKill.Add(QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (dialog is DialogAction.QUEST_ACCEPT_SIMPLE or DialogAction.QUEST_ACCEPT)
            {
                StartQuestTimer(env, conn, TimerSeconds);
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == StartNpc && dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);

            if (dialog == DialogAction.SET_SUCCEED)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == EndNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int varIdx = env.TargetId switch
        {
            Mob1 => 1,
            Mob2 => 2,
            Mob3 => 3,
            Mob4 => 4,
            _ => 0,
        };
        if (varIdx == 0) return false;

        if (entry.GetVar(varIdx) < 1)
            await ChangeQuestStepAsync(conn, entry, varIdx, 1, toReward: false, ct);

        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (entry.GetVar(1) != 1 || entry.GetVar(2) != 1 || entry.GetVar(3) != 1 || entry.GetVar(4) != 1)
        {
            entry.Status = QuestStatus.NONE;
            entry.Step = 0;
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }
}
