// Port of Java data/scripts/system/handlers/quest/morheim/_2041HoldTheFrontLine.java (vlog).
// Zone-mission chain quest (auto-started via OnLevelUpAsync/OnZoneMissionEndAsync once 2300 is
// COMPLETE). Talk Aegir (204301, var 0->1), Taisan (204403, var 1->2), Kargate (204432, var 2->3)
// spawns the first defense wave and starts a 240s quest timer; killing Crusaders/Draconute Scouts
// escalates through four waves (var-slots 1-4, each capped at 4), the last kill of wave 4 flips
// var 0 to 4 and cancels the timer; Kargate's SETPRO4 (var 4) flips to REWARD. Turn in at Aegir.
// Java bug fixed: onKillEvent's wave-1 guard was `if (var1 >= 0 || var1 < 3)`, which is true for
// every possible int (the OR of two complementary-ish ranges) - this made var1 increment forever
// and made every later wave-2/3/4 branch permanently unreachable. Fixed to `var1 < 3` (var1 is
// already guaranteed >= 0), matching the clear intent of the surrounding wave cascade.
// Java quirk preserved as-is: the outer kill-event switch only has case labels for 211624
// (Crusader) and 280818 (Draconute Scout) - killing a Chandala Scaleguard (213578) or Chandala
// Fangblade (213579) never reaches the wave-tracking logic at all, even though later waves spawn
// them too, so only Crusader/Scout kills ever count. This is the real, shipped behavior of the
// Java source (not a one-line typo like the fix above), so it is kept unchanged.
// Skip vs Java: mob.getAggroList().addHate(player, 1) has no equivalent - SpawnQuestNpc doesn't
// return the spawned Npc reference, so spawned waves don't auto-aggro the player (they still exist
// and can be killed normally). onDieEvent (reverts var 0 from 3 to 2 and cancels the timer if the
// player dies mid-wave) has no OnDieAsync hook in this port. Kargate's SETPRO4 teleport to
// 220020000 (TeleportService2) isn't ported - the var/status transition is kept (same precedent as
// ascension._1007ACeremonyinSanctum). QuestService.questTimerEnd's cancel-on-success/kill has no
// equivalent (fire-and-forget timer), but the timer callback still no-ops once var 0 != 3.
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

namespace Quest.Morheim;

public sealed class _2041HoldTheFrontLine : QuestHandlerBase
{
    private const int QuestIdConst = 2041;
    private const int AegirNpc      = 204301;
    private const int TaisanNpc     = 204403;
    private const int KargateNpc    = 204432;
    private const int ScoutNpc      = 280818;
    private const int CrusaderNpc   = 211624;
    private const int ScaleguardNpc = 213578;
    private const int FangbladeNpc  = 213579;
    private const int SpawnWorldId  = 320040000;
    private const float SpawnX = 254.21326f;
    private const float SpawnY = 256.9302f;
    private const float SpawnZ = 226.6418f;
    private const byte SpawnHeading = 93;
    private const int TimerSeconds = 240;

    public _2041HoldTheFrontLine(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int mob in new[] { ScoutNpc, CrusaderNpc, ScaleguardNpc, FangbladeNpc })
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
        foreach (int npc in new[] { AegirNpc, TaisanNpc, KargateNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 2300, isZoneMission: true, ct);

    private void SpawnWave(int instanceId, params int[] npcIds)
    {
        foreach (int npcId in npcIds)
            SpawnQuestNpc(SpawnWorldId, instanceId, npcId, SpawnX, SpawnY, SpawnZ, SpawnHeading);
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 3) return false;

        int targetId = env.TargetId;
        if (targetId != CrusaderNpc && targetId != ScoutNpc) return false;

        int var1 = entry.GetVar(1);
        int var2 = entry.GetVar(2);
        int var3 = entry.GetVar(3);
        int var4 = entry.GetVar(4);
        int instanceId = player.Position.InstanceId;

        if (var1 < 3)
        {
            entry.SetVar(1, var1 + 1);
            return true;
        }
        if (var1 == 3)
        {
            entry.SetVar(1, 4);
            SpawnWave(instanceId, ScoutNpc, ScoutNpc, CrusaderNpc, ScaleguardNpc);
            return true;
        }
        if (var1 == 4 && var2 < 3)
        {
            entry.SetVar(2, var2 + 1);
            return true;
        }
        if (var1 == 4 && var2 == 3)
        {
            entry.SetVar(2, 4);
            SpawnWave(instanceId, ScoutNpc, CrusaderNpc, ScaleguardNpc);
            return true;
        }
        if (var1 == 4 && var2 == 4 && var3 < 3)
        {
            entry.SetVar(3, var3 + 1);
            return true;
        }
        if (var1 == 4 && var2 == 4 && var3 == 3)
        {
            entry.SetVar(3, 4);
            SpawnWave(instanceId, ScoutNpc, CrusaderNpc, ScaleguardNpc, FangbladeNpc);
            return true;
        }
        if (var1 == 4 && var2 == 4 && var3 == 4 && var4 < 4)
        {
            entry.SetVar(4, var4 + 1);
            return true;
        }
        if (var1 == 4 && var2 == 4 && var3 == 4 && var4 == 4)
        {
            entry.SetVar(0, 4);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 3) return false;

        entry.SetVar(0, 4);
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD && targetId == AegirNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == AegirNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == TaisanNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 1 && await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }

            if (targetId == KargateNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO3)
                {
                    if (var != 2) return false;
                    SpawnWave(player.Position.InstanceId, CrusaderNpc, CrusaderNpc, ScoutNpc, ScoutNpc);
                    StartQuestTimer(env, conn, TimerSeconds);
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                }
                if (dialog == DialogAction.SETPRO4)
                {
                    if (var != 4) return false;
                    return await DefaultCloseDialogAsync(env, conn, 4, 4, reward: true, sameNpc: false, ct);
                }
                return false;
            }
            return false;
        }

        return false;
    }
}
