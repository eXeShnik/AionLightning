// Port of Java data/scripts/system/handlers/quest/fenris_fang/_4943LuckandPersistence.java
// (Nanou/Gigi). Start at Kvasir (204053, plain accept); Latatusk (204096, var 0->1); Relir (204097,
// var 1->2, pays 3400000 kinah and receives the empty vessel 182207123); a quest device (700538,
// var 2, USE_OBJECT no-op fills the vessel); back at Latatusk (var 2->3, a quest_data.xml
// collect-item check that ALSO removes 182207123 — a two-item consume Java expresses inline rather
// than via checkQuestItems, so the collect-items sweep is reimplemented here as
// TryConsumeCollectItemsAsync, mirroring Java's QuestService.collectItemCheck); Balder (204075)
// requires 186000084 x1 to flip to reward; turn in at Kvasir.
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

namespace Quest.FenrisFang;

public sealed class _4943LuckandPersistence : QuestHandlerBase
{
    private const int QuestIdConst  = 4943;
    private const int KvasirNpc     = 204053;
    private const int LatatuskNpc   = 204096;
    private const int RelirNpc      = 204097;
    private const int DeviceNpc     = 700538;
    private const int BalderNpc     = 204075;
    private const int EmptyVesselItem = 182207123;
    private const int HolyWaterItem = 186000084;
    private const int KinahItemId   = 182400001;
    private const long KinahCost    = 3400000;

    private readonly IItemDao _itemDao;

    public _4943LuckandPersistence(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(KvasirNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(KvasirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LatatuskNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DeviceNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BalderNpc).OnTalk.Add(QuestId);
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
            if (targetId != KvasirNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == LatatuskNpc)
            {
                if (var == 0)
                {
                    if (dialog == DialogAction.QUEST_SELECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (dialog == DialogAction.SETPRO1)
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                if (var == 2)
                {
                    if (dialog == DialogAction.QUEST_SELECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    {
                        if (await TryConsumeCollectItemsAsync(player, conn, ct))
                        {
                            await RemoveQuestItemAsync(player, conn, _itemDao, EmptyVesselItem, 1, ct);
                            await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                            return await SendQuestDialogAsync(conn, targetObjId, 10000, ct);
                        }
                        return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
                    }
                }
                return false;
            }
            if (targetId == RelirNpc)
            {
                if (var != 1) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SELECT_ACTION_1354)
                {
                    if (await TryDeductKinahAsync(player, conn, KinahCost, ct))
                    {
                        if (!HasItem(player, EmptyVesselItem, 1)
                            && !await GiveQuestItemAsync(player, conn, _itemDao, EmptyVesselItem, 1, ct))
                            return true;
                        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 1354, ct);
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 1438, ct);
                }
                return false;
            }
            if (targetId == DeviceNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 2)
                    return await UseQuestObjectAsync(env, conn, 2, 2, reward: false, varNum: 0, ct);
                return false;
            }
            if (targetId == BalderNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        if (var == 3)
                            return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                        return false;
                    case DialogAction.SET_SUCCEED:
                        if (HasItem(player, HolyWaterItem, 1))
                        {
                            await RemoveQuestItemAsync(player, conn, _itemDao, HolyWaterItem, 1, ct);
                            return await DefaultCloseDialogAsync(env, conn, 3, 3, reward: true, sameNpc: false, ct);
                        }
                        return await SendQuestDialogAsync(conn, targetObjId, 2120, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }
            // Java switch(targetId) default: return sendQuestStartDialog(env).
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == KvasirNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    private static bool HasItem(Player player, int itemId, long count)
    {
        var item = player.Inventory.FindByItemId(itemId);
        return item is not null && item.Count >= count;
    }

    /// <summary>Java Inventory.tryDecreaseKinah(amount): deducts kinah if the player has enough, persisting the change.</summary>
    private async ValueTask<bool> TryDeductKinahAsync(Player player, GsClientConnection conn, long amount, CancellationToken ct)
    {
        var kinah = player.Inventory.FindByItemId(KinahItemId);
        if (kinah is null || kinah.Count < amount) return false;

        kinah.Count -= amount;
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinah]), ct);
        return true;
    }

    /// <summary>Java QuestService.collectItemCheck(env, true): verifies every quest_data.xml
    /// &lt;collect_item&gt; is present and consumes them, without sending any dialog or changing the
    /// step — the caller here also needs to remove an extra, non-collect-list item on success, which
    /// QuestHandlerBase.CheckQuestItemsAsync doesn't support in one call.</summary>
    private async ValueTask<bool> TryConsumeCollectItemsAsync(Player player, GsClientConnection conn, CancellationToken ct)
    {
        var collectItems = Template?.CollectItems?.Items;
        if (collectItems is not { Count: > 0 }) return false;

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
