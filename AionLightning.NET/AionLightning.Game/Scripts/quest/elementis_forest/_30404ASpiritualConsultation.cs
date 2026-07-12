// Port of Java data/scripts/system/handlers/quest/elementis_forest/_30404ASpiritualConsultation.java (Ritsu).
// Talk to 799551 to start; at 205575, SETPRO1 advances var 0->1, then the collect-item check
// (inlined Java QuestService.collectItemCheck, not the checkQuestItemsSimple wrapper) flips var
// 1->2 and REWARD; turn in at 205575 (not the start npc).
// Java bug fixed: the QUEST_SELECT case at 205575 fell through (missing break) into SETPRO1's body
// whenever var wasn't 0 or 1 - harmless in practice (defaultCloseDialog's own var==0 gate no-ops),
// but written here without the fallthrough for clarity.
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

public sealed class _30404ASpiritualConsultation : QuestHandlerBase
{
    private const int QuestIdConst = 30404;
    private const int StartNpc     = 799551;
    private const int SeerNpc      = 205575;

    private readonly IItemDao _itemDao;

    public _30404ASpiritualConsultation(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SeerNpc).OnTalk.Add(QuestId);
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
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != SeerNpc) return false;
            int var = entry.GetVar(0);

            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    return false;
                case DialogAction.SETPRO1:
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                case DialogAction.CHECK_USER_HAS_QUEST_ITEM_SIMPLE:
                    if (var != 1) return false; // mirrors Java changeQuestStep's internal var==1 gate
                    if (await CollectItemCheckAsync(player, conn, ct))
                    {
                        entry.SetVar(0, 2);
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                    }
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == SeerNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    /// <summary>Java QuestService.collectItemCheck(env, true), inlined directly (not via the
    /// step-gated checkQuestItemsSimple wrapper) — checks every quest_data.xml collect item is
    /// present, then consumes them all, without touching quest state itself.</summary>
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
