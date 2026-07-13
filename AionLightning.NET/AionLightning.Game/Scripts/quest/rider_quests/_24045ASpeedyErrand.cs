// Port of Java data/scripts/system/handlers/quest/rider_quests/_24045ASpeedyErrand.java (pralinka).
// Zone-mission sub-quest of 24040: talk to 278034 (var0 0->1), 279004 (var0 1->2, movie 292), 279024
// (var0 2->3, flight-teleport departure; var0 4->reward), 279006 (var0 3->4), back to 279024, turn
// in at 278034.
// Skip vs Java: the flight-teleport departure (player.setState(FLIGHT_TELEPORT),
// setFlightTeleportId(55001), SM_EMOTION(START_FLYTELEPORT) broadcast) at var0 2->3 isn't ported -
// no flight-teleport player state in this port (same precedent as
// quest/reshanta/_2075PuttingontheSpeed.cs / _1077FragmentofMemory3.cs). The var/status transition
// is kept.
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

public sealed class _24045ASpeedyErrand : QuestHandlerBase
{
    private const int QuestIdConst = 24045;
    private const int Npc1 = 278034;
    private const int Npc2 = 279004;
    private const int Npc3 = 279024;
    private const int Npc4 = 279006;

    public _24045ASpeedyErrand(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in new[] { Npc1, Npc2, Npc3, Npc4 })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 24040, isZoneMission: true, ct);

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
            if (targetId != Npc1) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        switch (targetId)
        {
            case Npc1:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 0:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.SETPRO1 when var == 0:
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    default:
                        return false;
                }
            case Npc2:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 1:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.SELECT_ACTION_1353:
                        await PlayQuestMovieAsync(conn, player, 292, ct);
                        return false;
                    case DialogAction.SETPRO2 when var == 1:
                        return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                    default:
                        return false;
                }
            case Npc3:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 2:
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    case DialogAction.QUEST_SELECT when var == 4:
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    case DialogAction.SETPRO3 when var == 2:
                        return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                    case DialogAction.SETPRO5 when var == 4:
                        await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            case Npc4:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 3:
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    case DialogAction.SETPRO4 when var == 3:
                        return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                    default:
                        return false;
                }
            default:
                return false;
        }
    }
}
