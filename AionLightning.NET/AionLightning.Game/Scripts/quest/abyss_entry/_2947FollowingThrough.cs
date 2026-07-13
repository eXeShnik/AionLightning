// Port of Java data/scripts/system/handlers/quest/abyss_entry/_2947FollowingThrough.java
// (Hellboy/aion4Free/Gigi/vlog/FrozenKiller). Asmodian mirror of _1922. Talk Kvasir (204053), then
// Garm (204089) whose SETPRO3 creates a Triniel Underground Arena instance (320090000) and teleports
// the player in (276, 294, 163, h90), advancing var0 4->5; inside, killing the arena spirits fills
// sub-counter var4 1..10 (movie 168 on the 10th), then Garm SETPRO4 flips to REWARD (var0=9); turn in
// at Aegir (204301). Movie 167 (entry) starts a 240s timer; timeout / leaving the arena world rolls
// var0 back to 4.
// Java switch fallthroughs preserved via guarded ifs (see inline comments).
// Skips vs Java (cosmetic / unported, state transitions preserved):
//   - the two TeleportService2 relocations (timer-end + movie 168 end, both to 120010000) are
//     non-entry cosmetic teleports - dropped with notes, the var/status steps are kept.
//   - QuestService.questTimerEnd's active-cancel is a fire-and-forget no-op here; the timer callback
//     still no-ops once var0 != 5 / var4 == 10.
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.AbyssEntry;

public sealed class _2947FollowingThrough : QuestHandlerBase
{
    private const int QuestIdConst = 2947;
    private const int Kvasir     = 204053;
    private const int Aegir      = 204301;
    private const int Garm       = 204089;
    private const int ArenaWorld = 320090000;
    private static readonly int[] Mobs = { 213583, 290048, 211987, 290047, 290050, 211986, 290049, 213584, 211982 };

    // Shared per-handler reward-path selection, faithful to Java's singleton `int choice` field.
    private int _choice;

    public _2947FollowingThrough(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterOnQuestMovieEnd(168, QuestId);
        engine.RegisterOnQuestMovieEnd(167, QuestId);
        engine.RegisterQuestNpc(Kvasir).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Aegir).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Garm).OnTalk.Add(QuestId);
        foreach (int mob in Mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == Kvasir)
            {
                // Java switch fallthrough cascade: QUEST_SELECT -> SETPRO12 -> FINISH_DIALOG (no breaks).
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 1019, ct);
                }
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO12)
                {
                    _choice = 1;
                    if (var == 0) return await DefaultCloseDialogAsync(env, conn, 0, 4, ct);
                    if (var == 4) return await DefaultCloseDialogAsync(env, conn, 4, 4, ct);
                }
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO12 || dialog == DialogAction.FINISH_DIALOG)
                {
                    if (var == 0) return await DefaultCloseDialogAsync(env, conn, 0, 0, ct);
                }
                return false;
            }
            if (targetId == Aegir)
                return false; // Java: START Aegir only had SELECT_QUEST_REWARD -> false (no-op).
            if (targetId == Garm)
            {
                // Java switch fallthrough: QUEST_SELECT (no break) -> SETPRO3.
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (entry.GetVar(4) == 10) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                }
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO3)
                {
                    await EnterInstanceAsync(player, conn, ArenaWorld, 276, 294, 163, 90, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: false, ct); // var 4 -> 5
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                if (dialog == DialogAction.SETPRO4)
                {
                    entry.Status = QuestStatus.REWARD; // Java qs.setStatus(REWARD)
                    entry.SetVar(0, 9);                // Java qs.setQuestVar(9)
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await DefaultCloseDialogAsync(env, conn, 9, 9, reward: true, sameNpc: false, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == Aegir)
            return await SendQuestEndDialogWithRewardAsync(env, conn, _choice, ct);

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 5) return false;
        if (!Mobs.Contains(env.TargetId)) return false;

        int var4 = entry.GetVar(4);
        if (var4 < 9)
        {
            entry.SetVar(4, var4 + 1); // defaultOnKillEvent(mobs, 0, 9, var4)
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        if (var4 == 9)
        {
            entry.SetVar(4, 10); // defaultOnKillEvent(mobs, 9, 10, var4)
            await UpdateQuestStatusAsync(conn, entry, ct);
            // note: Java questTimerEnd cancel dropped (fire-and-forget timer); callback no-ops once var4 == 10.
            await PlayQuestMovieAsync(conn, player, 168, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(4) == 10) return false;

        entry.SetVar(0, 4); // Java qs.setQuestVar(4)
        await UpdateQuestStatusAsync(conn, entry, ct);
        // note: Java TeleportService2.teleportTo(120010000, ...) relocation dropped.
        return true;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (entry.GetVar(0) == 5 && entry.GetVar(4) != 10)
        {
            if (player.Position.WorldId != ArenaWorld)
            {
                // note: Java questTimerEnd cancel dropped (fire-and-forget timer).
                entry.SetVar(0, 4); // Java qs.setQuestVar(4)
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            await PlayQuestMovieAsync(conn, player, 167, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (movieId == 168)
        {
            // note: Java TeleportService2.teleportTo(120010000, ...) relocation dropped.
            return true;
        }
        if (movieId == 167)
        {
            StartQuestTimer(env, conn, 240);
            return true;
        }
        return false;
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 2946, ct);

    /// <summary>Java QuestHandler.sendQuestEndDialog(env, reward): SELECT_QUEST_REWARD/USE_OBJECT
    /// shows the tier-confirm page 5+reward; SELECTED_QUEST_REWARDn/NOREWARD completes with that tier.</summary>
    private async ValueTask<bool> SendQuestEndDialogWithRewardAsync(QuestEnv env, GsClientConnection conn, int rewardIndex, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.REWARD) return false;
        int targetObjId = env.Target?.ObjectId ?? 0;
        int dialogId = env.DialogId;

        if (dialogId >= (int)DialogAction.SELECTED_QUEST_REWARD1 && dialogId <= (int)DialogAction.SELECTED_QUEST_NOREWARD)
        {
            if (!await FinishQuestAsync(conn, env.Player, rewardIndex, ct)) return false;
            await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
            return true;
        }
        if (dialogId == (int)DialogAction.SELECT_QUEST_REWARD || dialogId == (int)DialogAction.USE_OBJECT)
            return await SendQuestDialogAsync(conn, targetObjId, 5 + rewardIndex, ct);
        return false;
    }
}
