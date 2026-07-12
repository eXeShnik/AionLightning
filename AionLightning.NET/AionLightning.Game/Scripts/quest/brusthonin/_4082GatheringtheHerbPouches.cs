// Port of Java data/scripts/system/handlers/quest/brusthonin/_4082GatheringtheHerbPouches.java.
// Talk to 205190 to start (grants the gathering pouch 182209058); use the 3 herb objects
// (700430/700431/700432, flavor-only acknowledgements, var 0); hand in the quest_data.xml collect
// items at 205190 (Java QuestService.collectItemCheck(env, true), inlined per the _2332MeatyTreats
// precedent) to remove the pouch and complete.
// Skip vs Java: the item-use 3s SM_ITEM_USAGE_ANIMATION delay on the pouch is collapsed into an
// immediate success (no follow-up dialog in the original either — animation only).
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Brusthonin;

public sealed class _4082GatheringtheHerbPouches : QuestHandlerBase
{
    private const int QuestIdConst = 4082;
    private const int StartNpc     = 205190;
    private const int PouchItemId  = 182209058;
    private static readonly int[] _herbObjs = [700430, 700431, 700432];

    private readonly IItemDao _itemDao;

    public _4082GatheringtheHerbPouches(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        foreach (int obj in _herbObjs)
            engine.RegisterQuestNpc(obj).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(PouchItemId, QuestId);
    }

    public override ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != PouchItemId) return ValueTask.FromResult(false);
        var entry = player.Quests.Get(QuestId);
        if (entry is null) return ValueTask.FromResult(false);
        if (entry.GetVar(0) != 0) return ValueTask.FromResult(false);
        return ValueTask.FromResult(true);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            if (entry is null)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, PouchItemId, 1, ct))
                        return await SendQuestStartDialogAsync(env, conn, ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }

            if (entry.Status == QuestStatus.START)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (env.DialogId == (int)DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                {
                    if (await CollectItemCheckAsync(player, conn, ct))
                    {
                        await RemoveQuestItemAsync(player, conn, _itemDao, PouchItemId, 1, ct);
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.REWARD)
                return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }

        if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 0)
        {
            foreach (int obj in _herbObjs)
            {
                if (targetId == obj && dialog == DialogAction.USE_OBJECT) return true;
            }
        }
        return false;
    }

    /// <summary>Java QuestService.collectItemCheck(env, true) — checks every quest_data.xml collect
    /// item is present, then consumes them all, without any quest-state transition (unlike
    /// <see cref="QuestHandlerBase.CheckQuestItemsAsync"/>, which always advances a step).</summary>
    private async ValueTask<bool> CollectItemCheckAsync(Player player, GsClientConnection conn, CancellationToken ct)
    {
        var collectItems = Template?.CollectItems?.Items;
        if (collectItems is not { Count: > 0 }) return true;

        foreach (var req in collectItems)
        {
            var item = player.Inventory.FindByItemId(req.ItemId);
            if (item is null || item.Count < req.Count) return false;
        }

        var partiallyConsumed = new List<Item>();
        foreach (var req in collectItems)
        {
            var item = player.Inventory.FindByItemId(req.ItemId)!;
            item.Count -= req.Count;
            if (item.Count <= 0)
            {
                player.Inventory.Remove(item.UniqueId);
                await _itemDao.DeleteAsync(item.UniqueId, ct);
                await conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
            }
            else
            {
                partiallyConsumed.Add(item);
            }
        }
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        if (partiallyConsumed.Count > 0)
            await conn.SendAsync(new SM_INVENTORY_ADD_ITEM(partiallyConsumed), ct);
        return true;
    }
}
