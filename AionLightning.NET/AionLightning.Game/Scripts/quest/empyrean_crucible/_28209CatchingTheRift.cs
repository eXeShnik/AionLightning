// Port of Java data/scripts/system/handlers/quest/empyrean_crucible/_28209CatchingTheRift.java (Kamui).
// Asmodian mirror of 18209 ARiftInTheSpaceTwineContinuum, daily-repeatable at Anja (205321). Kill mob
// 217819 four times (var1 0->4), a fifth kill of 217819 flips var0 to 1, then a kill of mob 218185
// completes to REWARD (var2 1); turn in at the same npc, with a SELECT_QUEST_REWARD confirmation
// page (5) before the actual reward grant.
// Skip vs Java: qs.canRepeat() (daily-repeat) isn't ported - approximated as "no active entry" like
// the rest of this port (see reshanta _1710Defeat1thRankAsmodianSoldiers), so this completes once per
// character instead of resetting daily.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.EmpyreanCrucible;

public sealed class _28209CatchingTheRift : QuestHandlerBase
{
    private const int QuestIdConst = 28209;
    private const int StartNpc     = 205321; // Anja
    private const int Mob1         = 217819;
    private const int Mob2         = 218185;

    public _28209CatchingTheRift(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Mob1).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Mob2).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry       = env.Player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }
        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int targetId = env.TargetId;
        int var  = entry.GetVar(0);
        int var1 = entry.GetVar(1);

        if (var == 0 && var1 < 4)
        {
            if (targetId != Mob1) return false;
            await ChangeQuestStepAsync(conn, entry, 1, var1 + 1, toReward: false, ct);
            return true;
        }
        if (var == 0 && var1 == 4)
        {
            if (targetId != Mob1) return false;
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
            return true;
        }
        if (var == 1 && targetId == Mob2)
        {
            await ChangeQuestStepAsync(conn, entry, 2, 1, toReward: true, ct);
            return true;
        }
        return false;
    }
}
