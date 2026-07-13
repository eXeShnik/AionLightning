// Port of Java data/scripts/system/handlers/quest/morheim/_2300MorheimCommandersCall.java (MrPoke + Dune11).
// The Morheim zone-mission chain opener: entering MORHEIM_ICE_FORTRESS_220020000 starts this quest
// directly (Java defaultOnEnterZoneEvent's startQuest, same convention as
// heiron._1500OrdersFromPerento); talking to Aegir (204301) flips straight to REWARD; turning it in
// (no-reward) broadcasts the zone-mission-end chain for quests 2031-2042 (2042 has no handler in
// this port and is a harmless no-op lookup miss, same as Java's own dead id in that list - only
// 2031-2041 are ported morheim quests).
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

public sealed class _2300MorheimCommandersCall : QuestHandlerBase
{
    private const int QuestIdConst = 2300;
    private const int AegirNpc = 204301;
    private const string FortressZone = "MORHEIM_ICE_FORTRESS_220020000";

    private static readonly int[] _chainQuestIds =
        [2031, 2032, 2033, 2034, 2035, 2036, 2037, 2038, 2039, 2040, 2041, 2042];

    private QuestEngine? _engine;

    public _2300MorheimCommandersCall(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        _engine = engine;
        engine.RegisterQuestNpc(AegirNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, FortressZone);
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != FortressZone) return false;
        var player = env.Player;
        if (player.Quests.Get(QuestId) is not null) return false;
        return await StartMissionAsync(conn, player, QuestStatus.START, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;
        if (env.TargetId != AegirNpc) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
            {
                entry.SetVar(0, 1);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
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
