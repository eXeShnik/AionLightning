// Port of Java data/scripts/system/handlers/quest/abyss_entry/_1922DeliveronYourPromises.java
// (Hellboy/aion4Free/Gigi/vlog). Talk Fuchsia (203830), then Epeios (203764) whose SETPRO3 creates a
// Triniel Underground Arena instance (310080000) and teleports the player in (276, 293, 163, h90),
// advancing var0 4->5; inside, killing the arena spirits (213580/213581/213582) fills sub-counter
// var4 1..10 (movie 166 + arena cleanup on the 10th), then movie 166 end / Epeios SETPRO4 sets var0=7;
// use Telemachus (203901) to flip REWARD; turn in at Telemachus. Movie 165 (entry) starts a 240s
// timer; timeout / leaving the arena world rolls var0 back to 4.
// Java switch fallthroughs preserved via guarded ifs (see inline comments).
// Skips vs Java (cosmetic / unported, state transitions preserved):
//   - the two TeleportService2 relocations (timer-end + movie 166 end, both to 110010000) are
//     non-entry cosmetic teleports - dropped with notes, the var/status steps are kept.
//   - the "delete every NPC in the arena instance" cleanup on the final kill (NpcActions.delete over
//     WorldMapInstance.getNpcs) has no quest-reachable instance-NPC enumeration helper - dropped.
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

public sealed class _1922DeliveronYourPromises : QuestHandlerBase
{
    private const int QuestIdConst = 1922;
    private const int Fuchsia    = 203830;
    private const int Telemachus = 203901;
    private const int Epeios     = 203764;
    private const int ArenaWorld = 310080000;
    private static readonly int[] Mobs = { 213580, 213581, 213582 };

    // Shared per-handler reward-path selection, faithful to Java's singleton `int choice` field.
    private int _choice;

    public _1922DeliveronYourPromises(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterQuestNpc(Fuchsia).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Telemachus).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Epeios).OnTalk.Add(QuestId);
        foreach (int mob in Mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        RegisterOnEnterZone(engine, "SANCTUM_UNDERGROUND_ARENA_310080000");
        engine.RegisterOnQuestMovieEnd(165, QuestId);
        engine.RegisterOnQuestMovieEnd(166, QuestId);
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
            if (targetId == Fuchsia)
            {
                // Java switch fallthrough: QUEST_SELECT (no break) -> SETPRO12; SETPRO12 returns.
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 4) return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO12)
                {
                    _choice = 1;
                    return await DefaultCloseDialogAsync(env, conn, 0, 4, ct); // var 0 -> 4
                }
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                return false;
            }
            if (targetId == Telemachus)
            {
                // Java switch fallthrough: USE_OBJECT (no break) -> SELECT_QUEST_REWARD.
                if (dialog == DialogAction.USE_OBJECT && var == 7)
                    return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
                if ((dialog == DialogAction.USE_OBJECT || dialog == DialogAction.SELECT_QUEST_REWARD) && var == 7)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 6, ct);
                }
                return false;
            }
            if (targetId == Epeios)
            {
                // Java switch fallthrough: QUEST_SELECT (no break) -> SETPRO3.
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (entry.GetVar(4) == 10) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                }
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO3)
                {
                    await EnterInstanceAsync(player, conn, ArenaWorld, 276, 293, 163, 90, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: false, ct); // var 4 -> 5
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                if (dialog == DialogAction.SETPRO4)
                {
                    entry.SetVar(0, 7); // Java qs.setQuestVar(7)
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await DefaultCloseDialogAsync(env, conn, 7, 7, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == Telemachus)
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
            // note: Java questTimerEnd + NpcActions.delete over the whole arena instance dropped - no
            // quest-reachable instance-NPC enumeration; the timer callback no-ops once var4 == 10.
            await PlayQuestMovieAsync(conn, player, 166, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(4) >= 10) return false;

        entry.SetVar(0, 4); // Java qs.setQuestVar(4)
        await UpdateQuestStatusAsync(conn, entry, ct);
        // note: Java TeleportService2.teleportTo(110010000, ...) relocation dropped.
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
            await PlayQuestMovieAsync(conn, player, 165, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId == 165)
        {
            StartQuestTimer(env, conn, 240);
            return true;
        }
        if (movieId == 166)
        {
            // note: Java TeleportService2.teleportTo(110010000, ...) relocation dropped.
            return true;
        }
        return false;
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 1921, ct);

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
