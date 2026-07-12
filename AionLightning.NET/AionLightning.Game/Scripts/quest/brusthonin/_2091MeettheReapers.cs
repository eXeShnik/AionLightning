// Port of Java data/scripts/system/handlers/quest/brusthonin/_2091MeettheReapers.java.
// Auto-starts on entering Brusthonin (worldId 220050000); talk to Surt (205150) -> REWARD;
// accepting the (no-reward) turn-in pokes the zone-mission-end chain for quests 2092-2094.
// Same pattern as the Poeta golden exemplar _1100KaliosCall.
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

namespace Quest.Brusthonin;

public sealed class _2091MeettheReapers : QuestHandlerBase
{
    private const int QuestIdConst      = 2091;
    private const int SurtNpc           = 205150;
    private const int BrusthoninWorldId = 220050000;

    private static readonly int[] _chainQuestIds = [2092, 2093, 2094];

    private QuestEngine? _engine;

    public _2091MeettheReapers(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        _engine = engine;
        engine.RegisterQuestNpc(SurtNpc).OnTalk.Add(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (player.Position.WorldId != BrusthoninWorldId) return false;
        if (player.Quests.Get(QuestId) is not null) return false;
        return await StartMissionAsync(conn, player, QuestStatus.START, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;
        if (env.TargetId != SurtNpc) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD && entry.Status != QuestStatus.COMPLETE)
            {
                entry.SetVar(0, 1);
                entry.Status = QuestStatus.REWARD;
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
