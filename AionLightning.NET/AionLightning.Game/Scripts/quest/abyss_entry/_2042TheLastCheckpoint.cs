// Port of Java data/scripts/system/handlers/quest/abyss_entry/_2042TheLastCheckpoint.java
// (Hellboy/aion4Free/Hilgert, reworked vlog). Asmodian counterpart of _1044: talk Aegir (204301) to
// accept (var0 0->1); Yornduf (204319) SETPRO2 starts a 150s timer and sends the player through 6 fly
// rings (var0 2..7), passing the last ring (rings[5]) sets var0=8 and ends the timer; then Yornduf
// SET_SUCCEED flips REWARD; report to Aegir. Timer expiry / death / world-change while mid-run rolls
// var0 to 9 (failed) so the player re-talks Yornduf (var==9 path) to retry.
// Java switch fallthroughs preserved via accumulated guarded ifs (see inline comments).
// Skips vs Java (state transitions preserved): QuestService.questTimerEnd active-cancel is a no-op here
//   (no timer-cancellation API); the timer callback still no-ops once var0 leaves its window.
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

namespace Quest.AbyssEntry;

public sealed class _2042TheLastCheckpoint : QuestHandlerBase
{
    private const int QuestIdConst = 2042;
    private const int Aegir   = 204301;
    private const int Yornduf = 204319;

    private static readonly string[] Rings =
    {
        "MORHEIM_ICE_FORTRESS_220020000_1", "MORHEIM_ICE_FORTRESS_220020000_2", "MORHEIM_ICE_FORTRESS_220020000_3",
        "MORHEIM_ICE_FORTRESS_220020000_4", "MORHEIM_ICE_FORTRESS_220020000_5", "MORHEIM_ICE_FORTRESS_220020000_6"
    };

    public _2042TheLastCheckpoint(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService) { }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(Aegir).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Yornduf).OnTalk.Add(QuestId);
        foreach (string ring in Rings)
            RegisterOnPassFlyingRing(engine, ring);
        engine.RegisterOnQuestTimerEnd(QuestId);
        RegisterOnDie(engine);
        engine.RegisterOnEnterWorld(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == Aegir)
            {
                // Java switch fallthrough: QUEST_SELECT (no break) -> SETPRO1.
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct); // var 0 -> 1
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await DefaultCloseDialogAsync(env, conn, 0, 0, ct);
                return false;
            }
            if (targetId == Yornduf)
            {
                // Java switch fallthroughs (no breaks): QUEST_SELECT -> SELECT_ACTION_1354 -> SETPRO2 -> SET_SUCCEED -> FINISH_DIALOG.
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 8) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (var == 9) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                }
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SELECT_ACTION_1354)
                {
                    if (var == 1 || var == 9)
                    {
                        await PlayQuestMovieAsync(conn, player, 89, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 1354, ct);
                    }
                }
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SELECT_ACTION_1354 || dialog == DialogAction.SETPRO2)
                {
                    if (var == 1)
                    {
                        StartQuestTimer(env, conn, 150);
                        return await DefaultCloseDialogAsync(env, conn, 1, 2, ct); // var 1 -> 2
                    }
                    if (var == 9)
                    {
                        StartQuestTimer(env, conn, 150);
                        return await DefaultCloseDialogAsync(env, conn, 9, 2, ct); // var 9 -> 2
                    }
                }
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SELECT_ACTION_1354
                    || dialog == DialogAction.SETPRO2 || dialog == DialogAction.SET_SUCCEED)
                {
                    if (var == 8)
                    {
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    }
                }
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SELECT_ACTION_1354
                    || dialog == DialogAction.SETPRO2 || dialog == DialogAction.SET_SUCCEED || dialog == DialogAction.FINISH_DIALOG)
                    return await DefaultCloseDialogAsync(env, conn, 9, 9, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == Aegir)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnPassFlyingRingAsync(QuestEnv env, string ringName, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (ringName == Rings[0]) { await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct); return true; }
        if (ringName == Rings[1]) { await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct); return true; }
        if (ringName == Rings[2]) { await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: false, ct); return true; }
        if (ringName == Rings[3]) { await ChangeQuestStepAsync(conn, entry, 0, 6, toReward: false, ct); return true; }
        if (ringName == Rings[4]) { await ChangeQuestStepAsync(conn, entry, 0, 7, toReward: false, ct); return true; }
        if (ringName == Rings[5])
        {
            await ChangeQuestStepAsync(conn, entry, 0, 8, toReward: false, ct);
            // note: Java QuestService.questTimerEnd active-cancel dropped; the timer callback no-ops once var0 == 8.
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var > 1 && var < 8)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 9, toReward: false, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var > 1 && var < 9)
        {
            // note: Java QuestService.questTimerEnd active-cancel dropped (fire-and-forget timer).
            return await OnQuestTimerEndAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var > 1 && var < 9)
        {
            // note: Java QuestService.questTimerEnd active-cancel dropped (fire-and-forget timer).
            return await OnQuestTimerEndAsync(env, conn, ct);
        }
        return false;
    }
}
