// Port of Java data/scripts/system/handlers/quest/elementis_forest/_30457SpiritChase.java (Ritsu).
// Talking to 799551 grants the chasing device (182213028); using it converts one device into one
// caught spirit (182213029, no cap - repeatable); handing in the quest_data.xml collect items (the
// caught spirits) at 799551 flips straight to REWARD. Identical structure to
// _30407CatchingtheLight - a different quest_data.xml collect-item/reward set, same npcs.
// Skip vs Java: the item-use target-npc check (player's current target must be 217262, in 12.5f
// range) can't be ported - OnItemUseAsync only carries (player, itemId), no NPC-target context
// (same limitation as _1573SomeTastyMushrooms/_4082GatheringtheHerbPouches); the 3s
// SM_ITEM_USAGE_ANIMATION delay is likewise collapsed into an immediate conversion, same precedent.
// Registers 205575 OnTalk to match Java, though Java's own onDialogEvent switch never reads that
// npc id either (dead registration in the original).
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

namespace Quest.ElementisForest;

public sealed class _30457SpiritChase : QuestHandlerBase
{
    private const int QuestIdConst  = 30457;
    private const int StartNpc      = 799551;
    private const int SeerNpc       = 205575;
    private const int DeviceItemId  = 182213028;
    private const int CaughtSpirit  = 182213029;

    private readonly IItemDao _itemDao;

    public _30457SpiritChase(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SeerNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(DeviceItemId, QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != DeviceItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, DeviceItemId, 1, ct);
        await GiveQuestItemAsync(player, conn, _itemDao, CaughtSpirit, 1, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
            {
                if (await GiveQuestItemAsync(player, conn, _itemDao, DeviceItemId, 1, ct))
                    return await SendQuestStartDialogAsync(env, conn, ct);
                return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != StartNpc) return false;
            int var = entry.GetVar(0);

            if (dialog == DialogAction.QUEST_SELECT)
                return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);

            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM_SIMPLE)
            {
                if (await CollectItemCheckAsync(player, conn, ct))
                {
                    entry.Status = QuestStatus.REWARD; // Java changeQuestStep(env, 0, 0, true) - reward flip only, var unchanged
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                }
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    /// <summary>Java QuestService.collectItemCheck(env, true), inlined directly (not via the
    /// step-gated checkQuestItemsSimple wrapper).</summary>
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
