// Port of Java data/scripts/system/handlers/quest/sarpan/_20053TheProtectorsBlessing.java (vlog).
// Elyos mirror of sarpan's _10053Revelations: zone-mission-chain quest (no OnQuestStart npc -
// started by 20052's dialog/level-up chain): talk to Garnon (205987) var 0->1; entering world
// 300330000 spawns the Protector's Seal (730493) and flips 1->2; use the Seal at var 2 to flip
// 2->3, start a 180s quest timer, and spawn a random Klaw (one of 218760/218761/218762/218763);
// killing that Klaw (or re-entering the world while var is in [2,4)) respawns another; the timer
// flips 3->4, spawns Oriata (205795) and plays movie 708; hand in the quest_data.xml collect-items
// at Oriata (movie 710 first, at var 5) to flip to REWARD; turn in at Aimah (205617).
// Skip vs Java: qe.registerOnInvisibleTimerEnd/onInvisibleTimerEndEvent (a second 170s timer that
// rescue-spawns Hesibanata (218890) if the player is still stuck) and qe.registerOnDie/onDieEvent
// (resets var back to 1 on player death) have no matching hook in this port's QuestEngine (only a
// single onQuestTimerEnd timer and no onDie dispatch exist) - the quest remains completable via the
// kill-then-timer path without them, just without the death/stuck safety nets. Also skips Java's
// spawn.getAggroList().addHate(...) on the spawned Klaw - no AI2/aggro framework in this port.
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

namespace Quest.Sarpan;

public sealed class _20053TheProtectorsBlessing : QuestHandlerBase
{
    private const int QuestIdConst        = 20053;
    private const int GarnonNpc           = 205987;
    private const int ProtectorsSealNpc   = 730493;
    private const int OriataNpc           = 205795;
    private const int AimahNpc            = 205617;
    private const int RevelationsWorldId  = 300330000;
    private const float KlawSpawnZ        = 124.942f;

    private static readonly int[] Mobs = [218760, 218762, 218761, 218763];
    private static readonly (int NpcId, float X, float Y)[] KlawSpawnPoints =
    [
        (218760, 250.081f, 268.308f),
        (218762, 273.354f, 244.489f),
        (218761, 272.994f, 244.674f),
        (218763, 250.800f, 222.782f),
    ];

    private readonly IItemDao _itemDao;

    public _20053TheProtectorsBlessing(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GarnonNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ProtectorsSealNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(OriataNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AimahNpc).OnTalk.Add(QuestId);
        foreach (int mob in Mobs) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 20052, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == GarnonNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == ProtectorsSealNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 2)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                    StartQuestTimer(env, conn, 180);
                    SpawnRandomKlaw(player);
                    return true;
                }
                return false;
            }
            if (targetId == OriataNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.QUEST_SELECT && var == 5)
                {
                    await PlayQuestMovieAsync(conn, player, 710, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 5, 5, reward: true, checkOkId: 10000, checkFailId: 10001, ct);
                if (dialog == DialogAction.SETPRO5) return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                if (dialog == DialogAction.FINISH_DIALOG) return await CloseDialogWindowAsync(conn, targetObjId, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == AimahNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (player.Position.WorldId != RevelationsWorldId) return false;

        int var = entry.GetVar(0);
        if (var == 1)
        {
            SpawnQuestNpc(RevelationsWorldId, player.Position.InstanceId, ProtectorsSealNpc, 250.348f, 245.210f, 126.270f, 60);
            await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
            return true;
        }
        if (var is >= 2 and < 4)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
            return true;
        }
        if (var == 4 || var == 5)
        {
            SpawnQuestNpc(RevelationsWorldId, player.Position.InstanceId, OriataNpc, 241.348f, 245.879f, 125.475f, 56);
            return true;
        }
        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return ValueTask.FromResult(false);
        if (entry.GetVar(0) != 3 || !System.Array.Exists(Mobs, m => m == env.TargetId)) return ValueTask.FromResult(false);

        SpawnRandomKlaw(env.Player);
        return ValueTask.FromResult(true);
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 3) return false;

        SpawnQuestNpc(RevelationsWorldId, player.Position.InstanceId, OriataNpc, 241.348f, 245.879f, 125.475f, 56);
        await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
        await PlayQuestMovieAsync(conn, player, 708, ct);
        return true;
    }

    private void SpawnRandomKlaw(Player player)
    {
        var (npcId, x, y) = KlawSpawnPoints[System.Random.Shared.Next(KlawSpawnPoints.Length)];
        SpawnQuestNpc(RevelationsWorldId, player.Position.InstanceId, npcId, x, y, KlawSpawnZ, 0);
    }
}
