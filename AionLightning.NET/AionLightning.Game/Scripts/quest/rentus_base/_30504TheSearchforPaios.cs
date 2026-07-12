// Port of Java data/scripts/system/handlers/quest/rentus_base/_30504TheSearchforPaios.java
// (Ritsu). Start at Lition (205438); use quest object 701098 (var0->1, dieObject requested);
// report to Paios (799536, var1->2, reward); turn in at Lition.
// Skip vs Java: useQuestObject's dieObject=true (Java kills the target npc via its AI controller)
// has no equivalent (no NPC AI/controller death-trigger infra in this port yet) — the step
// transition still applies, see QuestHandlerBase.UseQuestObjectAsync's own doc comment.
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

namespace Quest.RentusBase;

public sealed class _30504TheSearchforPaios : QuestHandlerBase
{
    private const int QuestIdConst = 30504;
    private const int StartNpc     = 205438;
    private const int ObjectNpc    = 701098;
    private const int PaiosNpc     = 799536;

    public _30504TheSearchforPaios(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ObjectNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PaiosNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == PaiosNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, reward: true, sameNpc: false, ct);
                return false;
            }

            if (targetId == ObjectNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await UseQuestObjectAsync(env, conn, 0, 1, reward: false, dieObject: true, ct);
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
