// Port of Java data/scripts/system/handlers/quest/inggison/_11289VeillesGift.java.
// Talk to 799038 to start (no item; using the gift item shows the same offer page too); handing over
// 182213147 (an explicit item-existence check, distinct from the quest_data.xml collect-items list —
// same idiom as morheim._2477ADishForDukar) removes it and flips straight to reward; turn in back at
// 799038.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Inggison;

public sealed class _11289VeillesGift : QuestHandlerBase
{
    private const int QuestIdConst = 11289;
    private const int TurnInNpc = 799038;
    private const int GiftItem  = 182213147;

    private readonly IItemDao _itemDao;

    public _11289VeillesGift(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(GiftItem, QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != GiftItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is not null && entry.Status != QuestStatus.NONE) return false;
        await conn.SendAsync(new SM_DIALOG_WINDOW(0, 4, QuestId), ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == TurnInNpc)
        {
            if (dialog != DialogAction.QUEST_SELECT || entry.GetVar(0) != 0) return false;

            var item = player.Inventory.FindByItemId(GiftItem);
            if (item is null || item.Count < 1) return false;

            await RemoveQuestItemAsync(player, conn, _itemDao, GiftItem, 1, ct);
            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
