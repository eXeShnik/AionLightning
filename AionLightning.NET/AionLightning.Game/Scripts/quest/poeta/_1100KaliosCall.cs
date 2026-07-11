// Port of Java data/scripts/system/handlers/quest/poeta/_1100KaliosCall.java (MrPoke).
// The Poeta campaign opener: auto-starts on entering Akarios Village, talk to Kalio → REWARD;
// accepting the (no-reward) turn-in pokes the zone-mission-end chain for quests 1001-1005.
// Skip vs Java: the start trigger is zone-enter (AKARIOS_VILLAGE_210010000) — no zone-shape
// system exists in C#, so this uses the OnEnterWorld hook gated to worldId 210010000 (Poeta):
// the quest starts on entering the map instead of the village polygon. Same net effect for a
// new character (they spawn in Akarios Village).
using System.Linq;
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

namespace Quest.Poeta;

public sealed class _1100KaliosCall : QuestHandlerBase
{
    private const int QuestIdConst = 1100;
    private const int KalioNpcId   = 203067;
    private const int PoetaWorldId = 210010000;

    private static readonly int[] _chainQuestIds = [1001, 1002, 1003, 1004, 1005];

    private QuestEngine? _engine;

    public _1100KaliosCall(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        _engine = engine;
        engine.RegisterQuestNpc(KalioNpcId).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KalioNpcId).OnQuestStart.Add(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (player.Position.WorldId != PoetaWorldId) return false;
        if (player.Quests.Get(QuestId) is not null) return false;
        // Java defaultOnEnterZoneEvent: create the quest at START when first entering the zone
        return await StartMissionAsync(conn, player, QuestStatus.START, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;
        if (env.TargetId != KalioNpcId) return false;

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
