// Port of Java data/scripts/system/handlers/quest/ishalgen/_2000Prologue.java.
// Asmodian intro: auto-starts on entering the world, plays movie 2, completes on movie end.
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Ishalgen;

public sealed class _2000Prologue : QuestHandlerBase
{
    private const int QuestIdConst = 2000;
    private const int MovieId      = 2;

    public _2000Prologue(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterOnQuestMovieEnd(MovieId, QuestId);
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (player.Race != Race.ASMODIANS) return false;

        if (player.Quests.Get(QuestId) is null)
            await StartMissionAsync(conn, player, QuestStatus.START, ct);

        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        await conn.SendAsync(new SM_PLAY_MOVIE(1, MovieId), ct);
        return true;
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != MovieId) return false;
        var player = env.Player;
        if (player.Race != Race.ASMODIANS) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        entry.Status = QuestStatus.REWARD;
        return await FinishQuestAsync(conn, player, 0, ct);
    }
}
