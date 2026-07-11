// Port of Java data/scripts/system/handlers/quest/morheim/_2493BringingUpTayga.java.
// Start at Ipoderr (204325); at Purra (204435) the SET_SUCCEED dialog (fired once the client-side
// interaction finishes) flips straight to REWARD; turn in at Ipoderr.
// Skip vs Java: the target npc's scheduleRespawn()/onDelete() (a despawn/respawn visual on the
// object handled) is omitted — no NPC AI/controller subsystem in this port yet (same skip as the
// Poeta golden exemplar's Sleeping Elder); the quest var transition still completes normally.
// Java's registerOnLogOut(questId) has no matching onLogOutEvent override in the source file (a
// dead registration in the original), so it is not ported.
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

namespace Quest.Morheim;

public sealed class _2493BringingUpTayga : QuestHandlerBase
{
    private const int QuestIdConst = 2493;
    private const int IpoderrNpc   = 204325;
    private const int PurraNpc     = 204435;

    public _2493BringingUpTayga(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(IpoderrNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(IpoderrNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PurraNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != IpoderrNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == PurraNpc)
        {
            int var = entry.GetVar(0);
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SET_SUCCEED)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == IpoderrNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
