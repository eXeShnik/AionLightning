// Port of Java data/scripts/system/handlers/quest/morheim/_2443TaisanMessage.java (Cheatkiller).
// Offered at Taisan (204403, OnQuestStart): SETPRO1/QUEST_ACCEPT_1 flips straight to REWARD (a
// flight-teleport departure to waypoint 30001). Turn in at 790016.
// Skip vs Java: the flight-teleport departure itself (player.setState(CreatureState.FLIGHT_TELEPORT),
// setFlightTeleportId(30001), SM_EMOTION(START_FLYTELEPORT) broadcast) isn't ported - no
// flight-teleport player state exists in this port (same precedent as
// eltnen._1393NewFlightPath/reshanta._2075PuttingontheSpeed). The var/status transition is kept.
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

namespace Quest.Morheim;

public sealed class _2443TaisanMessage : QuestHandlerBase
{
    private const int QuestIdConst = 2443;
    private const int TaisanNpc = 204403;
    private const int CourierNpc = 790016;

    public _2443TaisanMessage(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(TaisanNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(CourierNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != TaisanNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != TaisanNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return entry.GetVar(0) == 0 && await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
            if (dialog == DialogAction.SETPRO1 || env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, reward: true, sameNpc: false, ct);
            if (dialog == DialogAction.SELECT_ACTION_1013)
                return await SendQuestDialogAsync(conn, targetObjId, 1013, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == CourierNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
