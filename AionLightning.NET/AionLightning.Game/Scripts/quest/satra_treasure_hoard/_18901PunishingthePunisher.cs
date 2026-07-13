// Port of Java data/scripts/system/handlers/quest/satra_treasure_hoard/_18901PunishingthePunisher.java
// (Ritsu). Start at 800332 (OnQuestStart only), turn-in/collector 205842 (OnTalk); killing 219299
// once while START flips straight to REWARD via the two-step updateQuestStatus Java uses (var bump,
// then status flip, each persisted separately) — DefaultOnKillEventAsync's reward-flip overload
// reproduces both writes. The START-status var==1 branch at 205842 is unreachable in Java too: by
// the time var reaches 1 the kill handler has already moved status to REWARD, so it's ported as-is
// for fidelity rather than dropped.
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

public sealed class _18901PunishingthePunisher : QuestHandlerBase
{
    private const int QuestIdConst = 18901;
    private const int StartNpc     = 800332;
    private const int CollectorNpc = 205842;
    private const int KillNpc      = 219299;

    public _18901PunishingthePunisher(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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
