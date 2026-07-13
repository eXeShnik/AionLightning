// Port of Java data/scripts/system/handlers/quest/tiamaranta/_20060TheFourHearts.java (zhkchi/vlog).
// Asmodian counterpart of 10061. Talk to Skafir (205864) to advance var 0->1 (SETPRO1); kill the three
// beasts (218829/218830/218831 - each tracked in its own var 1/2/3) to reach var 0=2; at Garnon
// (800020) SELECT_ACTION_1694 plays movie 754, SETPRO3 spawns the decoy Garnon (800081) plus the
// attacking wave and starts a 30s timer, advancing var 2->3; when the timer expires the quest flips
// to REWARD; turn in at Garnon.
// Skip vs Java: (1) onLvlUpEvent's fan-out that re-runs onEnterZoneMissionEnd for {20061..20065} is
// dropped - those dependent zone-missions start via their own level-up/zone triggers, and this port
// has no in-script engine dispatch handle; the level-up still starts 20060 itself. (2) the attacking
// wave aggro (addHate) and the 30s decoy/real Garnon despawn-swap (World.despawn / NpcActions.delete /
// SpawnEngine) have no ported API - mobs are spawned un-aggroed and the swap is skipped (cosmetic; the
// timer still completes). (3) onDieEvent (no OnDie hook) is dropped - it only shortcuts timer completion.
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

public sealed class _20060TheFourHearts : QuestHandlerBase
{
    private const int QuestIdConst = 20060;
    private const int SkafirNpc    = 205864;
    private const int GarnonNpc    = 800020;
    private const int DecoyGarnon  = 800081;
    private const int Beast1       = 218829;
    private const int Beast2       = 218830;
    private const int Beast3       = 218831;

    private static readonly int[] Beasts = { Beast1, Beast2, Beast3 };

    public _20060TheFourHearts(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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
        engine.RegisterOnQuestTimerEnd(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        // note: Java also fans out onEnterZoneMissionEnd to {20061..20065} - dropped (they start via own triggers).
        => DefaultOnLvlUpEventAsync(env, conn, ct);

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
                    await PlayQuestMovieAsync(conn, player, 754, ct);
                    return true;
                }
                if (dialog == DialogAction.SETPRO3)
                {
                    await CloseDialogWindowAsync(conn, targetObjId, ct);
                    var pos = player.Position;
                    SpawnQuestNpc(pos.WorldId, pos.InstanceId, DecoyGarnon, 442.279f, 464.349f, 341.520f, 20);
                    StartQuestTimer(env, conn, 30);
                    SpawnAttackWave(pos.WorldId, pos.InstanceId);
                    await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                    // note: Java despawns the real Garnon (800020) here - no despawn API, skipped (cosmetic).
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
        SpawnQuestNpc(worldId, instanceId, 218825, 454.845f, 471.380f, 341.728f, 0);
        SpawnQuestNpc(worldId, instanceId, 218765, 455.157f, 470.027f, 341.647f, 0);
        SpawnQuestNpc(worldId, instanceId, 218825, 454.616f, 470.859f, 341.647f, 0);
        SpawnQuestNpc(worldId, instanceId, 218825, 454.415f, 471.770f, 341.686f, 0);
        SpawnQuestNpc(worldId, instanceId, 218825, 454.273f, 470.475f, 341.568f, 0);
    }
}
