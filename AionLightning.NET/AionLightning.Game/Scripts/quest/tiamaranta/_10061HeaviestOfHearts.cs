// Port of Java data/scripts/system/handlers/quest/tiamaranta/_10061HeaviestOfHearts.java (Luzien).
// Campaign follow-up to 10060. Talk to Skafir (205886) to advance var 0->1 (SETPRO1); kill the three
// beasts (218826/218827/218828 - each tracked in its own var 1/2/3) to reach var 0=2; at Garnon
// (800019) SELECT_ACTION_1694 plays movie 751, SETPRO3 spawns the decoy Garnon (800081) plus the
// attacking wave and starts a 30s timer, advancing var 2->3; when the timer expires the quest flips
// to REWARD; turn in at Garnon.
// Skip vs Java: (1) onEnterWorldEvent auto-starts the quest via QuestService.startQuest - approximated
// here by starting only once the preceding campaign quest 10060 is COMPLETE, since StartMissionAsync
// does not run QuestService's level/race/class condition checks (same simplification family as
// DefaultOnLvlUpEventAsync). (2) the attacking-wave aggro (getAggroList().addHate) and the 30s
// despawn/respawn of the decoy vs real Garnon (World.despawn / NpcActions.delete / SpawnEngine) have no
// ported API - the mobs are still spawned but left un-aggroed and the swap is skipped (cosmetic; the
// 30s timer still completes the quest). (3) onDieEvent (no OnDie hook) is dropped - it only shortcuts
// the same timer completion.
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

namespace Quest.Tiamaranta;

public sealed class _10061HeaviestOfHearts : QuestHandlerBase
{
    private const int QuestIdConst = 10061;
    private const int SkafirNpc    = 205886;
    private const int GarnonNpc    = 800019;
    private const int DecoyGarnon  = 800081;
    private const int Beast1       = 218826;
    private const int Beast2       = 218827;
    private const int Beast3       = 218828;
    private const int PrecedingQuest = 10060;

    private static readonly int[] Beasts = { Beast1, Beast2, Beast3 };

    public _10061HeaviestOfHearts(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(SkafirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GarnonNpc).OnTalk.Add(QuestId);
        foreach (int beast in Beasts)
            engine.RegisterQuestNpc(beast).OnKill.Add(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, PrecedingQuest, isZoneMission: true, ct);

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is not null && entry.Status != QuestStatus.NONE) return false;
        // note: Java uses QuestService.startQuest (with condition checks); gated here on 10060 COMPLETE.
        if (player.Quests.Get(PrecedingQuest)?.Status != QuestStatus.COMPLETE) return false;
        return await StartMissionAsync(conn, player, QuestStatus.START, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == SkafirNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                // Java switch fallthrough: QUEST_SELECT (var != 0) falls into SETPRO1
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            else if (targetId == GarnonNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                // Java switch fallthrough: QUEST_SELECT (var != 2) falls into SELECT_ACTION_1694
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SELECT_ACTION_1694)
                {
                    await SendQuestDialogAsync(conn, targetObjId, 1694, ct);
                    await PlayQuestMovieAsync(conn, player, 751, ct);
                    return true;
                }
                if (dialog == DialogAction.SETPRO3)
                {
                    await CloseDialogWindowAsync(conn, targetObjId, ct);
                    var pos = player.Position;
                    SpawnQuestNpc(pos.WorldId, pos.InstanceId, DecoyGarnon, 2468f, 164f, 327f, 20);
                    StartQuestTimer(env, conn, 30);
                    SpawnAttackWave(pos.WorldId, pos.InstanceId);
                    await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                    // note: Java despawns the real Garnon (800019) here - no despawn API, skipped (cosmetic).
                    return true;
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == GarnonNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;

        int idx = System.Array.IndexOf(Beasts, env.TargetId);
        if (idx < 0) return false;

        int killedSoFar = entry.GetVar(1) + entry.GetVar(2) + entry.GetVar(3);
        if (killedSoFar == 2) // the two others are already down; this is the third
        {
            await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
            return true;
        }
        // Java defaultOnKillEvent(env, targetId, 0, 1, idx+1): bump this beast's own var 0->1
        if (entry.GetVar(idx + 1) == 0)
        {
            await ChangeQuestStepAsync(conn, entry, idx + 1, 1, toReward: false, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 3) return false;
        // note: Java deletes the decoy Garnon and respawns the real one here - no despawn/spawn-object API, skipped.
        await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: true, ct);
        return true;
    }

    private void SpawnAttackWave(int worldId, int instanceId)
    {
        // note: Java aggros each spawn onto the decoy Garnon (addHate) - no aggro API in scripts, dropped.
        SpawnQuestNpc(worldId, instanceId, 218825, 2463f, 156f, 327f, 0);
        SpawnQuestNpc(worldId, instanceId, 218765, 2451f, 157f, 323f, 0);
        SpawnQuestNpc(worldId, instanceId, 218825, 2445f, 167f, 322f, 0);
        SpawnQuestNpc(worldId, instanceId, 218825, 2446f, 178f, 320f, 0);
        SpawnQuestNpc(worldId, instanceId, 218825, 2450f, 191f, 320f, 0);
    }
}
