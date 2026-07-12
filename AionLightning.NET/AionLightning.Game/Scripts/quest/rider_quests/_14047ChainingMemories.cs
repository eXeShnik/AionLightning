// Port of Java data/scripts/system/handlers/quest/rider_quests/_14047ChainingMemories.java (pralinka).
// Zone-mission sub-quest of 14040: talk to 203704 (var0 0->1), 798154 (var0 1->2), 204574 (var0
// 2->3), 802051 (var0 3->4, flight-teleport departure), 802052 (var0 4->5, movie 421 + flight
// departure), kill a 214598 (var0 5->6, movie 422), turn in at 802051 (var0 6 -> REWARD), close out
// at 278500. Structurally identical to the already-ported quest/reshanta/_1077FragmentofMemory3.cs.
// Java bugs fixed: both 802051's and 802052's dialog switches sent a "not ready" hint dialog
// (10009 / 10010) on QUEST_SELECT when the var didn't match yet, but had no return/break after it -
// execution fell through into the next case body and fired the flight-teleport-departure side
// effects (dialog close + state change + emotion broadcast) unconditionally. Ported with explicit
// `when` guards so the departure sequence only fires on its own legitimate SETPRO10/SETPRO11
// dialog id at the matching var.
// Skip vs Java: the flight-teleport departure itself (player.setState(FLIGHT_TELEPORT),
// setFlightTeleportId, SM_EMOTION(START_FLYTELEPORT) broadcast) isn't ported - no flight-teleport
// player state in this port (same precedent as quest/reshanta/_1077FragmentofMemory3.cs). The
// var/status transitions are kept so the quest stays completable without the flight flourish.
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

public sealed class _14047ChainingMemories : QuestHandlerBase
{
    private const int QuestIdConst = 14047;
    private const int Npc203704 = 203704;
    private const int Npc798154 = 798154;
    private const int Npc204574 = 204574;
    private const int Npc802051 = 802051;
    private const int Npc802052 = 802052;
    private const int Npc278500 = 278500;
    private const int SpawnedMob = 214598;

    public _14047ChainingMemories(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(SpawnedMob).OnKill.Add(QuestId);
        foreach (int npc in new[] { Npc203704, Npc798154, Npc204574, Npc802051, Npc802052, Npc278500 })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 14040, isZoneMission: true, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (!await DefaultOnKillEventAsync(env, conn, SpawnedMob, 5, 6, ct)) return false;
        await PlayQuestMovieAsync(conn, env.Player, 422, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != Npc278500) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        switch (targetId)
        {
            case Npc203704:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 0:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.SETPRO1 when var == 0:
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    default:
                        return false;
                }
            case Npc798154:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 1:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.SETPRO2 when var == 1:
                        return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                    default:
                        return false;
                }
            case Npc204574:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 2:
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    case DialogAction.SETPRO3 when var == 2:
                        return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                    default:
                        return false;
                }
            case Npc802051:
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
            case Npc802052:
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
