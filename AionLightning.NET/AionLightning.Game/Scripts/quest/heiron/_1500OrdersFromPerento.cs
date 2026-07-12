// Port of Java data/scripts/system/handlers/quest/heiron/_1500OrdersFromPerento.java (MrPoke + Dune11).
// The Heiron zone-mission chain opener: entering the NEW_HEIRON_GATE_210040000 zone starts this
// quest directly (Java defaultOnEnterZoneEvent's startQuest, simplified here to the StartMissionAsync
// convention already used by the _1130SummonstotheCitadel precedent); talking to Perento (204500)
// flips straight to REWARD; turning it in (no-reward) pokes the zone-mission-end chain for quests
// 1051-1059 and 1062-1063. Uses the new RegisterOnEnterZone/OnEnterZoneAsync hook - no OnEnterWorld
// workaround needed (unlike the pre-hook _1130 port).
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

namespace Quest.Heiron;

public sealed class _1500OrdersFromPerento : QuestHandlerBase
{
    private const int QuestIdConst = 1500;
    private const int PerentoNpc = 204500;
    private const string GateZone = "NEW_HEIRON_GATE_210040000";

    private static readonly int[] _chainQuestIds =
        [1051, 1052, 1053, 1054, 1055, 1056, 1057, 1058, 1059, 1062, 1063];

    private QuestEngine? _engine;

    public _1500OrdersFromPerento(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        _engine = engine;
        engine.RegisterQuestNpc(PerentoNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, GateZone);
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != GateZone) return false;
        var player = env.Player;
        if (player.Quests.Get(QuestId) is not null) return false;
        return await StartMissionAsync(conn, player, QuestStatus.START, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || env.TargetId != PerentoNpc) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);

            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
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
