// Port of Java data/scripts/system/handlers/quest/marchutan_priory/_47003AGlobalProblem.java
// (Cheatkiller). Kill 217173 x5 (var 0->5), turn in at 799872.
// Skip vs Java: talking to 700971 while in a mentor group despawns it and respawns 217173 at its
// position (a mentor-escort "start the fight" trigger) — requires isInGroup2/isMentor +
// GROUP_MAX_DISTANCE plus Npc.getController().scheduleRespawn()/onDelete(), none of which exist in
// this port (same pre-existing mentor gap as MentorMonsterHuntHandler; no NPC controller/AI
// subsystem either). Omitted entirely — 700971 is still registered for OnTalk parity with Java, but
// with no reachable branch the block never returns, matching Java's own behavior for a non-grouped
// player (falls through to the bottom `false`).
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

public sealed class _47003AGlobalProblem : QuestHandlerBase
{
    private const int QuestIdConst = 47003;
    private const int MentorNpc    = 700971; // group2/mentor-gated NPC — no reachable dialog case (see file header)
    private const int TurnInNpc    = 799872;
    private const int KillNpc      = 217173;

    public _47003AGlobalProblem(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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
