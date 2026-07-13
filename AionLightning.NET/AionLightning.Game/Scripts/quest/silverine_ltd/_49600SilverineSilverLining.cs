// Port of Java data/scripts/system/handlers/quest/silverine_ltd/_49600SilverineSilverLining.java (Rinzler).
// Asmodian counterpart of _39600SilversCrossYourPalm — identical structure at Dumurinerk (800942):
// level-up auto-offered, QUEST_ACCEPT_SIMPLE opens page 1011 / else sendQuestStartDialog,
// SELECT_QUEST_REWARD in START flips straight to REWARD, turn in.
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

namespace Quest.SilverineLtd;

public sealed class _49600SilverineSilverLining : QuestHandlerBase
{
    private const int QuestIdConst  = 49600;
    private const int DumurinerkNpc = 800942;

    public _49600SilverineSilverLining(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(DumurinerkNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(DumurinerkNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == DumurinerkNpc)
            {
                if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == DumurinerkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                    return await SendQuestEndDialogAsync(env, conn, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == DumurinerkNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
