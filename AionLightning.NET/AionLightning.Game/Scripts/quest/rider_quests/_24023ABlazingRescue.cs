// Port of Java data/scripts/system/handlers/quest/rider_quests/_24023ABlazingRescue.java (pralinka).
// Zone-mission sub-quest of 24020: talk to 204317 (var0 0->1), reach world 320050000 (var0 2->3, via
// onEnterWorldEvent), talk to 204408 (gives a letter, movie 78, var0 1->2; collect-check + reward
// flip at var0 3), turn in at 204407.
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

namespace Quest.RiderQuests;

public sealed class _24023ABlazingRescue : QuestHandlerBase
{
    private const int QuestIdConst = 24023;
    private const int FirstNpc  = 204317;
    private const int LetterNpc = 204408;
    private const int FinalNpc  = 204407;
    private const int LetterItem = 182215369;

    private readonly IItemDao _itemDao;

    public _24023ABlazingRescue(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        foreach (int npc in new[] { FirstNpc, LetterNpc, FinalNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 24020, isZoneMission: true, ct);

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.Player.Position.WorldId == 320050000 && entry.GetVar(0) == 2)
        {
            entry.SetVar(0, 3);
            await UpdateQuestStatusAsync(conn, entry, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == FirstNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    if (var != 0) return false;
                    entry.SetVar(0, 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }
            if (targetId == LetterNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO2)
                {
                    if (var != 1) return false;
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, LetterItem, 1, ct)) return true;
                    await PlayQuestMovieAsync(conn, player, 78, ct);
                    entry.SetVar(0, 2);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                {
                    if (var != 3) return false;
                    if (await CollectItemCheckAsync(player, conn, ct))
                    {
                        await RemoveQuestItemAsync(player, conn, _itemDao, LetterItem, 1, ct);
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 10000, ct);
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
                }
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == FinalNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
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
