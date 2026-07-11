// Port of Java data/scripts/system/handlers/quest/heiron/_1612LepharistSecrets.java.
// Talk to the start NPC (204530); use the quest object (700352) three times (var 0->1->2->3),
// then a fourth use flips to REWARD; turn in at the start NPC.
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

namespace Quest.Heiron;

public sealed class _1612LepharistSecrets : QuestHandlerBase
{
    private const int QuestIdConst = 1612;
    private const int StartNpc     = 204530;
    private const int ObjectNpc    = 700352;

    public _1612LepharistSecrets(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ObjectNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
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
            if (targetId == ObjectNpc && dialog == DialogAction.USE_OBJECT)
            {
                int var = entry.GetVar(0);
                return var switch
                {
                    0 or 1 or 2 => await UseQuestObjectAsync(env, conn, var, var + 1, false, false, ct),
                    3 => await UseQuestObjectAsync(env, conn, 3, 3, true, false, ct),
                    _ => false,
                };
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
