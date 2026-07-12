// Port of Java data/scripts/system/handlers/quest/oriel/_18826TodaysFind.java (zhkchi).
// Talk to any of 830520/830660/830661 to start (repeatable — canRepeat() approximated as "no
// active entry", same simplification used across ~7 other zones); use-object at 730522 (var 0,
// dialog 2375) then SELECT_QUEST_REWARD flips to REWARD (var stays 0); turn in at 730522.
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

namespace Quest.Oriel;

public sealed class _18826TodaysFind : QuestHandlerBase
{
    private const int QuestIdConst = 18826;
    private const int TurnInNpc    = 730522;

    public _18826TodaysFind(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(830520).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(830520).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(830660).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(830660).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(830661).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(830661).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
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
            if (targetId is 830520 or 830660 or 830661)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
