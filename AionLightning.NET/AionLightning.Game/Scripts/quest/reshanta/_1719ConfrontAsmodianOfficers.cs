// Port of Java data/scripts/system/handlers/quest/reshanta/_1719ConfrontAsmodianOfficers.java (vlog).
// [Group] Kill 10 opposing-race players ranked STAR1_OFFICER (Java registerOnKillRanked(STAR1_OFFICER),
// defaultOnKillRankedEvent(env, 0, 10, true)) to flip to REWARD; report to Michalis (278501).
// Skip vs Java: qs.canRepeat() (daily-repeat) isn't ported - approximated as "no active entry"
// like the rest of this port, so this completes once per character.
// Bespoke vs the rest of this port (per _1702 precedent): no rank-keyed dispatcher exists, so this
// reuses the kill_in_world hook (QuestEngine.RegisterKillInWorld/OnPlayerKillAsync) scoped to
// Reshanta's own world id (400010000) and checks the victim's AbyssRank itself.
// note: Player.AbyssRank now models the full 1-18 rank range - soldier ranks (1-9) from AP, officer/
// general ranks (10-18) from Glory Points via AbyssRankService.AddGloryPoints. STAR1_OFFICER is rank
// 10, so victim.AbyssRank reaches it once the player accrues enough GP (see
// AbyssRankService.GpThresholds); this quest can now complete.
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

public sealed class _1719ConfrontAsmodianOfficers : QuestHandlerBase
{
    private const int QuestIdConst    = 1719;
    private const int StartNpc        = 278501;
    private const int RequiredGrade   = 10;  // AbyssRankEnum.STAR1_OFFICER
    private const int KillCount       = 10;  // defaultOnKillRankedEvent endVar
    private const int ReshantaWorldId = 400010000;

    public _1719ConfrontAsmodianOfficers(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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
        if (var < KillCount - 1)
        {
            await ChangeQuestStepAsync(conn, entry, var, var + 1, toReward: false, ct);
            return true;
        }
        if (var == KillCount - 1)
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
