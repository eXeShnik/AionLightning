// Port of Java data/scripts/system/handlers/quest/pernon/_28826FreetoaGoodHome.java (zhkchi).
// Talk to any of 830662/830663/830521 to start; use object 730525 (dialog 2375, then var 0->0
// reward at dialog 5) to flip to REWARD; turn in at 730525.
// Skip vs Java: qs.canRepeat() (repeatable-quest gate) is approximated as "no active entry", same
// as the rest of this port (see QuestEngine.ComputeNearbyQuests).
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

namespace Quest.Pernon;

public sealed class _28826FreetoaGoodHome : QuestHandlerBase
{
    private const int QuestIdConst = 28826;
    private const int StartNpc1    = 830663;
    private const int StartNpc2    = 830521;
    private const int StartNpc3    = 830662;
    private const int BasketNpc    = 730525;

    public _28826FreetoaGoodHome(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc1).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc1).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc2).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc2).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc3).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc3).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BasketNpc).OnTalk.Add(QuestId);
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
            if (targetId is StartNpc1 or StartNpc2 or StartNpc3)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == BasketNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == BasketNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
