// Port of Java data/scripts/system/handlers/quest/rentus_base/_30506TragicReasons.java (Ritsu).
// Start at Oreitia (799544); hand in the quest_data.xml collect items at the same npc
// (CHECK_USER_HAS_QUEST_ITEM_SIMPLE, var0 stays 0, straight to REWARD); turn in at Maios (799549).
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

namespace Quest.RentusBase;

public sealed class _30506TragicReasons : QuestHandlerBase
{
    private const int QuestIdConst = 30506;
    private const int StartNpc     = 799544;
    private const int MaiosNpc     = 799549;

    private readonly IItemDao _itemDao;

    public _30506TragicReasons(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MaiosNpc).OnTalk.Add(QuestId);
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
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == StartNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM_SIMPLE)
                return await CheckQuestItemsSimpleAsync(env, conn, 0, 0, reward: true, checkOkId: 10000, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == MaiosNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    /// <summary>Java QuestHandler.checkQuestItemsSimple: like <see cref="CheckQuestItemsAsync"/>
    /// (validates + consumes quest_data.xml collect_items) but closes the dialog window on failure
    /// instead of showing a fail-specific dialog page (Java's signature has no checkFailId).</summary>
    private async ValueTask<bool> CheckQuestItemsSimpleAsync(QuestEnv env, GsClientConnection conn,
        int step, int nextStep, bool reward, int checkOkId, CancellationToken ct)
    {
        var player      = env.Player;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != step) return false;

        var collectItems = Template?.CollectItems?.Items;
        if (collectItems is not { Count: > 0 })
            return await CloseDialogWindowAsync(conn, targetObjId, ct);

        foreach (var req in collectItems)
        {
            var item = player.Inventory.FindByItemId(req.ItemId);
            if (item is null || item.Count < req.Count)
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
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

        await ChangeQuestStepAsync(conn, entry, 0, nextStep, reward, ct);
        return await SendQuestDialogAsync(conn, targetObjId, checkOkId, ct);
    }
}
