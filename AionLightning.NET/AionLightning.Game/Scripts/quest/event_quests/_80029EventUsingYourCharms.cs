// Port of Java data/scripts/system/handlers/quest/event_quests/_80029EventUsingYourCharms.java.
// No NONE-status handling here (Java qs==null returns false) — Nebrith's NPC (799766) is also
// registered OnQuestStart, so the generic engine fallback (CM_DIALOG_SELECT.HandleQuestSelectAsync
// / HandleQuestAcceptAsync) offers/starts the quest via quest_data.xml when this handler declines.
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

namespace Quest.EventQuests;

public sealed class _80029EventUsingYourCharms : QuestHandlerBase
{
    private const int QuestIdConst = 80029;
    private const int FayNpc       = 799766;

    public _80029EventUsingYourCharms(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(FayNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(FayNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.NONE) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START && env.TargetId == FayNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.QUEST_ACCEPT_1:
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                case DialogAction.SELECT_QUEST_REWARD:
                    await DefaultCloseDialogAsync(env, conn, 0, 0, reward: true, sameNpc: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                case DialogAction.SELECTED_QUEST_NOREWARD:
                    return await SendQuestRewardDialogAsync(env, conn, ct);
                default:
                    return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }

        return await SendQuestRewardDialogAsync(env, conn, ct);
    }

    /// <summary>Java sendQuestRewardDialog(env, 799766, 0): reportDialogId 0 means the USE_OBJECT
    /// branch never fires, so a REWARD-status turn-in always finishes the quest directly.</summary>
    private async ValueTask<bool> SendQuestRewardDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not { Status: QuestStatus.REWARD } || env.TargetId != FayNpc) return false;
        return await SendQuestEndDialogAsync(env, conn, ct);
    }
}
