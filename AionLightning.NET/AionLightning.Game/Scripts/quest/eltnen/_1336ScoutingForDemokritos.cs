// Port of Java data/scripts/system/handlers/quest/eltnen/_1336ScoutingForDemokritos.java.
// Talk to Demokritos (204006) to start; then move within onAtDistance range of the three scouting
// points 206020 -> 206021 -> 206022 in order, each playing a movie (43/44/45) and advancing var0
// (0 -> 16 -> 48 -> REWARD). Turn in at 204006.
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

public sealed class _1336ScoutingForDemokritos : QuestHandlerBase
{
    private const int QuestIdConst = 1336;
    private const int Demokritos = 204006;
    private const int Point1     = 206020;
    private const int Point2     = 206021;
    private const int Point3     = 206022;

    public _1336ScoutingForDemokritos(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Demokritos).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Demokritos).OnTalk.Add(QuestId);
        RegisterOnAtDistance(engine, Point1);
        RegisterOnAtDistance(engine, Point2);
        RegisterOnAtDistance(engine, Point3);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (env.TargetId != Demokritos) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (env.TargetId != Demokritos) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnAtDistanceAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (env.TargetId == Point1 && var == 0)
        {
            await PlayQuestMovieAsync(conn, player, 43, ct);
            await ChangeQuestStepAsync(conn, entry, 0, 16, toReward: false, ct);
            return true;
        }
        if (env.TargetId == Point2 && var == 16)
        {
            await PlayQuestMovieAsync(conn, player, 44, ct);
            await ChangeQuestStepAsync(conn, entry, 0, 48, toReward: false, ct);
            return true;
        }
        if (env.TargetId == Point3 && var == 48)
        {
            await PlayQuestMovieAsync(conn, player, 45, ct);
            await ChangeQuestStepAsync(conn, entry, 0, 48, toReward: true, ct);
            return true;
        }
        return false;
    }
}
