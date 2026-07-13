// Port of Java data/scripts/system/handlers/quest/rider_quests/_3547EnemyHarassingTheElim.java (pralinka).
// Talk to 730024 to start; Voice of Arbolu (204647, var0 0->1, then a collect-item check that flips
// straight to REWARD); turn in back at Voice of Arbolu.
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

namespace Quest.RiderQuests;

public sealed class _3547EnemyHarassingTheElim : QuestHandlerBase
{
    private const int QuestIdConst = 3547;
    private const int StartNpc = 730024;
    private const int VoiceNpc = 204647;

    private readonly IItemDao _itemDao;

    public _3547EnemyHarassingTheElim(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(VoiceNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry       = env.Player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var0 = entry.GetVar(0);
            if (targetId == VoiceNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 0) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.QUEST_SELECT && var0 == 1) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM_SIMPLE)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 1, reward: true, checkOkId: 5, checkFailId: 0, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == VoiceNpc) return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
