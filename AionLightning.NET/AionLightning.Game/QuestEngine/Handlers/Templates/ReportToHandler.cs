using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Templates.Quest.Script;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.QuestEngine.Handlers.Templates;

/// <summary>
/// Data-driven "deliver an item / talk to npc" quest handler (Java
/// <c>questEngine.handlers.template.ReportTo</c> port) — covers &lt;report_to&gt; entries. About a
/// third of the shipped entries carry an <c>item_id</c>: a "quest work item" the handler itself
/// gives on accept and consumes on turn-in (independent of quest_data.xml's collect_items list).
/// </summary>
public sealed class ReportToHandler : QuestHandlerBase
{
    private readonly HashSet<int> _startNpcs;
    private readonly HashSet<int> _endNpcs;
    private readonly int          _itemId;
    private readonly IItemDao     _itemDao;

    public ReportToHandler(ReportToScriptEntry data, IDataManager dataManager,
        IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(data.Id, dataManager, questDao, rewardService)
    {
        _startNpcs = data.StartNpcIds;
        _endNpcs   = data.EndNpcIds;
        _itemId    = data.ItemId;
        _itemDao   = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        foreach (int npcId in _startNpcs)
        {
            var npc = engine.RegisterQuestNpc(npcId);
            npc.OnQuestStart.Add(QuestId);
            npc.OnTalk.Add(QuestId);
        }

        foreach (int npcId in _endNpcs)
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var template = Template;
        if (template is null) return false;

        var player       = env.Player;
        int targetId      = env.TargetId;
        int targetObjId   = env.Target?.ObjectId ?? 0;
        var entry         = player.Quests.Get(QuestId);
        var status        = entry?.Status ?? QuestStatus.NONE;
        var dialog        = DialogActionLookup.FromId(env.DialogId);

        switch (status)
        {
            case QuestStatus.NONE:
                if (!_startNpcs.Contains(targetId)) return false;
                if (player.Level < template.MinLevel) return false;

                return dialog switch
                {
                    DialogAction.QUEST_SELECT => await SendQuestDialogAsync(conn, targetObjId, 1011, ct),
                    DialogAction.QUEST_ACCEPT or DialogAction.QUEST_ACCEPT_1
                        => await StartQuestAsync(env, conn, ct),
                    DialogAction.QUEST_REFUSE or DialogAction.QUEST_REFUSE_1
                        or DialogAction.QUEST_REFUSE_2 or DialogAction.QUEST_REFUSE_SIMPLE
                        => await SendQuestStartDialogAsync(env, conn, ct),
                    _ => false,
                };

            case QuestStatus.START:
                if (!_endNpcs.Contains(targetId)) return false;

                return dialog switch
                {
                    DialogAction.QUEST_SELECT        => await SendQuestDialogAsync(conn, targetObjId, 2375, ct),
                    DialogAction.SELECT_QUEST_REWARD => await TryCompleteAsync(env, conn, entry!, ct),
                    _ => false,
                };

            case QuestStatus.REWARD:
                if (!_endNpcs.Contains(targetId)) return false;
                return await SendQuestEndDialogAsync(env, conn, ct);

            default:
                return false;
        }
    }

    /// <summary>
    /// Gives the quest work item (if any) before starting the quest (Java's
    /// <c>giveQuestItem</c>-then-<c>sendQuestStartDialog</c> sequence). Aborts without starting the
    /// quest when the item can't be given (bag full) — matching Java's <c>giveQuestItem</c> guard.
    /// </summary>
    private async ValueTask<bool> StartQuestAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (_itemId != 0 && !await GiveQuestItemAsync(env.Player, conn, ct))
            return false;

        return await SendQuestStartDialogAsync(env, conn, ct);
    }

    private async ValueTask<bool> GiveQuestItemAsync(Player player, GsClientConnection conn, CancellationToken ct)
    {
        if (player.Inventory.FindByItemId(_itemId) is not null) return true; // already carrying one (Java: "can't get lore item", still proceeds)

        int maxStack = DataManager.Items.GetTemplate(_itemId)?.MaxStackCount ?? 1;
        if (!player.Inventory.CanReceive(_itemId, maxStack)) return false;

        long uid = await _itemDao.NextUniqueIdAsync(ct);
        var item = new Item { UniqueId = uid, ItemId = _itemId, Count = 1, Slot = -1 };
        player.Inventory.Add(item);
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
        return true;
    }

    /// <summary>
    /// Validates the turn-in item (if any), consumes it, transitions to REWARD, and grants the
    /// quest reward — all on the same confirm click (Java's SELECT_QUEST_REWARD branch at
    /// QuestStatus.START, which removes the item and calls updateQuestStatus + sendQuestEndDialog
    /// in one go).
    /// </summary>
    private async ValueTask<bool> TryCompleteAsync(QuestEnv env, GsClientConnection conn, QuestEntry entry, CancellationToken ct)
    {
        var player      = env.Player;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (_itemId != 0)
        {
            var item = player.Inventory.FindByItemId(_itemId);
            if (item is null || item.Count < 1)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);

            await RemoveQuestItemAsync(player, item, conn, ct);
        }

        await ChangeQuestStepAsync(conn, entry, varIdx: 0, newValue: 1, toReward: true, ct);
        return await SendQuestEndDialogAsync(env, conn, ct);
    }

    private async ValueTask RemoveQuestItemAsync(Player player, Item item, GsClientConnection conn, CancellationToken ct)
    {
        item.Count -= 1;
        if (item.Count <= 0)
        {
            player.Inventory.Remove(item.UniqueId);
            await _itemDao.DeleteAsync(item.UniqueId, ct);
            await conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
        }
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
    }
}
