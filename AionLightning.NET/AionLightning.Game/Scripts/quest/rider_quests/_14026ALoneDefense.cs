// Port of Java data/scripts/system/handlers/quest/rider_quests/_14026ALoneDefense.java (pralinka).
// Zone-mission sub-quest of 14021-14025: talk to TelemachusNpc (203901, var0 0->1), talk to
// MabangtahNpc (204020, var0 1->2, gives item 182215324), talk to the defend-target KrotanNpc
// (204044, var0 2->3, starts a 180s quest timer + spawns one random mob from {211628,211630,213575}
// nearby), kill the spawned mob (respawns another while var0==3), timer expiry advances var0 3->4,
// talking to KrotanNpc again (SETPRO4) flips to REWARD; turn in at TelemachusNpc.
// Skip vs Java: TeleportService2.teleportTo (three call sites moving the player between Eltnen
// coordinates) is omitted - no TeleportService equivalent in this port (same simplification as
// eltnen._1430ATeleportationExperiment); the var/status transitions are kept so the quest stays
// completable without the physical move. The defend-mob's AI (AbstractAI.setStateIfNot(WALKING),
// MoveController.moveToTargetObject, SM_EMOTION broadcast making it walk toward and attack Krotan)
// is also omitted - no NPC AI/move-controller infra in this port (Batch 0.2 SpawnQuestNpc
// convention: spawn only, no movement/aggro). No OnDie/OnLogOut hook exists in this port, so
// onDieEvent/onLogOutEvent (both punitive safety nets reverting var0 3->2 if the player dies or
// disconnects mid-timer) are omitted - same simplification as eltnen._1033SatalocasHeart's
// onLogOutEvent skip.
// Java bug: onDialogEvent's switches on TelemachusNpc (203901), MabangtahNpc (204020) and KrotanNpc
// (204044) had no breaks after their QUEST_SELECT cases, so talking with a var0 outside
// QUEST_SELECT's guarded values fell through into SETPRO1/SETPRO2/SETPRO3's bodies - which have no
// var guard of their own - and force-advanced the quest var / started the timer+mob spawn
// regardless of the actual dialog id sent. Fixed here so those state changes only fire on their
// own actual dialog id.
using System;
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

namespace Quest.RiderQuests;

public sealed class _14026ALoneDefense : QuestHandlerBase
{
    private const int QuestIdConst     = 14026;
    private const int TelemachusNpc    = 203901;
    private const int MabangtahNpc     = 204020;
    private const int KrotanNpc        = 204044;
    private const int GivenItem        = 182215324;
    private const int RewardRemoveItem = 182201013;
    private const int SpawnWorldId     = 310040000;
    private const float SpawnZ         = 217.48f;
    private const byte SpawnHeading    = 95;

    private static readonly int[]   _mobIds = [211628, 211630, 213575];
    private static readonly float[] _mobX   = [254.74f, 257.92f, 261.86f];
    private static readonly float[] _mobY   = [236.72f, 237.39f, 237.5f];

    private static readonly int[] _precedingQuestIds = [14021, 14022, 14023, 14024, 14025];

    private readonly IItemDao _itemDao;

    public _14026ALoneDefense(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
        foreach (int npc in new[] { TelemachusNpc, MabangtahNpc, KrotanNpc, 700141 })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        foreach (int mob in _mobIds) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, _precedingQuestIds, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, _precedingQuestIds, isZoneMission: true, ct);

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 3) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 3) return false;
        if (Array.IndexOf(_mobIds, env.TargetId) < 0) return false;

        SpawnDefenseMob(env.Player);
        return true;
    }

    private void SpawnDefenseMob(Player player)
    {
        int idx = Random.Shared.Next(_mobIds.Length);
        SpawnQuestNpc(SpawnWorldId, player.Position.InstanceId, _mobIds[idx], _mobX[idx], _mobY[idx], SpawnZ, SpawnHeading);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (env.TargetId == TelemachusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (env.TargetId == MabangtahNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 1 && await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
                    await GiveQuestItemAsync(player, conn, _itemDao, GivenItem, 1, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (env.TargetId == KrotanNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO3)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                    StartQuestTimer(env, conn, 180);
                    SpawnDefenseMob(player);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                if (dialog == DialogAction.SETPRO4)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return true;
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (env.TargetId != TelemachusNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, RewardRemoveItem, 1, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
