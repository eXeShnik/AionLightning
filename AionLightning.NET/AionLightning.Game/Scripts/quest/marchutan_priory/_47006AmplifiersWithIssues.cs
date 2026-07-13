// Port of Java data/scripts/system/handlers/quest/marchutan_priory/_47006AmplifiersWithIssues.java
// (Cheatkiller). Kill 217175 x5 (var 0->5), turn in at 799872. Structurally identical to
// _47003AGlobalProblem (same zone, same mentor-gated NPC gap — see that file's header for why
// 700972's mentor-escort branch is omitted).
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

namespace Quest.MarchutanPriory;

public sealed class _47006AmplifiersWithIssues : QuestHandlerBase
{
    private const int QuestIdConst = 47006;
    private const int MentorNpc    = 700972; // group2/mentor-gated NPC — no reachable dialog case (see _47003AGlobalProblem)
    private const int TurnInNpc    = 799872;
    private const int KillNpc      = 217175;

    public _47006AmplifiersWithIssues(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(MentorNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KillNpc).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, KillNpc, startVar: 0, endVar: 5, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId == 0 && dialog == DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, 0, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 5)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await DefaultCloseDialogAsync(env, conn, 5, 5, reward: true, sameNpc: true, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
