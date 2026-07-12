// Port of Java data/scripts/system/handlers/quest/eltnen/_1393NewFlightPath.java (Balthazar).
// Talk to NPC 204041; SETPRO1 flips straight to REWARD, unlocking a flight-teleport waypoint.
// Skip vs Java: the flight-teleport departure itself (player.setState(CreatureState.FLIGHT_TELEPORT),
// setFlightTeleportId(17001), SM_EMOTION(START_FLYTELEPORT) broadcast) isn't ported - no
// flight-teleport player state exists in this port (same precedent as
// quest/reshanta/_2075PuttingontheSpeed.cs). The var/status transition is kept. Java's
// registerOnEnterZone(LEPHARIST_BASTION_210020000, questId) has no matching onEnterZoneEvent
// override in the source (a dead registration in the original) - not ported.
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

namespace Quest.Eltnen;

public sealed class _1393NewFlightPath : QuestHandlerBase
{
    private const int QuestIdConst = 1393;
    private const int WaypointNpc  = 204041;

    public _1393NewFlightPath(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(WaypointNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(WaypointNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId != WaypointNpc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, reward: true, sameNpc: false, ct);
            return await CloseDialogWindowAsync(conn, targetObjId, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
