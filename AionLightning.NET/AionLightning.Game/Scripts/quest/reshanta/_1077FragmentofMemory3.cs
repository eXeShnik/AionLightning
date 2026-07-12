// Port of Java data/scripts/system/handlers/quest/reshanta/_1077FragmentofMemory3.java.
// Talk to Boreas (203704, var 0->1), Gaix (798154, var 1->2), Finn (204574, var 2->3), Acestes
// (204652, var 3->4, flight-teleport departure), Peitho (204653, var 4->5, movie 421 + flight
// departure), kill a 214599 (var 5->6, movie 422), turn in at Acestes (var 6 -> REWARD), then close
// out at Yuditio (278500). Zone-mission chain, level-up gated on 1701.
// Java bugs fixed: both Acestes' and Peitho's dialog switches sent a "not ready" hint dialog
// (10009 / 10010) on QUEST_SELECT when the var didn't match yet, but had no return/break after it -
// execution fell through into the next case body and fired the flight-teleport-departure side
// effects (dialog close + state change + emotion broadcast) unconditionally, even though the
// player hadn't actually reached that step. Ported with explicit `when` guards so the departure
// sequence only fires on its own legitimate SETPRO10/SETPRO11 dialog id at the matching var.
// Skip vs Java: the flight-teleport departure itself (player.setState(FLIGHT_TELEPORT),
// setFlightTeleportId, SM_EMOTION(START_FLYTELEPORT) broadcast) and the two TeleportService2 hops
// (to Morheim/Beluslan) aren't ported - no flight-teleport player state or TeleportService2 in this
// port (same precedent as quest/eltnen/_1430ATeleportationExperiment.cs). The var/status
// transitions are kept so the quest stays completable without the cross-zone/flight flourish.
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

namespace Quest.Reshanta;

public sealed class _1077FragmentofMemory3 : QuestHandlerBase
{
    private const int QuestIdConst = 1077;
    private const int BoreasNpc    = 203704;
    private const int GaixNpc      = 798154;
    private const int FinnNpc      = 204574;
    private const int AcestesNpc   = 204652;
    private const int PeithoNpc    = 204653;
    private const int YuditioNpc   = 278500;
    private const int SpawnedMob   = 214599;

    public _1077FragmentofMemory3(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(SpawnedMob).OnKill.Add(QuestId);
        foreach (int npc in new[] { BoreasNpc, GaixNpc, FinnNpc, AcestesNpc, PeithoNpc, YuditioNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1701, isZoneMission: true, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (!await DefaultOnKillEventAsync(env, conn, SpawnedMob, 5, 6, ct)) return false;
        await PlayQuestMovieAsync(conn, env.Player, 422, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != YuditioNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        switch (targetId)
        {
            case BoreasNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 0:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.SETPRO1 when var == 0:
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    default:
                        return false;
                }
            case GaixNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 1:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.SETPRO2 when var == 1:
                        return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                    default:
                        return false;
                }
            case FinnNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 2:
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    case DialogAction.SETPRO3 when var == 2:
                        return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                    default:
                        return false;
                }
            case AcestesNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 3:
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    case DialogAction.QUEST_SELECT when var == 6:
                        return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                    case DialogAction.QUEST_SELECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 10009, ct);
                    case DialogAction.SETPRO10 when var == 3:
                        entry.SetVar(0, 4);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    case DialogAction.SET_SUCCEED when var == 6:
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            case PeithoNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 4:
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    case DialogAction.QUEST_SELECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 10010, ct);
                    case DialogAction.SELECT_ACTION_2376 when var == 4:
                        await PlayQuestMovieAsync(conn, player, 421, ct);
                        return false;
                    case DialogAction.SETPRO11 when var == 4:
                        entry.SetVar(0, 5);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            default:
                return false;
        }
    }
}
