// Port of Java data/scripts/system/handlers/quest/satra_treasure_hoard/_18903GatesofSatra.java
// (Ritsu). Start at 800330 (OnQuestStart only), collector 205843 (OnTalk). Two independent kill
// counters: var 0 for 219297, var 1 for 219298, each bumped 0 -> 1 once. DefaultOnKillEventAsync
// only tracks var index 0, so the dual-index span check is reproduced manually via
// ChangeQuestStepAsync (matches Java's defaultOnKillEvent(env, npcId, 0, 1, varIndex) 5-arg form).
// Java bug: the collector's completion check only tests var 0 == 1 (the 219297 counter) — killing
// 219298 (var 1) is tracked but never required to finish the quest. Ported as-is.
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

public sealed class _18903GatesofSatra : QuestHandlerBase
{
    private const int QuestIdConst = 18903;
    private const int StartNpc     = 800330;
    private const int CollectorNpc = 205843;
    private const int KillNpc1     = 219297;
    private const int KillNpc2     = 219298;

    public _18903GatesofSatra(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(CollectorNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KillNpc1).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(KillNpc2).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not { Status: QuestStatus.START }) return false;

        switch (env.TargetId)
        {
            case KillNpc1:
                if (entry.GetVar(0) != 0) return false;
                await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                return true;
            case KillNpc2:
                if (entry.GetVar(1) != 0) return false;
                await ChangeQuestStepAsync(conn, entry, 1, 1, toReward: false, ct);
                return true;
            default:
                return false;
        }
    }

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
            {
                entry.SetVar(0, entry.GetVar(0) + 1);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != CollectorNpc) return false;
            return dialog switch
            {
                DialogAction.QUEST_SELECT        => await SendQuestDialogAsync(conn, targetObjId, 5, ct),
                DialogAction.SELECT_QUEST_REWARD => await SendQuestDialogAsync(conn, targetObjId, 5, ct),
                _                                 => await SendQuestEndDialogAsync(env, conn, ct),
            };
        }

        return false;
    }
}
