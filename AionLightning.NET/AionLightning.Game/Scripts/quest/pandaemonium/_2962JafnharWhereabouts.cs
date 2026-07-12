// Port of Java data/scripts/system/handlers/quest/pandaemonium/_2962JafnharWhereabouts.java.
// Accept at 204253; 2-step chain (278067 var0 0->1, 278137 var0 1->2); at 204253 (var0==2) two
// alternate dialog branches (SETPRO3/SETPRO4) both flip to REWARD picking one of two reward tiers;
// turn in at 204253.
// Java bug fixed: the reward tier was tracked in a per-quest-handler instance field (`int
// rewIdex`, default 0) shared by every player, and both SETPRO3/SETPRO4 write the *same* var0
// value (2 -> 2) — so the persisted quest state can't tell the branches apart either, and two
// concurrent players picking different tiers would clobber each other's reward. This port persists
// the choice into quest var 1 instead (0 for SETPRO3, 1 for SETPRO4), matching the same fix
// pattern as pandaemonium/_2920ElementaryMyDearDaeva.cs.
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

public sealed class _2962JafnharWhereabouts : QuestHandlerBase
{
    private const int QuestIdConst = 2962;
    private const int StartNpc = 204253;
    private const int StepOneNpc = 278067;
    private const int StepTwoNpc = 278137;

    public _2962JafnharWhereabouts(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StepOneNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StepTwoNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == StepOneNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return entry.GetVar(0) == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == StepTwoNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return entry.GetVar(0) == 1 && await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == StartNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return entry.GetVar(0) == 2 && await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    case DialogAction.SELECT_ACTION_1694:
                        return await SendQuestDialogAsync(conn, targetObjId, 1694, ct);
                    case DialogAction.SELECT_ACTION_1779:
                        return await SendQuestDialogAsync(conn, targetObjId, 1779, ct);
                    case DialogAction.SETPRO3:
                        entry.SetVar(1, 0);
                        return await DefaultCloseDialogAsync(env, conn, 2, 2, reward: true, sameNpc: true, ct);
                    case DialogAction.SETPRO4:
                        entry.SetVar(1, 1);
                        return await DefaultCloseDialogAsync(env, conn, 2, 2, reward: true, sameNpc: true, ct);
                    default:
                        return false;
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != StartNpc) return false;
            int rewardIndex = entry.GetVar(1);
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 5 + rewardIndex, ct);
            return await SendQuestEndDialogAsync(env, conn, rewardIndex, ct);
        }
        return false;
    }

    /// <summary>Java sendQuestEndDialog(env, reward): explicit reward-tier override, bypassing
    /// whatever index the client's SELECT_QUEST_REWARD packet carried.</summary>
    private async ValueTask<bool> SendQuestEndDialogAsync(QuestEnv env, GsClientConnection conn, int rewardIndex, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.REWARD) return false;
        if (DialogActionLookup.FromId(env.DialogId) != DialogAction.SELECT_QUEST_REWARD) return false;
        return await FinishQuestAsync(conn, env.Player, rewardIndex, ct);
    }
}
