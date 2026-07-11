// Port of Java data/scripts/system/handlers/quest/eltnen/_1300OrdersfromTelemachus.java (MrPoke + Dune11).
// Campaign opener: auto-starts on entering Eltnen, talk to Telemachus (203901) -> REWARD; turning
// in (no-reward) pokes the zone-mission-end chain for the Kaidan Fortress quests (1031-1043).
// Skip vs Java: the start trigger is zone-enter (ELTNEN_FORTRESS_210020000) - no zone-shape system
// exists in C#, so this uses the OnEnterWorld hook gated to worldId 210020000 (Eltnen), same
// pattern as Poeta's _1100KaliosCall. Only 1034 and 1042 of the 13 dependent ids are ported in
// this batch; the engine's OnZoneMissionEndAsync silently no-ops for the rest (not yet registered).
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

namespace Quest.Eltnen;

public sealed class _1300OrdersfromTelemachus : QuestHandlerBase
{
    private const int QuestIdConst  = 1300;
    private const int TelemachusNpc = 203901;
    private const int EltnenWorldId = 210020000;

    private static readonly int[] _chainQuestIds =
        [1031, 1032, 1033, 1034, 1035, 1036, 1037, 1038, 1039, 1040, 1041, 1042, 1043];

    private QuestEngine? _engine;

    public _1300OrdersfromTelemachus(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        _engine = engine;
        engine.RegisterQuestNpc(TelemachusNpc).OnTalk.Add(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (player.Position.WorldId != EltnenWorldId) return false;
        if (player.Quests.Get(QuestId) is not null) return false;
        return await StartMissionAsync(conn, player, QuestStatus.START, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;
        if (env.TargetId != TelemachusNpc) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
            {
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
                    await _engine.OnZoneMissionEndAsync(new QuestEnv(env.Target, env.Player, id, env.DialogId), conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
