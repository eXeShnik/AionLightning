// Port of Java data/scripts/system/handlers/quest/greater_stigma/_11551TentoOneOdds.java (zhkchi).
// Repeatable abyss-kill daily: accept at 205531, then 10 player kills in worlds 300350000/
// 300360000 (Java defaultOnKillRankedEvent(0, 10, true)) flip straight to REWARD; turn in at
// 205531. Skip vs Java: qs.canRepeat() (daily-repeat/cooldown) isn't ported (no repeat-tracking
// infra yet), approximated as "no active entry" like the rest of this port (see
// QuestEngine.ComputeNearbyQuests) — completable once, not re-takable after COMPLETE.
// Java bug worked around: the accept action here is QUEST_ACCEPT_SIMPLE, which the ported
// QuestHandlerBase.SendQuestStartDialogAsync doesn't handle (it only recognizes QUEST_ACCEPT/
// QUEST_ACCEPT_1/QUEST_REFUSE*); ported directly via StartMissionAsync + CloseDialogWindowAsync
// (Java's own sendQuestStartDialog(QUEST_ACCEPT_SIMPLE) path: start the quest, close the window).
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

namespace Quest.GreaterStigma;

public sealed class _11551TentoOneOdds : QuestHandlerBase
{
    private const int QuestIdConst = 11551;
    private const int StartNpc     = 205531;
    private const int WorldId1     = 300350000;
    private const int WorldId2     = 300360000;

    public _11551TentoOneOdds(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterKillInWorld(WorldId1, QuestId);
        engine.RegisterKillInWorld(WorldId2, QuestId);
    }

    public override async ValueTask<bool> OnPlayerKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var < 9)
        {
            await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
            return true;
        }
        if (var == 9)
        {
            await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId != StartNpc) return false;

        if (entry is null)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
