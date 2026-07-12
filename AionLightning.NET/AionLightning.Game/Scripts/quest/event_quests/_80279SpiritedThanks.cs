// Port of Java data/scripts/system/handlers/quest/event_quests/_80279SpiritedThanks.java.
// Accept at Xurabo (831117); turn in at Kubu (831350). Both npcs are registered OnQuestStart
// only (no OnTalk) — matches Java exactly; the client already carries the active questId once
// the quest is under way, so dialog dispatch reaches this handler via QuestEnv.QuestId regardless.
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

public sealed class _80279SpiritedThanks : QuestHandlerBase
{
    private const int QuestIdConst = 80279;
    private const int XuraboNpc    = 831117;
    private const int KubuNpc      = 831350;

    public _80279SpiritedThanks(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(XuraboNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(KubuNpc).OnQuestStart.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (env.TargetId != XuraboNpc) return false;
            return dialog == DialogAction.QUEST_SELECT
                ? await SendQuestDialogAsync(conn, targetObjId, 1011, ct)
                : await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && env.TargetId == KubuNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, reward: true, sameNpc: true, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && env.TargetId == KubuNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
