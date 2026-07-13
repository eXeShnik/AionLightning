// Port of Java data/scripts/system/handlers/quest/argent_manor/_30409StoriesofthePast.java (Ritsu).
// Talk to the start npc (799539) to accept; report to 798116 (var 0->1); back at 799539
// (SELECT_QUEST_REWARD flips to REWARD, page 5); turn in at 799539.
// Java bug: the START-status switch on 799539 has no break after `QUEST_SELECT`, so talking there
// with any var != 1 (e.g. var 0, right after accepting) would fall through into
// SELECT_QUEST_REWARD's unconditional changeQuestStep(env, 1, 1, true) - unlike the 798116 case
// (whose fallthrough lands in defaultCloseDialog, which re-checks the step itself and is a no-op),
// this one has no internal guard and would wrongly jump straight to REWARD. Fixed by gating
// SELECT_QUEST_REWARD on var == 1 to match the evident intent (only reachable after dialog 2375).
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

public sealed class _30409StoriesofthePast : QuestHandlerBase
{
    private const int QuestIdConst = 30409;
    private const int StartNpc     = 799539;
    private const int ReportNpc    = 798116;

    public _30409StoriesofthePast(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ReportNpc).OnTalk.Add(QuestId);
    }

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
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            switch (targetId)
            {
                case ReportNpc:
                    return dialog switch
                    {
                        DialogAction.QUEST_SELECT when var == 0 => await SendQuestDialogAsync(conn, targetObjId, 1352, ct),
                        DialogAction.SETPRO1 when var == 0        => await DefaultCloseDialogAsync(env, conn, 0, 1, ct),
                        _                                          => false,
                    };
                case StartNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT when var == 1:
                            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                        case DialogAction.SELECT_QUEST_REWARD when var == 1:
                            await ChangeQuestStepAsync(conn, entry, 1, 1, toReward: true, ct);
                            return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                        default:
                            return false;
                    }
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != StartNpc) return false;
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
