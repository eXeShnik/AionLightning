// Port of Java data/scripts/system/handlers/quest/weapon_enchant/_22410ThePerfectAethercannon.java.
// Asmodian counterpart of _12410BuildABetterAethercannon - identical structure and npcs.
// Auto-starts on level-up once quest 10064 is complete (zone-mission style, min level 60,
// Asmodian only - class/gender gating from quest_data.xml isn't ported, see QuestHandlerBase).
// Accept at Ulian (205892); Grais (205891) and Lumen (205575) each show a lore dialog and advance
// var0 (0->1, 1->2); handing in the crafting materials (quest_data.xml collect_items: Adella's
// Aethercannon + Upsorceller + Magical Aether Crystal + Fusion Fragment) at Ulian flips straight
// to REWARD; turn in at Ulian.
//
// Skip vs Java: Grais' SETPRO1 branch calls TeleportService2.teleportTo(player, 600020000,
// 1444.7386f, 1359.0792f, 602.5101f, heading 103, BEAM_ANIMATION) - no TeleportService2 exists in
// this port (same precedent as _1916DispatchtoVerteron and others in this batch). The var advance
// is kept so the quest stays completable without the teleport.
//
// Java bug fixed: after QuestService.collectItemCheck(env, true) already consumes all four
// quest_data.xml collect_items, the original handler calls removeQuestItem again for the same
// four item ids - a redundant double-removal (harmless no-op since the items are already gone,
// but dead code). Collapsed to the single collectItemCheck-equivalent removal below.
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

namespace Quest.WeaponEnchant;

public sealed class _22410ThePerfectAethercannon : QuestHandlerBase
{
    private const int QuestIdConst = 22410;
    private const int UlianNpc     = 205892;
    private const int GraisNpc     = 205891;
    private const int LumenNpc     = 205575;

    private readonly IItemDao _itemDao;

    public _22410ThePerfectAethercannon(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(UlianNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(UlianNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GraisNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LumenNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 10064, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == UlianNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                {
                    await StartMissionAsync(conn, player, QuestStatus.START, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == GraisNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    // Skip: Java teleports to world 600020000 (1444.7386, 1359.0792, 602.5101,
                    // heading 103, BEAM_ANIMATION) here - TeleportService2 not ported (see header).
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }

            if (targetId == LumenNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }

            if (targetId == UlianNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM_SIMPLE)
                {
                    if (await CollectItemCheckAsync(player, conn, ct))
                    {
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                    }
                    return false;
                }
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == UlianNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    /// <summary>Java QuestService.collectItemCheck(env, true), inlined directly (not via the
    /// step-gated CheckQuestItemsAsync wrapper, since this dialog has no var-step precondition).</summary>
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
