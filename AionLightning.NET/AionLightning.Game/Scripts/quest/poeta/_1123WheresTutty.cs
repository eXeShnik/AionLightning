// Port of Java data/scripts/system/handlers/quest/poeta/_1123WheresTutty.java.
// Talk to Pernos (790001) to start; entering the QUEST_1123 zone plays movie 11 and flips to
// REWARD; return to Pernos to finish.
// Skip vs Java: the zone trigger (QUEST_1123_210010000 polygon) has no C# zone-shape system, so
// the movie/REWARD flip is driven off the OnEnterWorld hook for Poeta (210010000). Approximation
// documented — fires on map entry rather than the specific sub-zone.
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

public sealed class _1123WheresTutty : QuestHandlerBase
{
    private const int QuestIdConst = 1123;
    private const int PernosNpc    = 790001;
    private const int PoetaWorldId = 210010000;

    public _1123WheresTutty(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(PernosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PernosNpc).OnQuestStart.Add(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (player.Position.WorldId != PoetaWorldId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        await PlayQuestMovieAsync(conn, player, 11, ct);
        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (env.TargetId != PernosNpc) return false;
        var entry = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
