// Port of Java data/scripts/system/handlers/quest/tiamaranta/_10064TheOldestOfFoes.java (Luzien).
// Elyos instance (world 300400000): talk 800018 -> SETPRO1 enters the instance (var 0->1); on entering
// the instance world at var 1, decorative NPCs (800022 + 800032/800033/800034) spawn; talk 800022 ->
// SETPRO2 plays movie 752 (var 1->2); movie 752 end spawns 13x enemy legioners (800037); clearing all
// 800037 spawns 6x 800031 + 800021 (var 2->3); talk 800021 -> SETPRO3/4 spawns 7 drakans (var 3->4);
// clearing all drakans spawns boss 218823 (var 4->5); killing 218823 plays movie 753 and flips REWARD;
// turn in at 205886. Leaving the instance world (or dying) while in progress reverts the step.
// Unblocked by EnterInstanceAsync (Java InstanceService triad) + AnyNpcAlive (Java getNpcsAlive) +
// OnDieAsync (Java onDieEvent).
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Tiamaranta;

public sealed class _10064TheOldestOfFoes : QuestHandlerBase
{
    private const int QuestIdConst = 10064;
    private const int InstanceWorld = 300400000;
    private const int Mob     = 800037;
    private const int Drakan1 = 218773;
    private const int Drakan2 = 218775;
    private const int Drakan3 = 218774;
    private const int Boss    = 218823;

    public _10064TheOldestOfFoes(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        RegisterOnDie(engine);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterQuestNpc(Mob).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Drakan1).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Drakan2).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Drakan3).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Boss).OnKill.Add(QuestId);
        engine.RegisterOnQuestMovieEnd(752, QuestId);
        engine.RegisterOnQuestMovieEnd(753, QuestId);
        engine.RegisterQuestNpc(800018).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(800021).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(800022).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(205886).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

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
            if (targetId == 800018)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                // Java switch fallthrough: QUEST_SELECT (var != 0) falls into SETPRO1.
                if (dialog == DialogAction.SETPRO1 || dialog == DialogAction.QUEST_SELECT)
                {
                    await EnterInstanceAsync(player, conn, InstanceWorld, 433.27f, 685.31f, 183.4f, 10, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
            }
            else if (targetId == 800022)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                // Java switch fallthrough: QUEST_SELECT (var != 1) falls into SETPRO2.
                if (dialog == DialogAction.SETPRO2 || dialog == DialogAction.QUEST_SELECT)
                {
                    await PlayQuestMovieAsync(conn, player, 752, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
            }
            else if (targetId == 800021)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 3)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                // Java switch fallthrough: QUEST_SELECT (var != 3) falls into SETPRO3 -> SETPRO4.
                if (dialog == DialogAction.SETPRO3 || dialog == DialogAction.SETPRO4 || dialog == DialogAction.QUEST_SELECT)
                {
                    SpawnDrakans(player);
                    await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == 205886)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var      = entry.GetVar(0);
        int targetId = env.TargetId;
        int instanceId = player.Position.InstanceId;

        if (var == 2 && targetId == Mob)
        {
            if (!AnyNpcAlive(player.Position.WorldId, instanceId, Mob))
            {
                SpawnQuestNpc(InstanceWorld, instanceId, 800031, 550.057f, 666.941f, 183.301f, 40);
                SpawnQuestNpc(InstanceWorld, instanceId, 800031, 542.284f, 662.375f, 183.301f, 40);
                SpawnQuestNpc(InstanceWorld, instanceId, 800031, 540.240f, 665.980f, 183.301f, 40);
                SpawnQuestNpc(InstanceWorld, instanceId, 800031, 548.020f, 670.540f, 183.301f, 40);
                SpawnQuestNpc(InstanceWorld, instanceId, 800031, 538.181f, 669.566f, 183.301f, 40);
                SpawnQuestNpc(InstanceWorld, instanceId, 800031, 545.943f, 674.044f, 183.301f, 40);
                SpawnQuestNpc(InstanceWorld, instanceId, 800021, 540.155f, 675.154f, 183.301f, 40);
                return await DefaultOnKillEventAsync(env, conn, targetId, 2, 3, ct);
            }
        }
        else if (var == 4 && (targetId == Drakan1 || targetId == Drakan2 || targetId == Drakan3))
        {
            if (!AnyNpcAlive(player.Position.WorldId, instanceId, Drakan1, Drakan2, Drakan3))
            {
                SpawnQuestNpc(InstanceWorld, instanceId, Boss, 527.996f, 700.251f, 178.393f, 120);
                return await DefaultOnKillEventAsync(env, conn, targetId, 4, 5, ct);
            }
        }
        else if (var == 5 && targetId == Boss)
        {
            await PlayQuestMovieAsync(conn, player, 753, ct);
            return await DefaultOnKillEventAsync(env, conn, Boss, 5, reward: true, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        if (entry.Status == QuestStatus.START && movieId == 752 && entry.GetVar(0) == 2)
        {
            SpawnLegioners(player);
            return true;
        }
        if (entry.Status == QuestStatus.REWARD && movieId == 753)
        {
            // note: Java teleports out to reward world 600030000 (BEAM) here — non-entry relocation, dropped; return true kept.
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        int instanceId = player.Position.InstanceId;

        if (player.Position.WorldId == InstanceWorld)
        {
            if (var == 1)
            {
                SpawnQuestNpc(InstanceWorld, instanceId, 800022, 514.004f, 718.839f, 178.393f, 0);
                SpawnQuestNpc(InstanceWorld, instanceId, 800033, 504.51f, 728.05f, 178.393f, 0);
                SpawnQuestNpc(InstanceWorld, instanceId, 800032, 500.42f, 735.26f, 178.393f, 0);
                SpawnQuestNpc(InstanceWorld, instanceId, 800033, 496.2f, 742.43f, 178.393f, 0);
                SpawnQuestNpc(InstanceWorld, instanceId, 800032, 500.91f, 727.45f, 178.393f, 0);
                SpawnQuestNpc(InstanceWorld, instanceId, 800033, 495.33f, 727.25f, 178.393f, 0);
                SpawnQuestNpc(InstanceWorld, instanceId, 800033, 491.35f, 734.78f, 178.393f, 0);
                SpawnQuestNpc(InstanceWorld, instanceId, 800034, 511.76f, 732.18f, 178.393f, 0);
                SpawnQuestNpc(InstanceWorld, instanceId, 800033, 507.68f, 739.07f, 178.393f, 0);
                SpawnQuestNpc(InstanceWorld, instanceId, 800032, 503.49f, 746.21f, 178.393f, 0);
                SpawnQuestNpc(InstanceWorld, instanceId, 800034, 514.64f, 735.77f, 178.393f, 0);
                SpawnQuestNpc(InstanceWorld, instanceId, 800034, 511.53f, 741.44f, 178.393f, 0);
                SpawnQuestNpc(InstanceWorld, instanceId, 800033, 516.96f, 739.95f, 178.393f, 0);
                SpawnQuestNpc(InstanceWorld, instanceId, 800034, 513.035f, 747.17f, 178.393f, 0);
                return true;
            }
        }
        else
        {
            if (var >= 1)
            {
                entry.SetVar(0, 0);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (entry.GetVar(0) >= 1)
        {
            entry.SetVar(0, 0);
            await UpdateQuestStatusAsync(conn, entry, ct);
            // note: Java sends SM_SYSTEM_MESSAGE QUEST_FAILED_$1 here — cosmetic notice, dropped.
            return true;
        }
        return false;
    }

    private void SpawnDrakans(Player player)
    {
        int instanceId = player.Position.InstanceId;
        // note: Java sets each drakan's target + aggro to the player (setTarget/addHate) — no aggro API, dropped (AI cosmetic).
        SpawnQuestNpc(InstanceWorld, instanceId, Drakan1, 550.057f, 666.941f, 183.301f, 40);
        SpawnQuestNpc(InstanceWorld, instanceId, Drakan1, 542.284f, 662.375f, 183.301f, 40);
        SpawnQuestNpc(InstanceWorld, instanceId, Drakan1, 540.240f, 665.980f, 183.301f, 40);
        SpawnQuestNpc(InstanceWorld, instanceId, Drakan2, 548.020f, 670.540f, 183.301f, 40);
        SpawnQuestNpc(InstanceWorld, instanceId, Drakan2, 538.181f, 669.566f, 183.301f, 40);
        SpawnQuestNpc(InstanceWorld, instanceId, Drakan3, 545.943f, 674.044f, 183.301f, 40);
        SpawnQuestNpc(InstanceWorld, instanceId, Drakan3, 540.155f, 675.154f, 183.301f, 40);
    }

    private void SpawnLegioners(Player player)
    {
        int instanceId = player.Position.InstanceId;
        // note: Java relocates each spawn to (504.99,737,178), sets WALKING AI + moveToPoint + START_EMOTE2
        //       broadcast — movement/AI/emotion cosmetics, dropped; the mobs are still spawned to be cleared.
        SpawnQuestNpc(InstanceWorld, instanceId, Mob, 437.11f, 679.46f, 183.3f, 10);
        SpawnQuestNpc(InstanceWorld, instanceId, Mob, 434.35f, 676.1f, 183.3f, 10);
        SpawnQuestNpc(InstanceWorld, instanceId, Mob, 431.36f, 672.6f, 183.3f, 10);
        SpawnQuestNpc(InstanceWorld, instanceId, Mob, 434.98f, 683.7f, 183.3f, 10);
        SpawnQuestNpc(InstanceWorld, instanceId, Mob, 431.6f, 681f, 183.3f, 10);
        SpawnQuestNpc(InstanceWorld, instanceId, Mob, 428.7f, 677.26f, 183.3f, 10);
        SpawnQuestNpc(InstanceWorld, instanceId, Mob, 430.1f, 683.9f, 183.3f, 10);
        SpawnQuestNpc(InstanceWorld, instanceId, Mob, 432.53f, 687.9f, 183.3f, 10);
        SpawnQuestNpc(InstanceWorld, instanceId, Mob, 426.15f, 682.9f, 183.3f, 10);
        SpawnQuestNpc(InstanceWorld, instanceId, Mob, 429.917f, 692.7f, 183.3f, 10);
        SpawnQuestNpc(InstanceWorld, instanceId, Mob, 427.48f, 688.82f, 183.3f, 10);
        SpawnQuestNpc(InstanceWorld, instanceId, Mob, 423f, 688f, 183.3f, 10);
        SpawnQuestNpc(InstanceWorld, instanceId, Mob, 421.8f, 690.4f, 183.3f, 10);
    }
}
