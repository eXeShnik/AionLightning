using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Templates.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.QuestEngine.Handlers;

/// <summary>
/// Base class for data-driven quest handlers (Java <c>questEngine.handlers.QuestHandler</c> port).
/// Provides the dialog/step helpers shared by every template: showing dialog pages, accepting a
/// quest, changing a var/step, and handing off reward payout to <see cref="QuestRewardService"/>.
/// </summary>
public abstract class QuestHandlerBase : IQuestHandler
{
    protected readonly IDataManager       DataManager;
    protected readonly IQuestDao          QuestDao;
    protected readonly QuestRewardService RewardService;

    public int QuestId { get; }

    protected QuestHandlerBase(int questId, IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService)
    {
        QuestId       = questId;
        DataManager   = dataManager;
        QuestDao      = questDao;
        RewardService = rewardService;
    }

    // Set once at engine startup (QuestEngineHostedService) so scripts — whose ctor is fixed at
    // four params for uniform reflection — can still spawn quest NPCs (Java QuestService.addNewSpawn).
    private static SpawnService? _spawnService;
    internal static void InitSpawnService(SpawnService spawnService) => _spawnService = spawnService;

    /// <summary>Spawns a quest NPC at a fixed location (Java QuestService.addNewSpawn). No-op with a
    /// warning-free false if the spawn service isn't wired or the npc template is unknown.</summary>
    protected bool SpawnQuestNpc(int worldId, int instanceId, int npcId, float x, float y, float z, byte heading)
    {
        if (_spawnService is null) return false;
        var template = DataManager.Npcs.GetTemplate(npcId);
        if (template is null) return false;
        _spawnService.SpawnNpcAt(template, new Position(x, y, z, heading, worldId, instanceId));
        return true;
    }

    /// <summary>The static quest_data.xml template for this quest, or null if not defined there.</summary>
    protected QuestTemplate? Template => DataManager.Quests.GetTemplate(QuestId);

    public abstract void Register(QuestEngine engine);

    public virtual ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);
    public virtual ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);
    public virtual ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);
    public virtual ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);
    public virtual ValueTask<bool> OnSkillUseAsync(Player player, int skillId, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);
    public virtual ValueTask<bool> OnPlayerKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);
    public virtual ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);
    public virtual ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);
    public virtual ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);
    public virtual ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);

    /// <summary>Opens an NPC dialog page (Java QuestHandler.sendDialogPacket). Always returns true (handled).</summary>
    protected async ValueTask<bool> SendQuestDialogAsync(GsClientConnection conn, int targetObjId, int dialogPageId, CancellationToken ct)
    {
        await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, dialogPageId, QuestId), ct);
        return true;
    }

    /// <summary>Closes the dialog window (Java QuestHandler.closeDialogWindow).</summary>
    protected ValueTask<bool> CloseDialogWindowAsync(GsClientConnection conn, int targetObjId, CancellationToken ct)
        => SendQuestDialogAsync(conn, targetObjId, 0, ct);

    /// <summary>
    /// Handles the accept/refuse leg of quest start (Java QuestHandler.sendQuestStartDialog).
    /// QUEST_ACCEPT/QUEST_ACCEPT_1 create the entry (status START) and show the accept-confirm
    /// page (1003); QUEST_REFUSE variants close the dialog. Any other action is unhandled.
    /// </summary>
    protected async ValueTask<bool> SendQuestStartDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player   = env.Player;
        int targetObjId = env.Target?.ObjectId ?? 0;

        switch (DialogActionLookup.FromId(env.DialogId))
        {
            case DialogAction.QUEST_ACCEPT:
            case DialogAction.QUEST_ACCEPT_1:
            {
                if (player.Quests.Contains(QuestId)) return false;

                var entry = new QuestEntry { QuestId = QuestId, Status = QuestStatus.START };
                player.Quests.Add(entry);
                await QuestDao.UpsertAsync(player.ObjectId, entry, ct);

                await conn.SendAsync(new SM_QUEST_ACTION(entry.QuestId,
                    SM_QUEST_ACTION.ActionType.Accept, (byte)entry.Status, entry.Step), ct);
                await conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);

                return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
            }

            case DialogAction.QUEST_REFUSE:
            case DialogAction.QUEST_REFUSE_1:
            case DialogAction.QUEST_REFUSE_2:
            case DialogAction.QUEST_REFUSE_SIMPLE:
                return await CloseDialogWindowAsync(conn, targetObjId, ct);

            default:
                return false;
        }
    }

    /// <summary>
    /// Handles quest completion (Java QuestHandler.sendQuestEndDialog). Refuses unless the quest
    /// is in REWARD status (exploit guard) and the action is SELECT_QUEST_REWARD; delegates the
    /// actual payout to <see cref="QuestRewardService"/> so it isn't duplicated per template.
    /// </summary>
    protected async ValueTask<bool> SendQuestEndDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.REWARD) return false;

        if (DialogActionLookup.FromId(env.DialogId) != DialogAction.SELECT_QUEST_REWARD) return false;

        var template = Template;
        if (template is null) return false;

        return await RewardService.GrantAndCompleteAsync(conn, player, entry, template, env.RewardIndex, ct);
    }

    /// <summary>
    /// Java QuestHandler.finishQuest(env, rewardIndex): grant a specific reward tier and complete,
    /// bypassing the SELECT_QUEST_REWARD dialog guard (used when the script has already validated
    /// the turn-in itself, e.g. multi-option reward quests picking the tier from a quest var).
    /// </summary>
    protected async ValueTask<bool> FinishQuestAsync(GsClientConnection conn, Player player, int rewardIndex, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        var template = Template;
        if (entry is null || template is null) return false;
        return await RewardService.GrantAndCompleteAsync(conn, player, entry, template, rewardIndex, ct);
    }

    /// <summary>Sets a quest var and/or transitions to REWARD, then broadcasts the update (Java changeQuestStep).</summary>
    protected async ValueTask ChangeQuestStepAsync(GsClientConnection conn, QuestEntry entry, int varIdx, int newValue, bool toReward, CancellationToken ct)
    {
        if (varIdx >= 0) entry.SetVar(varIdx, newValue);
        if (toReward) entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
    }

    /// <summary>Persists the entry and sends the SM_QUEST_ACTION step-update (Java updateQuestStatus).</summary>
    protected async ValueTask UpdateQuestStatusAsync(GsClientConnection conn, QuestEntry entry, CancellationToken ct)
    {
        var player = conn.ActivePlayer;
        if (player is null) return;

        await QuestDao.UpsertAsync(player.ObjectId, entry, ct);
        await conn.SendAsync(new SM_QUEST_ACTION(entry.QuestId,
            SM_QUEST_ACTION.ActionType.StepUpdate, (byte)entry.Status, entry.Step), ct);
        if (entry.Status is QuestStatus.REWARD or QuestStatus.COMPLETE)
            await conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);
    }

    /// <summary>Shows the generic "select next action" dialog page (Java QuestHandler.sendQuestSelectionDialog, always page 10). Always returns true (handled).</summary>
    protected ValueTask<bool> SendQuestSelectionDialogAsync(GsClientConnection conn, int targetObjId, CancellationToken ct)
        => SendQuestDialogAsync(conn, targetObjId, 10, ct);

    /// <summary>Plays a cutscene/movie for the player (Java QuestHandler.playQuestMovie — always type 0).</summary>
    protected ValueTask PlayQuestMovieAsync(GsClientConnection conn, Player player, int movieId, CancellationToken ct)
        => conn.SendAsync(new SM_PLAY_MOVIE(0, movieId), ct);

    /// <summary>
    /// Creates this quest's entry at <paramref name="status"/> (START or LOCKED) if — and only if
    /// — the player has no state for it yet, and broadcasts the resulting SM_QUEST_ACTION (Java
    /// QuestService.startMission). Returns false without effect when an entry already exists,
    /// matching Java's guard exactly.
    /// </summary>
    protected async ValueTask<bool> StartMissionAsync(GsClientConnection conn, Player player, QuestStatus status, CancellationToken ct)
    {
        if (player.Quests.Get(QuestId) is not null) return false;

        var entry = new QuestEntry { QuestId = QuestId, Status = status };
        player.Quests.Add(entry);
        await QuestDao.UpsertAsync(player.ObjectId, entry, ct);
        await conn.SendAsync(new SM_QUEST_ACTION(entry.QuestId,
            SM_QUEST_ACTION.ActionType.Accept, (byte)entry.Status, entry.Step), ct);
        return true;
    }

    /// <summary>Race + minimum-level gate shared by the mission-start helpers below (Java's race half of checkMissionStatConditions + checkLevelRequirement).</summary>
    private static bool MeetsRaceAndLevel(QuestTemplate template, Player player)
    {
        if (player.Level < template.MinLevel) return false;
        return template.Race == "PC_ALL" || string.Equals(template.Race, player.Race.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks whether this quest's mission-start preconditions are now met on a level-up tick and
    /// starts (or (re)locks) it accordingly (Java QuestHandler.defaultOnLvlUpEvent). Simplified vs
    /// Java: only race, minimum level, and "every id in <paramref name="precedingQuestIds"/> is
    /// COMPLETE" are checked — Java's class/gender/combine-skill stat gate
    /// (QuestService.checkMissionStatConditions) and &lt;start_condition&gt; XML rules
    /// (XMLStartCondition) aren't ported yet (no class/gender/skill or XML-start-condition infra);
    /// see migration_plan.md.
    /// </summary>
    protected async ValueTask<bool> DefaultOnLvlUpEventAsync(QuestEnv env, GsClientConnection conn,
        IReadOnlyCollection<int> precedingQuestIds, bool isZoneMission, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);

        // Only unset or LOCKED quests can (re)start on a level-up tick.
        if (entry is not null && entry.Status != QuestStatus.LOCKED) return false;

        var template = Template;
        if (template is null) return false;
        if (!MeetsRaceAndLevel(template, player)) return false;

        foreach (int precedingId in precedingQuestIds)
        {
            if (precedingId == 0) continue;
            var preceding = player.Quests.Get(precedingId);
            if (preceding is null || preceding.Status != QuestStatus.COMPLETE)
            {
                if (entry is null && !isZoneMission)
                    await StartMissionAsync(conn, player, QuestStatus.LOCKED, ct);
                return false;
            }
        }

        if (entry is null)
        {
            await StartMissionAsync(conn, player, QuestStatus.START, ct);
        }
        else
        {
            entry.Status = QuestStatus.START;
            await UpdateQuestStatusAsync(conn, entry, ct);
        }
        return true;
    }

    /// <summary>No-precondition shorthand (Java's 1-arg defaultOnLvlUpEvent).</summary>
    protected ValueTask<bool> DefaultOnLvlUpEventAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, [], isZoneMission: false, ct);

    /// <summary>Single-precondition shorthand (Java's 2-arg defaultOnLvlUpEvent).</summary>
    protected ValueTask<bool> DefaultOnLvlUpEventAsync(QuestEnv env, GsClientConnection conn, int precedingQuestId, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, [precedingQuestId], isZoneMission: false, ct);

    /// <summary>Single-precondition, zone-mission shorthand (Java's 3-arg defaultOnLvlUpEvent).</summary>
    protected ValueTask<bool> DefaultOnLvlUpEventAsync(QuestEnv env, GsClientConnection conn, int precedingQuestId, bool isZoneMission, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, [precedingQuestId], isZoneMission, ct);

    /// <summary>
    /// Checks whether this quest can now start after a related zone-mission quest was just turned
    /// in (Java QuestHandler.defaultOnZoneMissionEndEvent) — call this from the *completing*
    /// mission's own <c>OnDialogAsync</c> once per dependent quest id, at the point where it
    /// itself flips to COMPLETE (see <see cref="QuestEngine.OnZoneMissionEndAsync"/>'s doc comment
    /// for why there is no automatic "any quest completed" trigger). Same simplifications as
    /// <see cref="DefaultOnLvlUpEventAsync(QuestEnv,GsClientConnection,IReadOnlyCollection{int},bool,CancellationToken)"/>
    /// (no class/gender/skill/XML-start-condition checks).
    /// </summary>
    protected async ValueTask<bool> DefaultOnZoneMissionEndEventAsync(QuestEnv env, GsClientConnection conn,
        IReadOnlyCollection<int> precedingQuestIds, CancellationToken ct)
    {
        var player = env.Player;
        if (player.Quests.Get(QuestId) is not null) return false; // only null quests can start here

        var template = Template;
        if (template is null) return false;
        if (template.Race != "PC_ALL" && !string.Equals(template.Race, player.Race.ToString(), StringComparison.OrdinalIgnoreCase))
            return false; // stands in for Java's unconditional-fail checkMissionStatConditions

        if (player.Level < template.MinLevel)
        {
            await StartMissionAsync(conn, player, QuestStatus.LOCKED, ct);
            return false;
        }

        foreach (int precedingId in precedingQuestIds)
        {
            if (precedingId == 0) continue;
            var preceding = player.Quests.Get(precedingId);
            if (preceding is null || preceding.Status != QuestStatus.COMPLETE)
            {
                await StartMissionAsync(conn, player, QuestStatus.LOCKED, ct);
                return false;
            }
        }

        await StartMissionAsync(conn, player, QuestStatus.START, ct);
        return true;
    }

    /// <summary>No-precondition shorthand (Java's 1-arg defaultOnZoneMissionEndEvent).</summary>
    protected ValueTask<bool> DefaultOnZoneMissionEndEventAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, [], ct);

    /// <summary>Single-precondition shorthand (Java's 2-arg defaultOnZoneMissionEndEvent).</summary>
    protected ValueTask<bool> DefaultOnZoneMissionEndEventAsync(QuestEnv env, GsClientConnection conn, int precedingQuestId, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, [precedingQuestId], ct);

    /// <summary>
    /// Advances a quest var from <paramref name="step"/> to <paramref name="nextStep"/> (or flips
    /// straight to REWARD), then shows the next dialog page — the generic "close this npc's
    /// dialog and move on" flow used by the majority of hand-written quests (Java
    /// QuestHandler.defaultCloseDialog, no-item overloads). No-ops (returns false) if the quest
    /// isn't currently sitting at <paramref name="step"/>. When <paramref name="sameNpc"/> is true,
    /// the same NPC immediately shows the reward/turn-in dialog instead of the generic "select
    /// next action" page (Java distinguishes this via <c>Npc.getAi2().getName() == "useitem"</c>;
    /// that AI-name lookup isn't ported, so callers must say so explicitly instead — see
    /// migration_plan.md).
    /// </summary>
    protected async ValueTask<bool> DefaultCloseDialogAsync(QuestEnv env, GsClientConnection conn,
        int step, int nextStep, bool reward, bool sameNpc, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != step) return false;

        await ChangeQuestStepAsync(conn, entry, 0, nextStep, reward, ct);

        if (sameNpc) return await SendQuestEndDialogAsync(env, conn, ct);
        return await SendQuestSelectionDialogAsync(conn, env.Target?.ObjectId ?? 0, ct);
    }

    /// <summary>Shorthand: no reward flip, next step shown by a different npc (Java's 2-arg defaultCloseDialog).</summary>
    protected ValueTask<bool> DefaultCloseDialogAsync(QuestEnv env, GsClientConnection conn, int step, int nextStep, CancellationToken ct)
        => DefaultCloseDialogAsync(env, conn, step, nextStep, reward: false, sameNpc: false, ct);

    /// <summary>
    /// Item-bearing variant (Java's give/removeItem defaultCloseDialog overloads): optionally
    /// gives a quest item before the step transition and/or removes one after it, aborting with no
    /// state change if the give fails (bag full) exactly like Java's guard.
    /// </summary>
    protected async ValueTask<bool> DefaultCloseDialogAsync(QuestEnv env, GsClientConnection conn, IItemDao itemDao,
        int step, int nextStep, bool reward, bool sameNpc,
        int giveItemId, long giveItemCount, int removeItemId, long removeItemCount, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != step) return false;

        if (giveItemId != 0 && giveItemCount != 0 && !await GiveQuestItemAsync(player, conn, itemDao, giveItemId, giveItemCount, ct))
            return false;

        if (removeItemId != 0 && removeItemCount != 0)
            await RemoveQuestItemAsync(player, conn, itemDao, removeItemId, removeItemCount, ct);

        await ChangeQuestStepAsync(conn, entry, 0, nextStep, reward, ct);

        if (sameNpc) return await SendQuestEndDialogAsync(env, conn, ct);
        return await SendQuestSelectionDialogAsync(conn, env.Target?.ObjectId ?? 0, ct);
    }

    /// <summary>
    /// Advances quest var 0 by one on a kill of any of <paramref name="npcIds"/>, as long as it's
    /// currently within [<paramref name="startVar"/>, <paramref name="endVar"/>) (Java
    /// QuestHandler.defaultOnKillEvent, span overload) — a flat per-kill counter, distinct from
    /// <see cref="Templates.MonsterHuntHandler"/>'s packed multi-group spans.
    /// </summary>
    protected async ValueTask<bool> DefaultOnKillEventAsync(QuestEnv env, GsClientConnection conn,
        IReadOnlyCollection<int> npcIds, int startVar, int endVar, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (!npcIds.Contains(env.TargetId)) return false;

        int var = entry.GetVar(0);
        if (var < startVar || var >= endVar) return false;

        await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
        return true;
    }

    /// <summary>Single-npc shorthand of the span overload above.</summary>
    protected ValueTask<bool> DefaultOnKillEventAsync(QuestEnv env, GsClientConnection conn, int npcId, int startVar, int endVar, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, (IReadOnlyCollection<int>)[npcId], startVar, endVar, ct);

    /// <summary>
    /// Flips straight to REWARD (or bumps var 0 by one) on a kill of any of
    /// <paramref name="npcIds"/> while var 0 == <paramref name="startVar"/> exactly (Java
    /// QuestHandler.defaultOnKillEvent, reward-flip overload). Java leaves the var untouched at
    /// <paramref name="startVar"/> on the reward branch — only the non-reward branch bumps it —
    /// so this passes varIdx -1 (no-op write) to <see cref="ChangeQuestStepAsync"/> in that case.
    /// </summary>
    protected async ValueTask<bool> DefaultOnKillEventAsync(QuestEnv env, GsClientConnection conn,
        IReadOnlyCollection<int> npcIds, int startVar, bool reward, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (!npcIds.Contains(env.TargetId)) return false;
        if (entry.GetVar(0) != startVar) return false;

        await ChangeQuestStepAsync(conn, entry, varIdx: reward ? -1 : 0, newValue: startVar + 1, toReward: reward, ct);
        return true;
    }

    /// <summary>Single-npc shorthand of the reward-flip overload above.</summary>
    protected ValueTask<bool> DefaultOnKillEventAsync(QuestEnv env, GsClientConnection conn, int npcId, int startVar, bool reward, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, (IReadOnlyCollection<int>)[npcId], startVar, reward, ct);

    /// <summary>
    /// Checks whether the player is carrying every &lt;collect_item&gt; quest_data.xml lists for
    /// this quest, consuming them on success (Java QuestService.collectItemCheck via
    /// QuestHandler.checkQuestItems) — the "hand in gathered materials" gate used by hand-written
    /// collect-then-turn-in quests. On success, optionally gives another item, advances the step,
    /// and shows <paramref name="checkOkId"/>; on failure shows <paramref name="checkFailId"/>
    /// without changing any state.
    /// </summary>
    protected async ValueTask<bool> CheckQuestItemsAsync(QuestEnv env, GsClientConnection conn, IItemDao itemDao,
        int step, int nextStep, bool reward, int checkOkId, int checkFailId,
        int giveItemId, long giveItemCount, CancellationToken ct)
    {
        var player      = env.Player;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != step) return false;

        var collectItems = Template?.CollectItems?.Items;
        if (collectItems is not { Count: > 0 })
            return await SendQuestDialogAsync(conn, targetObjId, checkFailId, ct);

        foreach (var req in collectItems)
        {
            var item = player.Inventory.FindByItemId(req.ItemId);
            if (item is null || item.Count < req.Count)
                return await SendQuestDialogAsync(conn, targetObjId, checkFailId, ct);
        }

        var partiallyConsumed = new List<Item>();
        foreach (var req in collectItems)
        {
            var item = player.Inventory.FindByItemId(req.ItemId)!;
            item.Count -= req.Count;
            if (item.Count <= 0)
            {
                player.Inventory.Remove(item.UniqueId);
                await itemDao.DeleteAsync(item.UniqueId, ct);
                await conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
            }
            else
            {
                partiallyConsumed.Add(item);
            }
        }
        await itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        if (partiallyConsumed.Count > 0)
            await conn.SendAsync(new SM_INVENTORY_ADD_ITEM(partiallyConsumed), ct);

        if (giveItemId != 0 && giveItemCount != 0 && !await GiveQuestItemAsync(player, conn, itemDao, giveItemId, giveItemCount, ct))
            return false;

        await ChangeQuestStepAsync(conn, entry, 0, nextStep, reward, ct);
        return await SendQuestDialogAsync(conn, targetObjId, checkOkId, ct);
    }

    /// <summary>No extra give-item shorthand (Java's 6-arg checkQuestItems).</summary>
    protected ValueTask<bool> CheckQuestItemsAsync(QuestEnv env, GsClientConnection conn, IItemDao itemDao,
        int step, int nextStep, bool reward, int checkOkId, int checkFailId, CancellationToken ct)
        => CheckQuestItemsAsync(env, conn, itemDao, step, nextStep, reward, checkOkId, checkFailId, 0, 0, ct);

    /// <summary>
    /// Gives the player enough of <paramref name="itemId"/> to reach <paramref name="itemCount"/>
    /// total (Java QuestHandler.giveQuestItem) — a no-op success (with a "can't get lore item"
    /// notice) if they already have that many. Returns false only when the bag genuinely can't fit
    /// the shortfall.
    /// </summary>
    protected async ValueTask<bool> GiveQuestItemAsync(Player player, GsClientConnection conn, IItemDao itemDao,
        int itemId, long itemCount, CancellationToken ct)
    {
        if (itemId == 0 || itemCount == 0) return false;

        var existing       = player.Inventory.FindByItemId(itemId);
        long existingCount = existing?.Count ?? 0;
        var itemTemplate   = DataManager.Items.GetTemplate(itemId);

        if (existingCount >= itemCount)
        {
            if (itemTemplate is not null)
                await conn.SendAsync(SM_SYSTEM_MESSAGE.CanNotGetLoreItem(itemTemplate.Name), ct);
            return true;
        }

        long toGive  = itemCount - existingCount;
        int  maxStack = itemTemplate?.MaxStackCount ?? 1;
        if (!player.Inventory.CanReceive(itemId, maxStack)) return false;

        Item item;
        if (existing is not null)
        {
            existing.Count += toGive;
            item = existing;
        }
        else
        {
            long uid = await itemDao.NextUniqueIdAsync(ct);
            item = new Item { UniqueId = uid, ItemId = itemId, Count = toGive, Slot = -1 };
            player.Inventory.Add(item);
        }
        await itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
        return true;
    }

    /// <summary>Removes up to <paramref name="itemCount"/> of <paramref name="itemId"/> from the player's bag (Java QuestHandler.removeQuestItem).</summary>
    protected async ValueTask<bool> RemoveQuestItemAsync(Player player, GsClientConnection conn, IItemDao itemDao,
        int itemId, long itemCount, CancellationToken ct)
    {
        if (itemId == 0 || itemCount == 0) return false;

        var item = player.Inventory.FindByItemId(itemId);
        if (item is null) return false;

        item.Count -= itemCount;
        if (item.Count <= 0)
        {
            player.Inventory.Remove(item.UniqueId);
            await itemDao.DeleteAsync(item.UniqueId, ct);
            await conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
        }
        await itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        return true;
    }

    /// <summary>
    /// Handles a "use this quest object" interaction (Java QuestHandler.useQuestObject) —
    /// validates the current var, optionally gives/removes items and plays a movie, then advances
    /// the step. Simplified vs Java: no cast-time animation/scheduling (Java's sibling
    /// <c>useQuestItem</c> delays 3s via a scheduled task + SM_ITEM_USAGE_ANIMATION) and no
    /// "kill the target npc" (<c>dieObject</c>) support — this port has no NPC AI/controller
    /// death-trigger infra yet, so <paramref name="dieObject"/> is accepted but ignored (documented
    /// in migration_plan.md). Every effect applies immediately on the same tick.
    /// </summary>
    protected async ValueTask<bool> UseQuestObjectAsync(QuestEnv env, GsClientConnection conn,
        int step, int nextStep, bool reward, int varNum,
        int addItemId, long addItemCount, int removeItemId, long removeItemCount, int movieId, bool dieObject,
        IItemDao? itemDao, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(varNum) != step) return false;

        if (addItemId != 0 && addItemCount != 0)
        {
            if (itemDao is null) throw new InvalidOperationException($"{nameof(UseQuestObjectAsync)}: itemDao is required when addItemId is set.");
            if (!await GiveQuestItemAsync(player, conn, itemDao, addItemId, addItemCount, ct)) return false;
        }

        if (removeItemId != 0 && removeItemCount != 0)
        {
            if (itemDao is null) throw new InvalidOperationException($"{nameof(UseQuestObjectAsync)}: itemDao is required when removeItemId is set.");
            await RemoveQuestItemAsync(player, conn, itemDao, removeItemId, removeItemCount, ct);
        }

        if (movieId != 0)
            await PlayQuestMovieAsync(conn, player, movieId, ct);

        // dieObject (Java: kills the target npc via its AI controller) is a documented no-op — see summary.

        await ChangeQuestStepAsync(conn, entry, varNum, nextStep, reward, ct);
        return true;
    }

    /// <summary>Minimal shorthand: step transition only, no items/movie/die (Java's 4-arg useQuestObject).</summary>
    protected ValueTask<bool> UseQuestObjectAsync(QuestEnv env, GsClientConnection conn, int step, int nextStep, bool reward, bool dieObject, CancellationToken ct)
        => UseQuestObjectAsync(env, conn, step, nextStep, reward, 0, 0, 0, 0, 0, 0, dieObject, null, ct);

    /// <summary>Shorthand with an explicit varNum but no items/movie/die (Java's 5-arg useQuestObject).</summary>
    protected ValueTask<bool> UseQuestObjectAsync(QuestEnv env, GsClientConnection conn, int step, int nextStep, bool reward, int varNum, CancellationToken ct)
        => UseQuestObjectAsync(env, conn, step, nextStep, reward, varNum, 0, 0, 0, 0, 0, false, null, ct);
}
