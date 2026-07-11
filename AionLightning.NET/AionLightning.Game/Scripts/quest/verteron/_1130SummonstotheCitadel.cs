// Port of Java data/scripts/system/handlers/quest/verteron/_1130SummonstotheCitadel.java
// (MrPoke). The Verteron zone-mission chain opener: talk to Santenius (203098) to flip straight
// to REWARD; turning it in (no-reward) pokes the zone-mission-end chain for quests 1011-1023.
// Skip vs Java: the start trigger is entering the "Verteron Citadel" zone
// (VERTERON_CITADEL_210030000) — no zone-shape system exists in C#, so (mirroring the
// _1100KaliosCall exemplar) this uses the OnEnterWorld hook gated to worldId 210030000 (Verteron):
// the quest starts on entering the map instead of the citadel polygon specifically. Verified
// worldId 210030000 = Verteron via data/static_data/world_maps.xml (the VERTERON_CITADEL_
// 210030000 zone name embeds it, and the citadel is inside the main Verteron map, not a
// separate world). Java's own onDialogEvent never creates the quest entry itself (returns false
// when qs == null) — this port preserves that: OnDialogAsync only ever advances an existing entry.
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

namespace Quest.Verteron;

public sealed class _1130SummonstotheCitadel : QuestHandlerBase
{
    private const int QuestIdConst  = 1130;
    private const int SanteniusNpc  = 203098;
    private const int VerteronWorldId = 210030000;

    private static readonly int[] _chainQuestIds =
        [1011, 1012, 1013, 1014, 1015, 1016, 1017, 1018, 1019, 1020, 1021, 1022, 1023];

    private QuestEngine? _engine;

    public _1130SummonstotheCitadel(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        _engine = engine;
        engine.RegisterQuestNpc(SanteniusNpc).OnTalk.Add(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (player.Position.WorldId != VerteronWorldId) return false;
        if (player.Quests.Get(QuestId) is not null) return false;
        return await StartMissionAsync(conn, player, QuestStatus.START, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || env.TargetId != SanteniusNpc) return false;

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
