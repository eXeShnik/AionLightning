// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41504Groundbreaking.java (mr.madison).
// Talk to 205935 to start; hand in 5x collected item 182212516 (quest_data.xml collect_items,
// dropped by 218221) at 205891 to receive quest item 182212517 and advance var 0->1; using that
// item while inside GIANT_CRATER_600030000 flips 1->2 to REWARD; turn in at 205887.
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

public sealed class _41504Groundbreaking : QuestHandlerBase
{
    private const int QuestIdConst = 41504;
    private const int StartNpc     = 205935;
    private const int CollectNpc   = 205891;
    private const int TurnInNpc    = 205887;
    private const int ItemId       = 182212517;
    private const string CraterZone = "GIANT_CRATER_600030000";

    private readonly IItemDao _itemDao;

    public _41504Groundbreaking(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CollectNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(ItemId, QuestId);
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
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == CollectNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, reward: false,
                    checkOkId: 10000, checkFailId: 10001, ItemId, 1, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;
        if (itemId != ItemId || !player.CurrentZones.Contains(CraterZone)) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
        return true;
    }
}
