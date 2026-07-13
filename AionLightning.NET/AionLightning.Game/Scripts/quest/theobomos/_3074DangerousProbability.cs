// Port of Java data/scripts/system/handlers/quest/theobomos/_3074DangerousProbability.java
// ("[Spend Coin]" gold quest, repeatable). Talk to Nagrunerk (798193) and spend an Angel's Eye
// (186000037) plus a kinah fee (1000/5000/25000) for one of three reward tiers, each granting the
// quest's declared exp plus a random count of Diamond Fragments (186000005). The tier is tracked
// in-memory (not persisted) exactly as Java's own instance field `reward`, matching the same
// crash/restart fragility.
// Skip vs Java: the repeatable `qs.canRepeat()` gate is approximated as "no active entry" (same
// documented gap as quest/raksang/_28710ScalingRewards.cs) - after one completion this quest's
// entry stays COMPLETE and the handler no longer responds, since this port has no repeat-count
// tracking wired into the accept gate yet.
using System;
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

namespace Quest.Theobomos;

public sealed class _3074DangerousProbability : QuestHandlerBase
{
    private const int QuestIdConst   = 3074;
    private const int NagrunerkNpc   = 798193;
    private const int AngelsEyeItem  = 186000037;
    private const int FragmentItemId = 186000005;
    private const int KinahItemId    = 182400001;

    private readonly IItemDao _itemDao;
    private int _reward = -1;

    public _3074DangerousProbability(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(NagrunerkNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(NagrunerkNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId != NagrunerkNpc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.EXCHANGE_COIN)
            {
                if (await StartMissionAsync(conn, player, QuestStatus.START, ct))
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            long kinahAmount = player.Inventory.FindByItemId(KinahItemId)?.Count ?? 0;
            long angelsEye   = player.Inventory.FindByItemId(AngelsEyeItem)?.Count ?? 0;

            switch (dialog)
            {
                case DialogAction.EXCHANGE_COIN:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.SELECT_ACTION_1011:
                    if (kinahAmount >= 1000 && angelsEye >= 1)
                    {
                        await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                        _reward = 0;
                        return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 1009, ct);
                case DialogAction.SELECT_ACTION_1352:
                    if (kinahAmount >= 5000 && angelsEye >= 1)
                    {
                        await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                        _reward = 1;
                        return await SendQuestDialogAsync(conn, targetObjId, 6, ct);
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 1009, ct);
                case DialogAction.SELECT_ACTION_1693:
                    if (kinahAmount >= 25000 && angelsEye >= 1)
                    {
                        await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                        _reward = 2;
                        return await SendQuestDialogAsync(conn, targetObjId, 7, ct);
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 1009, ct);
                case DialogAction.FINISH_DIALOG:
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog != DialogAction.SELECTED_QUEST_NOREWARD) return false;

            switch (_reward)
            {
                case 0 when await FinishQuestAsync(conn, player, 0, ct):
                    await DecreaseKinahAsync(player, conn, 1000, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, AngelsEyeItem, 1, ct);
                    await GrantItemAsync(player, conn, FragmentItemId, 1, ct);
                    _reward = -1;
                    break;
                case 1 when await FinishQuestAsync(conn, player, 1, ct):
                    await DecreaseKinahAsync(player, conn, 5000, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, AngelsEyeItem, 1, ct);
                    await GrantItemAsync(player, conn, FragmentItemId, Random.Shared.Next(1, 4), ct);
                    _reward = -1;
                    break;
                case 2 when await FinishQuestAsync(conn, player, 2, ct):
                    await GrantItemAsync(player, conn, FragmentItemId, Random.Shared.Next(1, 7), ct);
                    await DecreaseKinahAsync(player, conn, 25000, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, AngelsEyeItem, 1, ct);
                    _reward = -1;
                    break;
            }
            return await CloseDialogWindowAsync(conn, targetObjId, ct);
        }

        return false;
    }

    private async ValueTask DecreaseKinahAsync(Player player, GsClientConnection conn, long amount, CancellationToken ct)
    {
        var kinah = player.Inventory.FindByItemId(KinahItemId);
        if (kinah is null) return;
        kinah.Count -= amount;
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinah]), ct);
    }

    private async ValueTask GrantItemAsync(Player player, GsClientConnection conn, int itemId, long count, CancellationToken ct)
    {
        var existing = player.Inventory.FindByItemId(itemId);
        Item item;
        if (existing is not null)
        {
            existing.Count += count;
            item = existing;
        }
        else
        {
            long uid = await _itemDao.NextUniqueIdAsync(ct);
            item = new Item { UniqueId = uid, ItemId = itemId, Count = count, Slot = -1 };
            player.Inventory.Add(item);
        }
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
    }
}
