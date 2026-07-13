// Port of Java data/scripts/system/handlers/quest/argent_manor/_30402TheRescue.java (Ritsu).
// Talk to the start npc (799535) to accept; kill 217242 (var 0->1); report to 799537 (var 1,
// SET_SUCCEED flips to REWARD); turn in back at 799535.
// Java bug: the START-status switch on 799537 has no break after QUEST_SELECT, falling through
// into SET_SUCCEED's unconditional defaultCloseDialog(env, 1, 1, true, false) when var != 1 - but
// that call re-checks var == 1 internally and returns false in that case, so the fallthrough is
// behaviorally a no-op; expressed here as separate `when`-guarded cases with the same outcome.
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

namespace Quest.ArgentManor;

public sealed class _30402TheRescue : QuestHandlerBase
{
    private const int QuestIdConst = 30402;
    private const int StartNpc     = 799535;
    private const int ReportNpc    = 799537;
    private const int MobNpc       = 217242;

    public _30402TheRescue(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ReportNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MobNpc).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, MobNpc, startVar: 0, endVar: 1, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
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

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId != ReportNpc) return false;
            return dialog switch
            {
                DialogAction.QUEST_SELECT when var == 1 => await SendQuestDialogAsync(conn, targetObjId, 1352, ct),
                DialogAction.SET_SUCCEED                 => await DefaultCloseDialogAsync(env, conn, 1, 1, reward: true, sameNpc: false, ct),
                _                                         => false,
            };
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
