// Port of Java data/scripts/system/handlers/quest/hero/_23517ChainofCommand.java (Evil_dnk).
// Kill 44 opposing-race players ranked STAR1..STAR5 OFFICER in Katalam (world 600050000,
// registerOnKillRanked(STAR1..STAR5_OFFICER), defaultOnKillRankedEvent(env, 0, 44, true)) to flip to
// REWARD; turn in at 800529.
// Bespoke vs the rest of this port (per _1702 precedent): no rank-keyed dispatcher exists, so this
// reuses the kill_in_world hook (QuestEngine.RegisterKillInWorld/OnPlayerKillAsync) scoped to Java's
// own zone check (KATALAM_600050000) and checks the victim's AbyssRank itself against the officer
// band (ranks 10-14).
// note: Player.AbyssRank now models the full 1-18 rank range - soldier ranks (1-9) from AP, officer/
// general ranks (10-18) from Glory Points via AbyssRankService.AddGloryPoints. The STAR1..STAR5
// OFFICER tier is ranks 10-14, so victim.AbyssRank reaches that band once the player accrues enough
// GP (see AbyssRankService.GpThresholds); this quest can now complete.
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

namespace Quest.Hero;

public sealed class _23517ChainofCommand : QuestHandlerBase
{
    private const int QuestIdConst    = 23517;
    private const int StartNpc        = 800529;
    private const int OfficerRankMin  = 10;  // AbyssRankEnum.STAR1_OFFICER
    private const int OfficerRankMax  = 14;  // AbyssRankEnum.STAR5_OFFICER
    private const int KillCount       = 44;  // defaultOnKillRankedEvent endVar
    private const int KatalamWorldId  = 600050000;

    public _23517ChainofCommand(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterKillInWorld(KatalamWorldId, QuestId);
    }

    public override async ValueTask<bool> OnPlayerKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.Target is not Player victim || victim.AbyssRank < OfficerRankMin || victim.AbyssRank > OfficerRankMax) return false;

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
