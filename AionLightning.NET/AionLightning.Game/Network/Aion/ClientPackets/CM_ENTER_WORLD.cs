using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_ENTER_WORLD : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly IPlayerDao               _playerDao;
    private readonly IPlayerAppearanceDao     _appearanceDao;
    private readonly IItemDao                 _itemDao;
    private readonly IQuestDao                _questDao;
    private readonly GameWorld                _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IDataManager             _dataManager;
    private readonly IMailDao                 _mailDao;
    private readonly IMacroDao                _macroDao;
    private readonly ISocialDao               _socialDao;
    private readonly ILegionDao               _legionDao;
    private readonly LegionService            _legionService;
    private readonly IPlayerSettingsDao       _settingsDao;
    private readonly IRecipeDao               _recipeDao;
    private readonly IMotionDao               _motionDao;
    private readonly ISkillDao                _skillDao;

    private int _objectId;

    public CM_ENTER_WORLD(GsClientConnection conn, IPlayerDao playerDao,
        IPlayerAppearanceDao appearanceDao, IItemDao itemDao, IQuestDao questDao,
        GameWorld world, PlayerConnectionRegistry connRegistry,
        IDataManager dataManager, IMailDao mailDao, IMacroDao macroDao,
        ISocialDao socialDao, ILegionDao legionDao, LegionService legionService,
        IPlayerSettingsDao settingsDao, IRecipeDao recipeDao, IMotionDao motionDao,
        ISkillDao skillDao)
    {
        _conn           = conn;
        _playerDao      = playerDao;
        _appearanceDao  = appearanceDao;
        _itemDao        = itemDao;
        _questDao       = questDao;
        _world          = world;
        _connRegistry   = connRegistry;
        _dataManager    = dataManager;
        _mailDao        = mailDao;
        _macroDao       = macroDao;
        _socialDao      = socialDao;
        _legionDao      = legionDao;
        _legionService  = legionService;
        _settingsDao    = settingsDao;
        _recipeDao      = recipeDao;
        _motionDao      = motionDao;
        _skillDao       = skillDao;
    }

    public override void Read(ref PacketReader r) => _objectId = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = await _playerDao.FindByObjectIdAsync(_objectId, ct);
        if (player is null || player.AccountId != _conn.AccountId)
        {
            await _conn.DisposeAsync();
            return;
        }

        var appearance = await _appearanceDao.FindByPlayerIdAsync(_objectId, ct);
        if (appearance is null)
        {
            await _conn.DisposeAsync();
            return;
        }

        // Guard against corrupt/legacy WorldId=0 — reset to race starting zone
        if (player.Position.WorldId == 0)
        {
            var spawn = _dataManager.PlayerInitial.GetSpawnLocation(player.Race);
            player.Position = new Position(spawn.X, spawn.Y, spawn.Z, spawn.Heading, spawn.MapId);
        }

        var tpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);

        // Base combat stats from template — HP/MP are finalized after items load (bonuses applied below)
        player.BasePhysicalAttack = tpl?.MainHandAttack ?? 0;

        player.Appearance  = appearance;
        _conn.ActivePlayer = player;
        _conn.State        = GsClientConnection.AionState.IN_GAME;

        // Auto-learn skills for class + race up to current level
        for (int lvl = 1; lvl <= player.Level; lvl++)
        {
            foreach (var slt in _dataManager.SkillTree.GetTemplatesFor(player.PlayerClass, lvl, player.Race))
            {
                if (slt.AutoLearn)
                    player.Skills.AddSkill(slt.SkillId, slt.SkillLevel, slt.Stigma);
            }
        }

        // Load player-learned skills (skill books, crafting skills) from DB
        var persistedSkills = await _skillDao.LoadByPlayerIdAsync(_objectId, ct);
        foreach (var (skillId, skillLevel) in persistedSkills)
            player.Skills.AddSkill(skillId, skillLevel);

        // Load inventory from DB; give starting items for new characters
        var storedItems = await _itemDao.FindByPlayerIdAsync(_objectId, ct);
        if (storedItems.Count == 0)
        {
            foreach (var si in _dataManager.PlayerInitial.GetStartingItems(player.PlayerClass))
            {
                var uniqueId = await _itemDao.NextUniqueIdAsync(ct);
                var item = new Item { UniqueId = uniqueId, ItemId = si.ItemId, Count = si.Count, Slot = -1 };
                player.Inventory.Add(item);
            }
            await _itemDao.SaveAllAsync(_objectId, player.Inventory.All, ct);
        }
        else
        {
            foreach (var item in storedItems)
                player.Inventory.Add(item);
        }

        // Apply cube expansion — capacity comes from persisted NpcExpands loaded from DB
        player.Inventory.Capacity = player.CubeCapacity;

        // Load personal warehouse items
        var warehouseItems = await _itemDao.FindWarehouseItemsAsync(_objectId, ct);
        foreach (var item in warehouseItems)
            player.Warehouse.Add(item);

        // Initialize weapon combat stats from the equipped main-hand weapon
        var equippedMainHand = storedItems.FirstOrDefault(i => i.IsEquipped && i.Slot == 1);
        if (equippedMainHand is not null)
        {
            var wpnTpl = _dataManager.Items.GetTemplate(equippedMainHand.ItemId);
            if (wpnTpl?.WeaponStats is { } ws)
            {
                player.MainHandMinDmg     = ws.MinDamage;
                player.MainHandMaxDmg     = ws.MaxDamage;
                player.CurrentAttackSpeed = ws.AttackSpeed > 0 ? ws.AttackSpeed : 1500;
            }
        }

        // Initialize combat stats from all currently equipped items
        player.PhysicalDefense = storedItems
            .Where(i => i.IsEquipped)
            .Sum(i => _dataManager.Items.GetTemplate(i.ItemId)?.PhysicalDefense ?? 0);
        player.MagicDefense = storedItems
            .Where(i => i.IsEquipped)
            .Sum(i => _dataManager.Items.GetTemplate(i.ItemId)?.MagicDefense ?? 0);
        player.BonusMaxHp = storedItems
            .Where(i => i.IsEquipped)
            .Sum(i => _dataManager.Items.GetTemplate(i.ItemId)?.MaxHpBonus ?? 0);
        player.BonusMaxMp = storedItems
            .Where(i => i.IsEquipped)
            .Sum(i => _dataManager.Items.GetTemplate(i.ItemId)?.MaxMpBonus ?? 0);

        // Apply item HP/MP bonuses and soul sickness penalty on top of template values
        float ssMult = player.SoulSicknessMultiplier;
        player.MaxHp = (int)(((tpl?.MaxHp ?? 1000) + player.BonusMaxHp) * ssMult);
        player.MaxMp = (int)(((tpl?.MaxMp ?? 500)  + player.BonusMaxMp) * ssMult);
        // Restore persisted HP/MP; fall back to full if none saved (new character or never persisted)
        player.CurrentHp = player.CurrentHp > 0 ? Math.Min(player.CurrentHp, player.MaxHp) : player.MaxHp;
        player.CurrentMp = player.CurrentMp > 0 ? Math.Min(player.CurrentMp, player.MaxMp) : player.MaxMp;

        // Load quests
        var questEntries = await _questDao.LoadByPlayerIdAsync(_objectId, ct);
        foreach (var entry in questEntries)
            player.Quests.Add(entry);

        // Load macros
        var macros = await _macroDao.LoadByPlayerIdAsync(_objectId, ct);
        foreach (var kv in macros)
            player.Macros[kv.Key] = kv.Value;

        // Load UI settings blobs
        var (uiSettings, shortcuts, houseBuddies) = await _settingsDao.LoadAsync(_objectId, ct);
        player.UiSettings   = uiSettings;
        player.Shortcuts    = shortcuts;
        player.HouseBuddies = houseBuddies;

        // Load persisted motion slots
        var motions = await _motionDao.LoadByPlayerIdAsync(_objectId, ct);
        foreach (var (slot, id) in motions)
            player.ActiveMotions[slot] = id;

        // Load legion membership from DB; reuse cached legion if already in service
        var legionResult = await _legionDao.GetMemberLegionAsync(player.ObjectId, ct);
        if (legionResult.HasValue)
        {
            var (dbLegion, dbMember) = legionResult.Value;
            var existing = _legionService.GetById(dbLegion.LegionId);
            if (existing is null)
            {
                // First online member — prime service with the full legion
                _legionService.AddLegion(dbLegion);
                player.Legion = dbLegion;
            }
            else
            {
                // Legion already loaded by another online member — update that member's entry
                if (existing.Members.TryGetValue(player.ObjectId, out var liveMember))
                    liveMember.IsOnline = true;
                player.Legion = existing;
            }
        }

        _world.Add(player);
        _connRegistry.Register(player.ObjectId, _conn);

        await _playerDao.UpdateOnlineAsync(player.ObjectId, online: true, ct);

        // Enter-world sequence — mirrors Java PlayerEnterWorldService.enterWorld() ordering
        await _conn.SendAsync(new SM_CHARACTER_SELECT(0), ct);

        // Skills and cooldowns
        await _conn.SendAsync(new SM_SKILL_LIST(player.Skills.AllSkills), ct);
        await _conn.SendAsync(new SM_SKILL_COOLDOWN(), ct);

        // Quests
        await _conn.SendAsync(new SM_QUEST_COMPLETED_LIST(player.Quests.Completed), ct);
        await _conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);

        // Titles, motion, and social settings — send actual persisted title IDs
        await _conn.SendAsync(SM_TITLE_INFO.ActiveTitle(player.TitleId), ct);
        await _conn.SendAsync(SM_TITLE_INFO.BonusTitle(player.BonusTitleId), ct);
        await _conn.SendAsync(SM_MOTION.OwnList(player.ActiveMotions), ct);
        await _conn.SendAsync(new SM_CUSTOM_SETTINGS(player.ObjectId, player.DisplaySettings, player.DenySettings), ct);

        // Second enter-world check (Java sends this after motions)
        await _conn.SendAsync(new SM_ENTER_WORLD_CHECK(), ct);

        // UI settings blobs — send after SM_ENTER_WORLD_CHECK, before inventory (Java ordering)
        if (player.UiSettings   is not null) await _conn.SendAsync(new SM_UI_SETTINGS(0, player.UiSettings),   ct);
        if (player.Shortcuts    is not null) await _conn.SendAsync(new SM_UI_SETTINGS(1, player.Shortcuts),    ct);
        if (player.HouseBuddies is not null) await _conn.SendAsync(new SM_UI_SETTINGS(2, player.HouseBuddies), ct);

        // Inventory, warehouse, stats, cube (sendItemInfos equivalent)
        // Only bag items — equipped items are sent via SM_UPDATE_PLAYER_APPEARANCE / SM_PLAYER_INFO
        byte npcExpands   = (byte)player.NpcExpands;
        byte questExpands = (byte)player.QuestExpands;
        await _conn.SendAsync(new SM_INVENTORY_INFO(isFirst: true, player.Inventory.All.Where(i => !i.IsEquipped), npcExpands, questExpands), ct);
        await _conn.SendAsync(new SM_INVENTORY_INFO(isFirst: false, [], npcExpands, questExpands), ct);
        await _conn.SendAsync(new SM_WAREHOUSE_INFO(player.Warehouse.All), ct);
        await _conn.SendAsync(new SM_STATS_INFO(player, tpl, _dataManager.ExpTable), ct);
        await _conn.SendAsync(new SM_DP_INFO(player.ObjectId, player.Dp), ct);
        await _conn.SendAsync(SM_CUBE_UPDATE.StigmaSlots(0), ct);

        // World placement
        await _conn.SendAsync(new SM_INSTANCE_INFO(player), ct);
        await _conn.SendAsync(new SM_CHANNEL_INFO(), ct);
        await _conn.SendAsync(new SM_PLAYER_SPAWN(player), ct);
        await _conn.SendAsync(new SM_GAME_TIME(), ct);

        // Bind point — send obelisk location so the client knows where to respawn on death
        Position bindPos;
        if (player.BindPosition.HasValue)
        {
            bindPos = player.BindPosition.Value;
        }
        else
        {
            var spawn = _dataManager.PlayerInitial.GetSpawnLocation(player.Race);
            bindPos = new Position(spawn.X, spawn.Y, spawn.Z, spawn.Heading, spawn.MapId);
        }
        await _conn.SendAsync(new SM_BIND_POINT_INFO(bindPos), ct);

        // Post-spawn info
        await _conn.SendAsync(SM_TITLE_INFO.EmptyList(), ct);
        await _conn.SendAsync(new SM_EMOTION_LIST(0), ct);
        await _conn.SendAsync(new SM_PRICES(), ct);
        await _conn.SendAsync(new SM_ABYSS_RANK(player.AbyssPoints, player.AbyssRank), ct);
        await _conn.SendAsync(new SM_FLY_TIME(player.CurrentFp, player.MaxFp), ct);
        await _conn.SendAsync(new SM_PACKAGE_INFO_NOTIFY(), ct);

        // Macro and recipe lists — split at position 24 to match Java two-part send
        await _conn.SendAsync(new SM_MACRO_LIST(player.ObjectId, player.Macros.Where(kv => kv.Key <= 24)), ct);
        await _conn.SendAsync(new SM_MACRO_LIST(player.ObjectId, player.Macros.Where(kv => kv.Key > 24)), ct);

        // Auto-learn recipes for player's race + player-specific learned recipes from DB
        foreach (var id in _dataManager.Recipes.GetAutoLearnIds(player.Race.ToString()))
            player.KnownRecipes.Add(id);
        var dbRecipes = await _recipeDao.LoadByPlayerIdAsync(player.ObjectId, ct);
        foreach (var id in dbRecipes)
            player.KnownRecipes.Add(id);
        await _conn.SendAsync(new SM_RECIPE_LIST(player.KnownRecipes), ct);

        // Mailbox state — icon highlight if unread mail exists
        var mails  = await _mailDao.GetReceivedMailsAsync(player.ObjectId, ct);
        int unread = mails.Count(m => !m.IsRead);
        await _conn.SendAsync(new SM_MAIL_SERVICE(mails.Count, unread), ct);

        // Social lists — friend list with online status, block list
        var friends = await _socialDao.GetFriendsAsync(player.ObjectId, ct);
        var blocks  = await _socialDao.GetBlocksAsync(player.ObjectId, ct);
        var onlineIds = new HashSet<int>(_connRegistry.GetAll()
            .Select(c => c.ActivePlayer?.ObjectId ?? 0)
            .Where(id => id != 0));
        await _conn.SendAsync(new SM_FRIEND_LIST(friends, onlineIds), ct);
        await _conn.SendAsync(new SM_BLOCK_LIST(blocks), ct);

        // Notify each online friend that this player is now online (player is already registered above)
        if (friends.Count > 0)
        {
            foreach (var f in friends)
            {
                var fc = _connRegistry.Get(f.PlayerId);
                if (fc?.ActivePlayer is null) continue;
                var friendFriends = await _socialDao.GetFriendsAsync(f.PlayerId, ct);
                await fc.SendAsync(new SM_FRIEND_LIST(friendFriends, onlineIds), ct);
            }
        }

        // Legion login: send legion info to self, notify other online members
        if (player.Legion is { } legion)
        {
            await _conn.SendAsync(new SM_LEGION_INFO(legion), ct);
            await _conn.SendAsync(new SM_LEGION_MEMBERLIST(legion.Members.Values), ct);

            if (legion.Members.TryGetValue(player.ObjectId, out var selfMember))
            {
                await _conn.SendAsync(new SM_LEGION_UPDATE_TITLE(
                    player.ObjectId, legion.LegionId, legion.Name, selfMember.Rank), ct);

                var loginPkt = new SM_LEGION_UPDATE_MEMBER(selfMember, isOnline: true);
                var titlePkt = new SM_LEGION_UPDATE_TITLE(
                    player.ObjectId, legion.LegionId, legion.Name, selfMember.Rank);
                foreach (var m in legion.Members.Values)
                {
                    if (m.ObjectId == player.ObjectId) continue;
                    var mc = _connRegistry.Get(m.ObjectId);
                    if (mc is not null)
                    {
                        try { await mc.SendAsync(loginPkt, ct); } catch { }
                        try { await mc.SendAsync(titlePkt, ct); } catch { }
                    }
                }
            }
        }
    }
}
