// Port of Java data/scripts/system/handlers/quest/rentus_base/_30501InfiltratingRentus.java
// (zhkchi). Start at Oreitia (799544); talk to 799666 to view its USE_OBJECT page, then turn in
// (var0 stays 0, straight to REWARD) and collect the reward at the same npc.
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

namespace Quest.RentusBase;

public sealed class _30501InfiltratingRentus : QuestHandlerBase
{
    private const int QuestIdConst = 30501;
    private const int StartNpc     = 799544;
    private const int TalkNpc      = 799666;

    public _30501InfiltratingRentus(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TalkNpc).OnTalk.Add(QuestId);
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
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == TalkNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TalkNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
