// Port of Java data/scripts/system/handlers/quest/rider_quests/_14020OrdersFromEltnen.java (pralinka).
// Auto-starts on entering the Eltnen Fortress zone (onEnterZone -> StartMissionAsync); talking to
// Telemachus (203901) flips straight to REWARD (dialog 1011); turning in with dialog id 23
// (SELECTED_QUEST_NOREWARD) pokes the five sub-quests 14021-14025 via OnZoneMissionEndAsync.
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

public sealed class _14020OrdersFromEltnen : QuestHandlerBase
{
    private const int QuestIdConst = 14020;
    private const int TelemachusNpc = 203901;
    private const string FortressZone = "ELTNEN_FORTRESS_210020000";

    private static readonly int[] _chainQuestIds = [14021, 14022, 14023, 14024, 14025];

    private QuestEngine? _engine;

    public _14020OrdersFromEltnen(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        _engine = engine;
        engine.RegisterQuestNpc(TelemachusNpc).OnTalk.Add(QuestId);
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
        if (entry is null || env.TargetId != TelemachusNpc) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
            {
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
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
