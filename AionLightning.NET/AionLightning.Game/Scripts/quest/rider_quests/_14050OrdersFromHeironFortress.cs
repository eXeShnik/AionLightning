// Port of Java data/scripts/system/handlers/quest/rider_quests/_14050OrdersFromHeironFortress.java (pralinka).
// Auto-starts on entering the New Heiron Gate fortress zone (onEnterZone -> StartMissionAsync);
// talking to 204500 with SELECT_QUEST_REWARD flips straight to REWARD (var0 1, dialog 5); turning
// in with dialog id 23 (SELECTED_QUEST_NOREWARD) pokes the four sub-quests 14051-14054 via
// OnZoneMissionEndAsync.
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

public sealed class _14050OrdersFromHeironFortress : QuestHandlerBase
{
    private const int QuestIdConst = 14050;
    private const int GateGuardNpc = 204500;
    private const string FortressZone = "NEW_HEIRON_GATE_210040000";

    private static readonly int[] _chainQuestIds = [14051, 14052, 14053, 14054];

    private QuestEngine? _engine;

    public _14050OrdersFromHeironFortress(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        _engine = engine;
        engine.RegisterQuestNpc(GateGuardNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, FortressZone);
    }

    public override ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != FortressZone) return ValueTask.FromResult(false);
        var player = env.Player;
        if (player.Quests.Get(QuestId) is not null) return ValueTask.FromResult(false);
        return StartMissionAsync(conn, player, QuestStatus.START, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || env.TargetId != GateGuardNpc) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (env.DialogId == (int)DialogAction.SELECTED_QUEST_NOREWARD && _engine is not null)
            {
                foreach (int id in _chainQuestIds)
                    await _engine.OnZoneMissionEndAsync(new QuestEnv(env.Target, player, id, env.DialogId), conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
