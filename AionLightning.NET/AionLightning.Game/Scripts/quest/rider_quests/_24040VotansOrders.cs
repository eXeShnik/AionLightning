// Port of Java data/scripts/system/handlers/quest/rider_quests/_24040VotansOrders.java (pralinka).
// Auto-starts on entering world 400010000 (onEnterWorld -> StartMissionAsync); talking to Votan
// (278001) opens an info dialog (10002); selecting the reward flips straight to REWARD (var0 1,
// dialog 5); turning in with dialog id 23 (SELECTED_QUEST_NOREWARD) pokes the sub-quests 24041-24046
// via OnZoneMissionEndAsync (24046 has no handler in this port and is a safe no-op poke).
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

public sealed class _24040VotansOrders : QuestHandlerBase
{
    private const int QuestIdConst = 24040;
    private const int VotanNpc = 278001;
    private const int StartWorldId = 400010000;

    private static readonly int[] _chainQuestIds = [24041, 24042, 24043, 24044, 24045, 24046];

    private QuestEngine? _engine;

    public _24040VotansOrders(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        _engine = engine;
        engine.RegisterQuestNpc(VotanNpc).OnTalk.Add(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
    }

    public override ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (player.Position.WorldId != StartWorldId) return ValueTask.FromResult(false);
        if (player.Quests.Get(QuestId) is not null) return ValueTask.FromResult(false);
        return StartMissionAsync(conn, player, QuestStatus.START, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || env.TargetId != VotanNpc) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
            {
                entry.Status = QuestStatus.REWARD;
                entry.SetVar(0, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
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
