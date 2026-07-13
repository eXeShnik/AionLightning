// Port of Java data/scripts/system/handlers/quest/satra_treasure_hoard/_18902PunishersPosture.java
// (Ritsu). Start at 800332 (OnQuestStart only), collector 205842 (OnTalk); killing 219300 once
// while START flips straight to REWARD (two-step var bump + status flip). Unlike _18901, the
// collector's own SELECT_QUEST_REWARD dialog action (not the kill) performs the var bump + REWARD
// flip here, and the REWARD-state turn-in calls SendQuestEndDialogAsync unconditionally instead of
// routing QUEST_SELECT/SELECT_QUEST_REWARD through page 5 first.
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

namespace Quest.SatraTreasureHoard;

public sealed class _18902PunishersPosture : QuestHandlerBase
{
    private const int QuestIdConst = 18902;
    private const int StartNpc     = 800332;
    private const int CollectorNpc = 205842;
    private const int KillNpc      = 219300;

    public _18902PunishersPosture(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(CollectorNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KillNpc).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, KillNpc, 0, reward: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != CollectorNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                entry.SetVar(0, entry.GetVar(0) + 1);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != CollectorNpc) return false;
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
