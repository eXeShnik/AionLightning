// Port of Java data/scripts/system/handlers/quest/pandaemonium/_2912FollowtheRibbon.java.
// Accept + turn in both at 204193; 3-step chain (204089 var0 0->1, 204088 var0 1->2, 204240 var0
// 2->3, each via manual var-increment + raw selection dialog, matching Java's PacketSendUtility
// call); final SELECT_QUEST_REWARD at 204236 sets var0=3 (already 3, a no-op) and flips to REWARD.
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

namespace Quest.Pandaemonium;

public sealed class _2912FollowtheRibbon : QuestHandlerBase
{
    private const int QuestIdConst = 2912;
    private const int StartNpc = 204193;
    private const int StepOneNpc = 204089;
    private const int StepTwoNpc = 204088;
    private const int StepThreeNpc = 204240;
    private const int TurnInNpc = 204236;

    public _2912FollowtheRibbon(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StepOneNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StepTwoNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StepThreeNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            if (entry is null)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.START)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.REWARD)
                return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }

        if (targetId == StepOneNpc && entry is { Status: QuestStatus.START } && entry.GetVar(0) == 0)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                entry.SetVar(0, entry.GetVar(0) + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (targetId == StepTwoNpc && entry is { Status: QuestStatus.START } && entry.GetVar(0) == 1)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SETPRO3)
            {
                entry.SetVar(0, entry.GetVar(0) + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (targetId == StepThreeNpc && entry is { Status: QuestStatus.START } && entry.GetVar(0) == 2)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO2)
            {
                entry.SetVar(0, entry.GetVar(0) + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (targetId == TurnInNpc && entry is not null)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.Status == QuestStatus.START)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);

            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD
                && entry.Status != QuestStatus.COMPLETE && entry.Status != QuestStatus.NONE)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: true, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
