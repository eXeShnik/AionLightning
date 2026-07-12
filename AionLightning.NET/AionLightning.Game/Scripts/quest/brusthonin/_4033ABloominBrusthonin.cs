// Port of Java data/scripts/system/handlers/quest/brusthonin/_4033ABloominBrusthonin.java.
// Talk to Heintz (205155) to start; hand in the quest_data.xml collect items (var 0->2, Java
// QuestService.collectItemCheck(env, true) inlined here since it doesn't match
// QuestHandlerBase.CheckQuestItemsAsync's dialog-page contract exactly — see _2332MeatyTreats for
// the same precedent); use Portaro's Tomb (700379, var 2 -> REWARD); turn in at Heintz.
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

public sealed class _4033ABloominBrusthonin : QuestHandlerBase
{
    private const int QuestIdConst = 4033;
    private const int HeintzNpc    = 205155;
    private const int TombObj      = 700379;

    private readonly IItemDao _itemDao;

    public _4033ABloominBrusthonin(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(HeintzNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(HeintzNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TombObj).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == HeintzNpc)
        {
            if (entry is null)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.START && entry.GetVar(0) == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (env.DialogId == (int)DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                {
                    if (await CollectItemCheckAsync(player, conn, ct))
                    {
                        entry.SetVar(0, entry.GetVar(0) + 2);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                        return true;
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 1438, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.REWARD)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestEndDialogAsync(env, conn, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry is not null && entry.Status == QuestStatus.START && targetId == TombObj)
        {
            if (entry.GetVar(0) == 2 && dialog == DialogAction.USE_OBJECT)
                return await UseQuestObjectAsync(env, conn, 2, 2, true, false, ct);
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
