// Port of Java data/scripts/system/handlers/quest/theobomos/_3103KyprosDesire.java.
// Talk to Kypros (798225) to start; report to the second Kypros (798226) to finish. Java
// completes this quest directly from START (no REWARD dialog step, exp-only reward) via
// QuestService.startQuest style manual completion — ported through the shared
// QuestRewardService.GrantAndCompleteAsync path (FinishQuestAsync) instead of duplicating the
// exp-award logic inline, which is equivalent since this quest has no kill/collect objectives.
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

namespace Quest.Theobomos;

public sealed class _3103KyprosDesire : QuestHandlerBase
{
    private const int QuestIdConst = 3103;
    private const int KyprosNpc1   = 798225;
    private const int KyprosNpc2   = 798226;

    public _3103KyprosDesire(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(KyprosNpc1).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(KyprosNpc1).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KyprosNpc2).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != KyprosNpc1) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == KyprosNpc2)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            if (env.DialogId == (int)DialogAction.SELECTED_QUEST_NOREWARD)
                return await FinishQuestAsync(conn, player, 0, ct);
            return false;
        }

        return false;
    }
}
