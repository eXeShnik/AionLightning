// Port of Java data/scripts/system/handlers/quest/morheim/_2332MeatyTreats.java.
// Talk to 798084 to start; selecting the quest option immediately collect-checks (and consumes)
// the quest_data.xml collect items (Java QuestService.collectItemCheck(env, true), inlined here
// since it fires without a step transition, unlike QuestHandlerBase.CheckQuestItemsAsync); picking
// one of 3 reward tiers (SETPRO1/2/3, dialog ids 10000-10002) sets the reward index into var 0 and
// flips to REWARD; the follow-up SELECTED_QUEST_NOREWARD dialog (id 23) finishes the quest with
// that reward index (Java's finishQuest(env, var)).
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

namespace Quest.Morheim;

public sealed class _2332MeatyTreats : QuestHandlerBase
{
    private const int QuestIdConst = 2332;
    private const int TreatsNpc    = 798084;

    private readonly IItemDao _itemDao;

    public _2332MeatyTreats(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(TreatsNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TreatsNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null)
        {
            if (targetId != TreatsNpc) return false;
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (targetId != TreatsNpc) return false;

        if (entry.Status == QuestStatus.START)
        {
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
            {
                bool hasItems = await CollectItemCheckAsync(player, conn, ct);
                return await SendQuestDialogAsync(conn, targetObjId, hasItems ? 1352 : 1693, ct);
            }
            if (env.DialogId is (int)DialogAction.SETPRO1 or (int)DialogAction.SETPRO2 or (int)DialogAction.SETPRO3)
            {
                int rewardIndex = env.DialogId - (int)DialogAction.SETPRO1;
                entry.SetVar(0, entry.GetVar(0) + rewardIndex);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestDialogAsync(conn, targetObjId, env.DialogId - 9995, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && env.DialogId == (int)DialogAction.SELECTED_QUEST_NOREWARD)
        {
            await FinishQuestAsync(conn, player, entry.GetVar(0), ct);
            return await SendQuestDialogAsync(conn, targetObjId, 1008, ct);
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
