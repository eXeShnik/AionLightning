// Port of Java data/scripts/system/handlers/quest/beluslan/_2500OrdersFromNerita.java.
// Beluslan campaign hub: auto-starts on entering the BELUSLAN_FORTRESS_220040000 zone; talking to
// Nerita (204702) flips it to REWARD; declining the reward (SELECTED_QUEST_NOREWARD) pokes the
// zone-mission-end recheck for all 11 Beluslan S/A quests (2051-2061) before closing out.
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

namespace Quest.Beluslan;

public sealed class _2500OrdersFromNerita : QuestHandlerBase
{
    private const int QuestIdConst = 2500;
    private const int StartNpc     = 204702;
    private const string EnterZoneName = "BELUSLAN_FORTRESS_220040000";

    private static readonly int[] _chainQuestIds =
        [2051, 2052, 2053, 2054, 2055, 2056, 2057, 2058, 2059, 2060, 2061];

    private QuestEngine? _engine;

    public _2500OrdersFromNerita(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        _engine = engine;
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, EnterZoneName);
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != EnterZoneName) return false;
        if (!await StartMissionAsync(conn, env.Player, QuestStatus.START, ct)) return false;
        await CloseDialogWindowAsync(conn, 0, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || env.TargetId != StartNpc) return false;

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
