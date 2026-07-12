// Port of Java data/scripts/system/handlers/quest/katalam/_22854KeepWhatYouKill.java (Eloann).
// Asmodian counterpart of 12854 ObjectiveBasedBaseObjectives. Talk to 800529 (Vard) to start;
// 800531 (Bilveo) bumps var 0 on each SETPRO1 talk (no upper bound in Java — a repeatable relay
// counter); back at Vard, SELECT_QUEST_REWARD flips straight to REWARD (var forced to 3) and shows
// page 5; turn in at Vard.
// Java bug fixed: the outer switch(targetId) had no break after the 800531 case, so any
// non-QUEST_SELECT/SETPRO1 dialog at Bilveo fell through into the Vard (800529) branch using the
// same targetId==800531 context. Ported as independent if-blocks instead.
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

namespace Quest.Katalam;

public sealed class _22854KeepWhatYouKill : QuestHandlerBase
{
    private const int QuestIdConst = 22854;
    private const int VardNpc      = 800529;
    private const int BilveoNpc    = 800531;

    public _22854KeepWhatYouKill(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(VardNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(VardNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BilveoNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != VardNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == BilveoNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    entry.SetVar(0, entry.GetVar(0) + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }
            if (targetId == VardNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: true, ct);
                    return await SendQuestEndDialogAsync(env, conn, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == VardNpc)
        {
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
