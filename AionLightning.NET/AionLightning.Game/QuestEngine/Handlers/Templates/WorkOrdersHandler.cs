using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Templates.Quest;
using AionLightning.Game.Model.Templates.Quest.Script;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.QuestEngine.Handlers.Templates;

/// <summary>
/// Data-driven crafting "work order" quest handler (Java
/// <c>questEngine.handlers.template.WorkOrders</c> port) — covers &lt;work_order&gt; entries (574
/// on disk, all repeatable TASK-category quests: accept teaches the recipe and gives its
/// components, turn-in consumes the crafted product declared in quest_data.xml's collect_items).
/// </summary>
/// <remarks>
/// Java's completion is a 2-3 click round trip: leftover-component cleanup fires on the first
/// START-status click (transitioning to REWARD without granting), then collect-item consumption +
/// recipe deletion fire on a second click while already REWARD, finally followed by the shared
/// SELECT_QUEST_REWARD grant. Collapsed here into the same one-shot "check objectives -> transition
/// to REWARD -> grant on SELECT_QUEST_REWARD" shape already used by ItemCollectingHandler/
/// MonsterHuntHandler/ReportToHandler: <see cref="Services.QuestRewardService.GrantAndCompleteAsync"/>
/// already validates and consumes collect_items generically at grant time, so this handler only
/// needs to additionally clean up leftover quest_work_items and drop the taught recipe at the
/// START -&gt; REWARD transition. Also note: Java's NONE-status switch only reacts to QUEST_SELECT
/// and QUEST_ACCEPT_1 (no generic accept/refuse fallback like the other three templates use) —
/// ported literally, since that's the real Java behavior for this template specifically.
/// Not ported: quest-repeat (<c>max_repeat_count="255"</c>) — the same pre-existing gap noted in
/// <c>QuestEngine.ComputeNearbyQuests</c>'s remarks applies to every template handler; a completed
/// work order can't currently be re-accepted through this engine.
/// </remarks>
public sealed class WorkOrdersHandler : QuestHandlerBase
{
    private readonly HashSet<int> _startNpcs;
    private readonly int _recipeId;
    private readonly IReadOnlyList<CollectItem> _giveComponents;
    private readonly IItemDao _itemDao;
    private readonly IRecipeDao _recipeDao;

    public WorkOrdersHandler(WorkOrderScriptEntry data, IDataManager dataManager, IQuestDao questDao,
        QuestRewardService rewardService, IItemDao itemDao, IRecipeDao recipeDao)
        : base(data.Id, dataManager, questDao, rewardService)
    {
        _startNpcs      = data.StartNpcIds;
        _recipeId       = data.RecipeId;
        _giveComponents = data.GiveComponents;
        _itemDao        = itemDao;
        _recipeDao      = recipeDao;
    }

    public override void Register(QuestEngine engine)
    {
        foreach (int npcId in _startNpcs)
        {
            var npc = engine.RegisterQuestNpc(npcId);
            npc.OnQuestStart.Add(QuestId);
            npc.OnTalk.Add(QuestId);
        }
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var template = Template;
        if (template is null) return false;

        var player      = env.Player;
        int targetId     = env.TargetId;
        int targetObjId  = env.Target?.ObjectId ?? 0;
        if (!_startNpcs.Contains(targetId)) return false;

        var entry  = player.Quests.Get(QuestId);
        var status = entry?.Status ?? QuestStatus.NONE;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        switch (status)
        {
            case QuestStatus.NONE:
                if (player.Level < template.MinLevel) return false;

                return dialog switch
                {
                    DialogAction.QUEST_SELECT   => await SendQuestDialogAsync(conn, targetObjId, 4, ct),
                    DialogAction.QUEST_ACCEPT_1 => await TryAcceptAsync(env, conn, ct),
                    _ => false,
                };

            case QuestStatus.START:
                if (dialog != DialogAction.QUEST_SELECT) return false;
                return await TryAdvanceToRewardAsync(conn, player, entry!, template, targetObjId, ct);

            case QuestStatus.REWARD:
                return dialog switch
                {
                    DialogAction.QUEST_SELECT        => await SendQuestDialogAsync(conn, targetObjId, 5, ct),
                    DialogAction.SELECT_QUEST_REWARD => await SendQuestEndDialogAsync(env, conn, ct),
                    _ => false,
                };

            default:
                return false;
        }
    }

    /// <summary>
    /// Validates the recipe is new to the player, gives its components, then reuses the shared
    /// accept flow (Java: <c>validateNewRecipe</c> + <c>QuestService.startQuest</c> +
    /// <c>ItemService.addQuestItems</c> + <c>RecipeService.addRecipe</c>). Aborts before creating
    /// the quest entry when components can't fit — ReportToHandler-style guard, avoiding Java's
    /// silent give-failure-but-still-start-quest quirk.
    /// </summary>
    private async ValueTask<bool> TryAcceptAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;

        if (player.KnownRecipes.Contains(_recipeId)) return false;
        if (DataManager.Recipes.GetTemplate(_recipeId) is null) return false;
        if (!await GiveComponentsAsync(player, conn, ct)) return false;

        if (!await SendQuestStartDialogAsync(env, conn, ct)) return false;

        player.KnownRecipes.Add(_recipeId);
        await _recipeDao.AddRecipeAsync(player.ObjectId, _recipeId, ct);
        await conn.SendAsync(new SM_RECIPE_LIST(player.KnownRecipes), ct);
        return true;
    }

    private async ValueTask<bool> GiveComponentsAsync(Player player, GsClientConnection conn, CancellationToken ct)
    {
        foreach (var component in _giveComponents)
        {
            int maxStack = DataManager.Items.GetTemplate(component.ItemId)?.MaxStackCount ?? 1;
            if (!player.Inventory.CanReceive(component.ItemId, maxStack)) return false;
        }

        var given = new List<Item>();
        foreach (var component in _giveComponents)
        {
            long uid = await _itemDao.NextUniqueIdAsync(ct);
            var item = new Item { UniqueId = uid, ItemId = component.ItemId, Count = component.Count, Slot = -1 };
            player.Inventory.Add(item);
            given.Add(item);
        }
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await conn.SendAsync(new SM_INVENTORY_ADD_ITEM(given), ct);
        return true;
    }

    /// <summary>
    /// Re-checks the crafted product is in the bag (shared <see cref="QuestService.IsRewardReady"/>
    /// check against quest_data.xml's collect_items), strips leftover work-order materials, drops
    /// the taught recipe, and transitions to REWARD (actual grant + collect_items consumption
    /// happens on the follow-up SELECT_QUEST_REWARD click via <see cref="QuestHandlerBase.SendQuestEndDialogAsync"/>).
    /// </summary>
    private async ValueTask<bool> TryAdvanceToRewardAsync(GsClientConnection conn, Player player,
        QuestEntry entry, QuestTemplate template, int targetObjId, CancellationToken ct)
    {
        if (!QuestService.IsRewardReady(entry, template, player))
            return await SendQuestDialogAsync(conn, targetObjId, 10, ct);

        await RemoveLeftoverWorkItemsAsync(player, template, conn, ct);

        if (player.KnownRecipes.Remove(_recipeId))
            await _recipeDao.DeleteRecipeAsync(player.ObjectId, _recipeId, ct);

        await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
        return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
    }

    private async ValueTask RemoveLeftoverWorkItemsAsync(Player player, QuestTemplate template, GsClientConnection conn, CancellationToken ct)
    {
        var workItems = template.QuestWorkItems?.Items;
        if (workItems is not { Count: > 0 }) return;

        bool removedAny = false;
        foreach (var workItem in workItems)
        {
            var item = player.Inventory.FindByItemId(workItem.ItemId);
            if (item is null) continue;

            player.Inventory.Remove(item.UniqueId);
            await _itemDao.DeleteAsync(item.UniqueId, ct);
            await conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
            removedAny = true;
        }

        if (removedAny)
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
    }
}
