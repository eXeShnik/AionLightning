// Port of Java data/scripts/system/handlers/quest/abyss_entry/_1044TestingFlightSkills.java
// (Hellboy/aion4Free/Hilgert/vlog/Antraxx, mod apozema). Talk Telemachus (203901) to accept (var0 0->1);
// Daedalus (203930) SETPRO2 starts a 90s timer and sends the player through 6 fly rings (var0 2..7),
// passing the last ring (rings[5]) sets var0=9 and ends the timer; then Daedalus SET_SUCCEED flips REWARD;
// turn in at Telemachus. Timer expiry / death / world-change while mid-run (var 2..6) rolls var0 to 8
// (failed) so the player must re-talk Daedalus (var==8 path) to retry.
// Java switch fallthroughs preserved via accumulated guarded ifs (see inline comments).
// Skips vs Java (state transitions preserved): QuestService.questTimerEnd active-cancel is a no-op here
//   (no timer-cancellation API - StartQuestTimer is fire-and-forget); the timer callback still no-ops
//   once var0 leaves the 2..6 window, matching Java's guard.
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

public sealed class _1044TestingFlightSkills : QuestHandlerBase
{
    private const int QuestIdConst = 1044;
    private const int Telemachus = 203901;
    private const int Daedalus   = 203930;

    private static readonly string[] Rings =
    {
        "ELTNEN_FORTRESS_210020000_1", "ELTNEN_FORTRESS_210020000_2", "ELTNEN_FORTRESS_210020000_3",
        "ELTNEN_FORTRESS_210020000_4", "ELTNEN_FORTRESS_210020000_5", "ELTNEN_FORTRESS_210020000_6"
    };

    public _1044TestingFlightSkills(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService) { }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(Telemachus).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Daedalus).OnTalk.Add(QuestId);
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
            if (targetId == Telemachus)
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
            if (targetId == Daedalus)
            {
                // Java switch fallthroughs (no breaks): QUEST_SELECT -> SELECT_ACTION_1354 -> SETPRO2 -> SET_SUCCEED -> FINISH_DIALOG.
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 8) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                    if (var == 9) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                }
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SELECT_ACTION_1354)
                {
                    if (var == 1 || var == 8)
                    {
                        await PlayQuestMovieAsync(conn, player, 40, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 1354, ct);
                    }
                }
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SELECT_ACTION_1354 || dialog == DialogAction.SETPRO2)
                {
                    if (var == 1)
                    {
                        StartQuestTimer(env, conn, 90);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await DefaultCloseDialogAsync(env, conn, 1, 2, ct); // var 1 -> 2
                    }
                    if (var == 8)
                    {
                        StartQuestTimer(env, conn, 90);
                        return await DefaultCloseDialogAsync(env, conn, 8, 2, ct); // var 8 -> 2
                    }
                }
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SELECT_ACTION_1354
                    || dialog == DialogAction.SETPRO2 || dialog == DialogAction.SET_SUCCEED)
                {
                    if (var == 9)
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

        if (entry.Status == QuestStatus.REWARD && targetId == Telemachus)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 1922, ct);

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
            await ChangeQuestStepAsync(conn, entry, 0, 9, toReward: false, ct);
            // note: Java QuestService.questTimerEnd active-cancel dropped; the timer callback no-ops once var0 == 9.
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var > 1 && var < 7)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 8, toReward: false, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var > 1 && var < 7)
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
        if (var > 1 && var < 7)
        {
            // note: Java QuestService.questTimerEnd active-cancel dropped (fire-and-forget timer).
            return await OnQuestTimerEndAsync(env, conn, ct);
        }
        return false;
    }
}
