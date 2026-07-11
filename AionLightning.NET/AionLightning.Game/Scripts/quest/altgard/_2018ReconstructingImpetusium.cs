// Port of Java data/scripts/system/handlers/quest/altgard/_2018ReconstructingImpetusium.java
// (MrPoke/Gigi/vlog). Talk to Gulkalla (203649), kill Hero Spirits (210588/210722, var 1->4),
// loot Umkata's Jewel Box (700097), bring the tokens to Umkata's Grave (700098) to summon and
// kill Umkata's Spirit (210752), then report back. Zone-mission chain, level-up gated.
// Bespoke: Java's collect-item check here is two-phase — CHECK_USER_HAS_QUEST_ITEM at the grave
// only *verifies* the collect_items (doesn't consume them yet, since the spirit still needs to be
// summoned and killed), then the kill of 210752 consumes them. QuestHandlerBase.CheckQuestItemsAsync
// always validates-and-consumes in one step, so this quest reimplements the peek/consume split
// inline (same "bespoke when it doesn't fit a helper" precedent as _1003IllegalLogging's boss kill).
using System.Collections.Generic;
using System.Linq;
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

namespace Quest.Altgard;

public sealed class _2018ReconstructingImpetusium : QuestHandlerBase
{
    private const int QuestIdConst  = 2018;
    private const int GulkallaNpc   = 203649;
    private const int JewelBoxObj   = 700097;
    private const int GraveObj      = 700098;
    private const int UmkataSpirit  = 210752;
    private const int AltgardWorldId = 220030000;

    private static readonly int[] _spiritMobs = [210588, 210722];

    private readonly IItemDao _itemDao;

    public _2018ReconstructingImpetusium(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(GulkallaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(JewelBoxObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GraveObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(210588).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(210722).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(UmkataSpirit).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2200, isZoneMission: true, ct);

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
            switch (targetId)
            {
                case GulkallaNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT when var == 0:
                            return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                        case DialogAction.QUEST_SELECT when var == 4:
                            return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                        case DialogAction.QUEST_SELECT when var == 7:
                            return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                        case DialogAction.SETPRO1:
                            return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                        case DialogAction.SETPRO2:
                            return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                        case DialogAction.SELECT_QUEST_REWARD:
                            await ChangeQuestStepAsync(conn, entry, 0, 7, toReward: true, ct);
                            return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                        default:
                            return false;
                    }
                case JewelBoxObj:
                    return dialog == DialogAction.USE_OBJECT && var == 5; // loot
                case GraveObj:
                    switch (dialog)
                    {
                        case DialogAction.USE_OBJECT when var == 5:
                            return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                        case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                            if (var != 5) return false;
                            if (HasAllCollectItems(player))
                            {
                                SpawnQuestNpc(AltgardWorldId, player.Position.InstanceId, UmkataSpirit,
                                    2889.9834f, 1741.3108f, 254.75f, 0);
                                return await CloseDialogWindowAsync(conn, targetObjId, ct);
                            }
                            return await SendQuestDialogAsync(conn, targetObjId, 2120, ct);
                        case DialogAction.FINISH_DIALOG:
                            return await CloseDialogWindowAsync(conn, targetObjId, ct);
                        default:
                            return false;
                    }
                default:
                    return false;
            }
        }
        if (entry.Status == QuestStatus.REWARD && targetId == GulkallaNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        int var = entry.GetVar(0);

        if (var is >= 1 and < 4)
            return await DefaultOnKillEventAsync(env, conn, (IReadOnlyCollection<int>)_spiritMobs, 1, 4, ct);

        if (var == 5 && env.TargetId == UmkataSpirit)
        {
            entry.SetVar(0, 7);
            await RemoveCollectItemsAsync(player, conn, ct);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    private bool HasAllCollectItems(Player player)
    {
        var items = Template?.CollectItems?.Items;
        if (items is not { Count: > 0 }) return false;
        foreach (var req in items)
        {
            var item = player.Inventory.FindByItemId(req.ItemId);
            if (item is null || item.Count < req.Count) return false;
        }
        return true;
    }

    private async ValueTask RemoveCollectItemsAsync(Player player, GsClientConnection conn, CancellationToken ct)
    {
        var items = Template?.CollectItems?.Items;
        if (items is not { Count: > 0 }) return;

        var partiallyConsumed = new List<Item>();
        foreach (var req in items)
        {
            var item = player.Inventory.FindByItemId(req.ItemId);
            if (item is null) continue;
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
    }
}
