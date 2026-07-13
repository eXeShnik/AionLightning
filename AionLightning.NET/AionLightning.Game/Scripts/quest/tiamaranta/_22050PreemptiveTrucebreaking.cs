// Port of Java data/scripts/system/handlers/quest/tiamaranta/_22050PreemptiveTrucebreaking.java (nrg).
// Kill 10 opposing-race players at abyss rank 5 (Java AbyssRankEnum.GRADE5_SOLDIER,
// registerOnKillRanked, defaultOnKillRankedEvent(env, 0, 10, true)) to flip to REWARD; turn in at 205865.
// Skip vs Java: qs.canRepeat() (daily-repeat) isn't ported - approximated as "no active entry"
// like the rest of this port, so this completes once per character.
// Bespoke vs the rest of this port (per _1702 precedent): Java's onKillRankedEvent has no world
// scoping (any rank-5 PvP kill counts, anywhere) - this port has no rank-keyed dispatcher, so it
// reuses the already-wired kill_in_world hook (QuestEngine.RegisterKillInWorld/OnPlayerKillAsync)
// scoped to Tiamaranta's own world id (600030000, where NPC 205865 hands the quest out) and checks
// the victim's AbyssRank itself. GRADE5_SOLDIER (rank 5) is modeled, so this is completable now.
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

namespace Quest.Tiamaranta;

public sealed class _22050PreemptiveTrucebreaking : QuestHandlerBase
{
    private const int QuestIdConst      = 22050;
    private const int StartNpc          = 205865;
    private const int RequiredGrade     = 5;   // AbyssRankEnum.GRADE5_SOLDIER
    private const int KillCount         = 10;  // defaultOnKillRankedEvent endVar
    private const int TiamarantaWorldId = 600030000;

    public _22050PreemptiveTrucebreaking(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterKillInWorld(TiamarantaWorldId, QuestId);
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
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
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
