// Port of Java data/scripts/system/handlers/quest/sarpan/_11524FieldRepairs.java (zhkchi).
// Talk to Reeva (205533) to start (QUEST_ACCEPT_SIMPLE — no item); Auxiliary Piece-fetch chain at
// Ottleigh (205536, var 0->1) then Krokim (205562, var 1->2); back at Reeva (var 2, SELECT_QUEST_REWARD
// flips to REWARD, page 5); turn in at Reeva.
// Java's outer switch has no `break` between cases sharing the same block (harmless — each case
// returns before falling through in every reachable path here), ported with C#'s required explicit
// control flow instead.
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

namespace Quest.Sarpan;

public sealed class _11524FieldRepairs : QuestHandlerBase
{
    private const int QuestIdConst = 11524;
    private const int ReevaNpc      = 205533;
    private const int OttleighNpc   = 205536;
    private const int KrokimNpc     = 205562;

    public _11524FieldRepairs(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(ReevaNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(ReevaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(OttleighNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KrokimNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != ReevaNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == OttleighNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == KrokimNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == ReevaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD && var == 2)
                {
                    await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == ReevaNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
