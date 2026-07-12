// Port of Java data/scripts/system/handlers/quest/pandaemonium/_2922FascinatingGift.java.
// Accept at 204261; SETPRO10/SETPRO20 pick one of two paths (var0=10 -> turn in at 798058 with
// reward tier 0; var0=20 -> turn in at 204108 with reward tier 1, an explicit override of
// whatever the client's SELECT_QUEST_REWARD carried — see SendQuestEndDialogAsync(env, tier)
// below, mirroring Java's sendQuestEndDialog(env, reward) overload).
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

public sealed class _2922FascinatingGift : QuestHandlerBase
{
    private const int QuestIdConst = 2922;
    private const int StartNpc = 204261;
    private const int PathANpc = 798058;
    private const int PathBNpc = 204108;

    public _2922FascinatingGift(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PathANpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PathBNpc).OnTalk.Add(QuestId);
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
            if (targetId != StartNpc) return false;
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return entry.GetVar(0) == 0 && await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
                case DialogAction.SELECT_ACTION_1012:
                    return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
                case DialogAction.SELECT_ACTION_1097:
                    return await SendQuestDialogAsync(conn, targetObjId, 1097, ct);
                case DialogAction.SETPRO10:
                    await ChangeQuestStepAsync(conn, entry, 0, 10, toReward: true, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                case DialogAction.SETPRO20:
                    await ChangeQuestStepAsync(conn, entry, 0, 20, toReward: true, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == PathANpc && entry.GetVar(0) == 10)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            if (targetId == PathBNpc && entry.GetVar(0) == 20)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                return await SendQuestEndDialogAsync(env, conn, 1, ct);
            }
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
