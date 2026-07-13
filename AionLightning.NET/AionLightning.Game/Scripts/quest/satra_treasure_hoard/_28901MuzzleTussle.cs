// Port of Java data/scripts/system/handlers/quest/satra_treasure_hoard/_28901MuzzleTussle.java
// (Ritsu). Elyos counterpart of _18901PunishingthePunisher: start at 800332 (OnQuestStart only),
// collector 205864 (OnTalk); killing 219299 once while START flips straight to REWARD. Java's
// onKillEvent re-resolves targetId via `((Npc) env.getVisibleObject()).getNpcId()` right after
// reading env.getTargetId() — redundant (env.TargetId already reflects the kill target npc here),
// so it's dropped like in the other satra_treasure_hoard ports.
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

public sealed class _28901MuzzleTussle : QuestHandlerBase
{
    private const int QuestIdConst = 28901;
    private const int StartNpc     = 800332;
    private const int CollectorNpc = 205864;
    private const int KillNpc      = 219299;

    public _28901MuzzleTussle(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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
