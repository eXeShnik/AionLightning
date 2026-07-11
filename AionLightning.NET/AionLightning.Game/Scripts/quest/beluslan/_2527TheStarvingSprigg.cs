// Port of Java data/scripts/system/handlers/quest/beluslan/_2527TheStarvingSprigg.java (vlog).
// Talk to Gark (204811) to start and turn in; collect-check consumes the Food Pouches
// (quest_data.xml collect_item list) on CHECK_USER_HAS_QUEST_ITEM. The Food Pouch object
// (700328) use is a no-op acknowledgement in Java (returns true without state change).
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

namespace Quest.Beluslan;

public sealed class _2527TheStarvingSprigg : QuestHandlerBase
{
    private const int QuestIdConst  = 2527;
    private const int GarkNpc       = 204811;
    private const int FoodPoucheObj = 700328;

    private readonly IItemDao _itemDao;

    public _2527TheStarvingSprigg(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GarkNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(GarkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FoodPoucheObj).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId == GarkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == GarkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 0, true, 5, 2716, ct);
                return false;
            }
            if (targetId == FoodPoucheObj)
                return dialog == DialogAction.USE_OBJECT;
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == GarkNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
