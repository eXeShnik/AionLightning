// Port of Java data/scripts/system/handlers/quest/altgard/_2200AltgardDuties.java (Akiro).
// The Altgard campaign zone-mission hub: talk to Suthran (203557) -> REWARD immediately; the
// (no-reward) turn-in pokes the zone-mission-end chain for quests 2011-2022.
// Skip vs Java: the start trigger is zone-enter (ALTGARD_FORTRESS_220030000) — no zone-shape
// system exists in this port, so this uses the OnEnterWorld hook gated to worldId 220030000
// (Altgard), matching the poeta._1100KaliosCall precedent: the quest starts on entering the map
// instead of the fortress polygon. Same net effect for a new character (they spawn in the fortress).
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

namespace Quest.Altgard;

public sealed class _2200AltgardDuties : QuestHandlerBase
{
    private const int QuestIdConst   = 2200;
    private const int SuthranNpc     = 203557;
    private const int AltgardWorldId = 220030000;

    private static readonly int[] _chainQuestIds =
        [2011, 2012, 2013, 2014, 2015, 2016, 2017, 2018, 2019, 2020, 2021, 2022];

    private QuestEngine? _engine;

    public _2200AltgardDuties(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        _engine = engine;
        engine.RegisterQuestNpc(SuthranNpc).OnTalk.Add(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (player.Position.WorldId != AltgardWorldId) return false;
        if (player.Quests.Get(QuestId) is not null) return false;
        // Java defaultOnEnterZoneEvent: create the quest at START when first entering the zone
        return await StartMissionAsync(conn, player, QuestStatus.START, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;
        if (env.TargetId != SuthranNpc) return false;

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
