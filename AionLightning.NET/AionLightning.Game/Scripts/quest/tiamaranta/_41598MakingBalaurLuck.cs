// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41598MakingBalaurLuck.java (Luzien).
// Repeatable "Balaur Luck" exchange at object 730555: use the object (requires the inventory item
// 186000096), accept to start, then hand back the two collect_items (186000096 + 186000174) via
// CHECK_USER_HAS_QUEST_ITEM_SIMPLE to flip to REWARD, and complete with SELECTED_QUEST_NOREWARD (which
// consumes the collect items). The quest has an empty <rewards/> block (a MEDAL bonus only).
// note: the special-cube guard is preserved via IsFullSpecialCube (a no-op false in this port, so the
// accept never blocks and the STR_MSG_FULL_INVENTORY warning branch is dead — dropped). Java's onCanAct
// gate (restricting quest actions to npc 730555) has no hook in this port and is dropped — it is not the
// sole progression gate, the dialog flow above drives the quest. The inventoryItemCheck warning packet
// (STR_QUEST_ACQUIRE_ERROR_INVENTORY_ITEM) is a cosmetic notice and is dropped. The Java canRepeat()
// re-accept gate is approximated as "no active entry" (same documented gap as theobomos/_3074): after one
// completion the COMPLETE entry blocks re-accept, as this port has no repeat-count tracking.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Tiamaranta;

public sealed class _41598MakingBalaurLuck : QuestHandlerBase
{
    private const int QuestIdConst = 41598;
    private const int ObjectNpc = 730555;

    private readonly IItemDao _itemDao;

    public _41598MakingBalaurLuck(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(ObjectNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(ObjectNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE) // Java: || qs.canRepeat()
        {
            if (targetId == ObjectNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                {
                    // Java: if (!inventoryItemCheck(env, true)) return true; else sendQuestDialog(1011).
                    if (!InventoryItemCheck(player))
                        return true; // note: warning packet dropped; nothing happens without the required item.
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                }
                if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                {
                    if (!IsFullSpecialCube(player))
                    {
                        if (await StartMissionAsync(conn, player, QuestStatus.START, ct))
                            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    }
                    else
                    {
                        // note: Java sends STR_MSG_FULL_INVENTORY here — dead branch (IsFullSpecialCube is always false).
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    }
                }
            }
        }
        else if (entry.Status == QuestStatus.START)
        {
            if (targetId == ObjectNpc)
            {
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM_SIMPLE)
                {
                    if (await CollectItemCheckAsync(player, conn, removeItem: false, ct))
                    {
                        await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                    }
                }
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == ObjectNpc)
            {
                if (dialog == DialogAction.SELECTED_QUEST_NOREWARD)
                {
                    if (await CollectItemCheckAsync(player, conn, removeItem: true, ct))
                        return await FinishQuestAsync(conn, player, 0, ct); // Java sendQuestEndDialog(env) -> finishQuest(env, 0)
                    return false;
                }
                // note: Java calls QuestService.abandonQuest here (declining the reward resets the repeatable
                // quest) — no abandon path is wired for scripts, so it is dropped; the quest stays at REWARD.
                return false;
            }
        }
        return false;
    }

    /// <summary>Java QuestService.inventoryItemCheck (presence only): the player must hold every
    /// &lt;inventory_item&gt; the quest declares. The showWarning notice is dropped.</summary>
    private bool InventoryItemCheck(Player player)
    {
        var items = Template?.InventoryItems?.Items;
        if (items is not { Count: > 0 }) return true;
        foreach (var ii in items)
            if ((player.Inventory.FindByItemId(ii.ItemId)?.Count ?? 0) < 1) return false;
        return true;
    }

    /// <summary>Java QuestService.collectItemCheck (collect_items path): the player must hold every
    /// &lt;collect_item&gt;; when <paramref name="removeItem"/> is set they are consumed.</summary>
    private async ValueTask<bool> CollectItemCheckAsync(Player player, GsClientConnection conn, bool removeItem, CancellationToken ct)
    {
        var items = Template?.CollectItems?.Items;
        if (items is not { Count: > 0 }) return true;
        foreach (var ci in items)
            if ((player.Inventory.FindByItemId(ci.ItemId)?.Count ?? 0) < ci.Count) return false;
        if (removeItem)
            foreach (var ci in items)
                await RemoveQuestItemAsync(player, conn, _itemDao, ci.ItemId, ci.Count, ct);
        return true;
    }
}
