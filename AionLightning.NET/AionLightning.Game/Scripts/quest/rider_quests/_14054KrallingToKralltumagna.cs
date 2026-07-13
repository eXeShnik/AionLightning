// Port of Java data/scripts/system/handlers/quest/rider_quests/_14054KrallingToKralltumagna.java (pralinka).
// Zone-mission sub-quest of 14050: talk to StartNpc (204500, var0 0->1), talk to WaypointNpc
// (800413, var0 1->2), talk to KrallNpc (802050, var0 2->3), then kill the two mob groups (11 named
// "grunt" ids advancing var1 0->6, and SwarmNpc 702040 advancing var2 0->3 - both independent of
// var0 and of each other, matching the Java source exactly), then kill BossNpc (233861) while
// var0==3 to advance var0 3->4, and finally KrallNpc's SETPRO6 flips to REWARD; turn in at StartNpc.
// Skip vs Java: TeleportService2.teleportTo (two call sites moving the player to Kralltumagna
// coordinates) is omitted - no TeleportService equivalent in this port (same simplification as
// eltnen._1430ATeleportationExperiment); the var/status transitions are kept so the quest stays
// completable without the physical move. Note: the Java register() mob list includes npc 214161,
// but onKillEvent's grunt-id check never tests for it (a pre-existing list mismatch in the source,
// not a control-flow bug) - ported as-is; 214161 is still registered for parity but has no effect.
using System;
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

namespace Quest.RiderQuests;

public sealed class _14054KrallingToKralltumagna : QuestHandlerBase
{
    private const int QuestIdConst = 14054;
    private const int StartNpc     = 204500;
    private const int WaypointNpc  = 800413;
    private const int KrallNpc     = 802050;
    private const int SwarmNpc     = 702040;
    private const int BossNpc      = 233861;

    private static readonly int[] _registeredMobs =
    [
        BossNpc, SwarmNpc, 214009, 214010, 214081, 214160, 214161, 214018, 214020, 214021, 214087, 214088, 214022, 214023
    ];

    private static readonly int[] _gruntMobs =
    [
        214009, 214010, 214022, 214023, 214020, 214021, 214087, 214088, 214160, 214018, 214081
    ];

    public _14054KrallingToKralltumagna(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(WaypointNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KrallNpc).OnTalk.Add(QuestId);
        foreach (int mob in _registeredMobs) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, 14050, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14050, isZoneMission: true, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int targetId = env.TargetId;
        if (Array.IndexOf(_gruntMobs, targetId) >= 0)
        {
            int var1 = entry.GetVar(1);
            if (var1 > 5) return false;
            await ChangeQuestStepAsync(conn, entry, 1, var1 + 1, toReward: false, ct);
            return true;
        }
        if (targetId == SwarmNpc)
        {
            int var2 = entry.GetVar(2);
            if (var2 > 2) return false;
            await ChangeQuestStepAsync(conn, entry, 2, var2 + 1, toReward: false, ct);
            return true;
        }
        if (targetId == BossNpc)
        {
            if (entry.GetVar(0) != 3) return false;
            await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.REWARD)
            return targetId == StartNpc && await SendQuestEndDialogAsync(env, conn, ct);
        if (entry.Status != QuestStatus.START) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int var = entry.GetVar(0);

        if (targetId == StartNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1 && var == 0)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (targetId == WaypointNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return var == 1 && await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2 && var == 1)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
                return true;
            }
            return false;
        }

        if (targetId == KrallNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO4 && var == 2)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            if (dialog == DialogAction.SETPRO6)
                return await DefaultCloseDialogAsync(env, conn, 4, 4, reward: true, sameNpc: false, ct);
            return false;
        }
        return false;
    }
}
