// Port of Java data/scripts/system/handlers/quest/beshmundir/_30301TrackingSupplies.java (Gigi).
// Identical shape to _30201SuppliesParty at a different NPC/item: talk to 799225 to start, hand in
// the tracking supplies item (182209701) via CHECK_USER_HAS_QUEST_ITEM to flip straight to REWARD.
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

namespace Quest.Beshmundir;

public sealed class _30301TrackingSupplies : QuestHandlerBase
{
    private const int QuestIdConst = 30301;
    private const int StartNpc     = 799225;
    private const int SupplyItemId = 182209701;

    private readonly IItemDao _itemDao;

    public _30301TrackingSupplies(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (env.TargetId != StartNpc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);

            if (env.DialogId == (int)DialogAction.CHECK_USER_HAS_QUEST_ITEM && entry.GetVar(0) == 0)
            {
                var item = player.Inventory.FindByItemId(SupplyItemId);
                if (item is not null && item.Count > 0)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, SupplyItemId, 1, ct);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
            }

            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return await SendQuestEndDialogAsync(env, conn, ct);
    }
}
