// Port of Java data/scripts/system/handlers/quest/elementis_forest/_30451KeytotheManor.java (Ritsu).
// Talk to 799535 to start; at 799582, SETPRO1 advances var 0->1; back at 799535, the collect-item
// check (inlined Java QuestService.collectItemCheck) flips var 1->2 and REWARD; turn in at 799535.
// Java bug fixed: the outer switch(targetId) had no break between the 799582 and 799535 cases, so
// talking to 799582 with an unmatched dialog also ran 799535's entire dialog switch (including the
// unguarded collect-item/reward-flip check) against the wrong npc; split into clean if/else-if here.
// Also added the var==1 gate matching changeQuestStep's own internal step check (see
// _30404ASpiritualConsultation for the identical situation).
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

public sealed class _30451KeytotheManor : QuestHandlerBase
{
    private const int QuestIdConst = 30451;
    private const int ManorNpc     = 799535;
    private const int OtherNpc     = 799582;

    private readonly IItemDao _itemDao;

    public _30451KeytotheManor(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(ManorNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(ManorNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(OtherNpc).OnTalk.Add(QuestId);
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
            if (targetId != ManorNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == OtherNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.SETPRO1:
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    default:
                        return false;
                }
            }

            if (targetId == ManorNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return var == 1 && await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
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
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == ManorNpc)
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
