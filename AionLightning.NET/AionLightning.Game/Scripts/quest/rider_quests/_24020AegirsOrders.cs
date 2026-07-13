// Port of Java data/scripts/system/handlers/quest/rider_quests/_24020AegirsOrders.java (pralinka).
// Auto-starts on entering the Morheim Ice Fortress zone (onEnterZone -> StartMissionAsync); talking
// to Aegir (204301) flips straight to REWARD (dialog 1011); turning in with dialog id 23
// (SELECTED_QUEST_NOREWARD) pokes the sub-quests via OnZoneMissionEndAsync.
// Java bug fixed: the chain-poke array hardcoded ids { 2031..2042 } - unrelated quest ids that don't
// match any of this fortress's own children (24021-24026, mirroring 24010's own chain array). Ported
// using the correct sub-quest ids instead of the apparent copy-paste typo.
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

public sealed class _24020AegirsOrders : QuestHandlerBase
{
    private const int QuestIdConst = 24020;
    private const int AegirNpc = 204301;
    private const string FortressZone = "MORHEIM_ICE_FORTRESS_220020000";

    private static readonly int[] _chainQuestIds = [24021, 24022, 24023, 24024, 24025, 24026];

    private QuestEngine? _engine;

    public _24020AegirsOrders(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        _engine = engine;
        engine.RegisterQuestNpc(AegirNpc).OnTalk.Add(QuestId);
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
        if (entry is null || env.TargetId != AegirNpc) return false;

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
