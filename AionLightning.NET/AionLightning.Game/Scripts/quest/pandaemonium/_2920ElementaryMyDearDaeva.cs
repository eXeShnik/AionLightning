// Port of Java data/scripts/system/handlers/quest/pandaemonium/_2920ElementaryMyDearDaeva.java.
// Accept at Deyla (204141); SETPRO11/SETPRO12 pick one of two reward tiers, shown/finished via
// the shared reward-tier dialog helper below (Java: SELECT_QUEST_REWARD/USE_OBJECT show page
// 5+choice, SELECTED_QUEST_REWARDn/NOREWARD actually complete with that tier — same pattern as
// morheim/_2303DaevaWheresMyHerb.cs).
// Java bug fixed: the Java handler stores the picked tier in a per-quest-handler instance field
// (`private int choice`) shared by every player using this singleton, and its own
// `changeQuestStep(env, 0, 0, true)` call never actually records the choice in the persisted quest
// var (value 0, a no-op write) — so two concurrent players picking different tiers would clobber
// each other's reward, and the choice couldn't be re-derived after a relog. This port persists the
// choice into quest var 0 (1 for SETPRO11, 2 for SETPRO12) instead, so it survives per-player state
// and concurrent players can't interfere.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Pandaemonium;

public sealed class _2920ElementaryMyDearDaeva : QuestHandlerBase
{
    private const int QuestIdConst = 2920;
    private const int DeylaNpc = 204141;

    public _2920ElementaryMyDearDaeva(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(DeylaNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(DeylaNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId != DeylaNpc) return false;

        if (entry is null)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.SETPRO1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                case DialogAction.SETPRO2:
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                case DialogAction.SETPRO11:
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                case DialogAction.SETPRO12:
                    await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 6, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogWithTierAsync(env, conn, entry.GetVar(0) - 1, ct);

        return false;
    }

    /// <summary>Java sendQuestEndDialog(env, reward): SELECT_QUEST_REWARD/USE_OBJECT shows the
    /// tier-confirm page 5+tier; SELECTED_QUEST_REWARDn/NOREWARD actually completes with it.</summary>
    private async ValueTask<bool> SendQuestEndDialogWithTierAsync(QuestEnv env, GsClientConnection conn, int rewardIndex, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.REWARD) return false;
        int targetObjId = env.Target?.ObjectId ?? 0;
        int dialogId = env.DialogId;

        if (dialogId >= (int)DialogAction.SELECTED_QUEST_REWARD1 && dialogId <= (int)DialogAction.SELECTED_QUEST_NOREWARD)
        {
            if (!await FinishQuestAsync(conn, env.Player, rewardIndex, ct)) return false;
            await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
            return true;
        }
        if (dialogId == (int)DialogAction.SELECT_QUEST_REWARD || dialogId == (int)DialogAction.USE_OBJECT)
            return await SendQuestDialogAsync(conn, targetObjId, 5 + rewardIndex, ct);
        return false;
    }
}
