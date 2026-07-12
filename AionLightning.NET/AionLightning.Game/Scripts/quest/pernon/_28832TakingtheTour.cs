// Port of Java data/scripts/system/handlers/quest/pernon/_28832TakingtheTour.java (Ritsu).
// Talk to 830532 to start; talk to 830085 (var 0, dialog 2375) then plays movie 802 and flips
// var 0->0 reward (dialog 5); turn in at 830085. Java's QUEST_SELECT case falls into
// SELECT_QUEST_REWARD when var != 0 — kept 1:1 (harmless, var only ever sits at 0 while START).
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

namespace Quest.Pernon;

public sealed class _28832TakingtheTour : QuestHandlerBase
{
    private const int QuestIdConst = 28832;
    private const int StartNpc     = 830532;
    private const int GuideNpc     = 830085;
    private const int TourMovieId  = 802;

    public _28832TakingtheTour(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GuideNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == GuideNpc)
        {
            int var = entry.GetVar(0);
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog is DialogAction.QUEST_SELECT or DialogAction.SELECT_QUEST_REWARD)
            {
                await PlayQuestMovieAsync(conn, player, TourMovieId, ct);
                await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == GuideNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
