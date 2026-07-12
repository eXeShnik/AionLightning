// Port of Java data/scripts/system/handlers/quest/reshanta/_1705Defeat6thRankAsmodianSoldiers.java (Hilgert/vlog).
// Kill 10 opposing-race players at abyss rank 4 (Java AbyssRankEnum.GRADE6_SOLDIER,
// registerOnKillRanked) anywhere in Reshanta to flip to REWARD; turn in at 278503.
// Skip vs Java: qs.canRepeat() (daily-repeat) isn't ported - approximated as "no active entry"
// like the rest of this port, so this completes once per character.
// Bespoke vs the rest of this port: Java's onKillRankedEvent has no world scoping at all (any
// PvP kill of the right rank counts, anywhere) - this port has no rank-keyed dispatcher, so it
// reuses the already-wired kill_in_world hook (QuestEngine.RegisterKillInWorld/OnPlayerKillAsync,
// PvP Phase 1) scoped to Reshanta's own world id (400010000, the zone these quests are handed out
// in) and checks the victim's AbyssRank itself in OnPlayerKillAsync. Player.AbyssRank only models
// the 9 soldier grades (AbyssRankService caps at rank 9) - Java's Officer/General tiers (ids
// 10-18) have no representation in this port, so the sibling Officer/General quests are deferred.
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

namespace Quest.Reshanta;

public sealed class _1705Defeat6thRankAsmodianSoldiers : QuestHandlerBase
{
    private const int QuestIdConst   = 1705;
    private const int StartNpc       = 278503;
    private const int RequiredGrade  = 4;
    private const int ReshantaWorldId = 400010000;

    public _1705Defeat6thRankAsmodianSoldiers(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterKillInWorld(ReshantaWorldId, QuestId);
    }

    public override async ValueTask<bool> OnPlayerKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.Target is not Player victim || victim.AbyssRank != RequiredGrade) return false;

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
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId != StartNpc) return false;

        if (entry is null)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
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
