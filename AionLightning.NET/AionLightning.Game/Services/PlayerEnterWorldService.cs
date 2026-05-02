using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services;

/// <summary>Loads all player data from DB and sends the full enter-world packet sequence.</summary>
public sealed class PlayerEnterWorldService
{
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
    private readonly IManastoneDao            _manastoneDao;
    private readonly IPlayerTitleDao          _titleDao;

    public PlayerEnterWorldService(
        IPlayerDao playerDao,
        IPlayerAppearanceDao appearanceDao,
        IItemDao itemDao,
        IQuestDao questDao,
        GameWorld world,
        PlayerConnectionRegistry connRegistry,
        IDataManager dataManager,
        IMailDao mailDao,
        IMacroDao macroDao,
        ISocialDao socialDao,
        ILegionDao legionDao,
        LegionService legionService,
        IPlayerSettingsDao settingsDao,
        IRecipeDao recipeDao,
        IMotionDao motionDao,
        ISkillDao skillDao,
        IManastoneDao manastoneDao,
        IPlayerTitleDao titleDao)
    {
        _playerDao     = playerDao;
        _appearanceDao = appearanceDao;
        _itemDao       = itemDao;
        _questDao      = questDao;
        _world         = world;
        _connRegistry  = connRegistry;
        _dataManager   = dataManager;
        _mailDao       = mailDao;
        _macroDao      = macroDao;
        _socialDao     = socialDao;
        _legionDao     = legionDao;
        _legionService = legionService;
        _settingsDao   = settingsDao;
        _recipeDao     = recipeDao;
        _motionDao     = motionDao;
        _skillDao      = skillDao;
        _manastoneDao  = manastoneDao;
        _titleDao      = titleDao;
    }

    public async ValueTask EnterWorldAsync(GsClientConnection conn, int objectId, CancellationToken ct)
    {
        var player = await _playerDao.FindByObjectIdAsync(objectId, ct);
        if (player is null || player.AccountId != conn.AccountId)
        {
            await conn.DisposeAsync();
            return;
        }

        var appearance = await _appearanceDao.FindByPlayerIdAsync(objectId, ct);
        if (appearance is null)
        {
            await conn.DisposeAsync();
            return;
        }

        if (player.Position.WorldId == 0)
        {
            var spawn = _dataManager.PlayerInitial.GetSpawnLocation(player.Race);
            player.Position = new Position(spawn.X, spawn.Y, spawn.Z, spawn.Heading, spawn.MapId);
        }

        var tpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);

        player.BasePhysicalAttack = tpl?.MainHandAttack ?? 0;
        player.Appearance  = appearance;
        conn.ActivePlayer  = player;
        conn.State         = GsClientConnection.AionState.IN_GAME;

        for (int lvl = 1; lvl <= player.Level; lvl++)
            foreach (var slt in _dataManager.SkillTree.GetTemplatesFor(player.PlayerClass, lvl, player.Race))
                if (slt.AutoLearn)
                    player.Skills.AddSkill(slt.SkillId, slt.SkillLevel, slt.Stigma);

        var persistedSkills = await _skillDao.LoadByPlayerIdAsync(objectId, ct);
        foreach (var (skillId, skillLevel) in persistedSkills)
            player.Skills.AddSkill(skillId, skillLevel);

        var dbTitles = await _titleDao.LoadByPlayerIdAsync(objectId, ct);
        foreach (var tid in dbTitles)
            player.OwnedTitles.Add(tid);

        var storedItems = await _itemDao.FindByPlayerIdAsync(objectId, ct);
        if (storedItems.Count == 0)
        {
            foreach (var si in _dataManager.PlayerInitial.GetStartingItems(player.PlayerClass))
            {
                var uniqueId = await _itemDao.NextUniqueIdAsync(ct);
                var item = new Item { UniqueId = uniqueId, ItemId = si.ItemId, Count = si.Count, Slot = -1 };
                player.Inventory.Add(item);
            }
            await _itemDao.SaveAllAsync(objectId, player.Inventory.All, ct);
        }
        else
        {
            foreach (var item in storedItems)
                player.Inventory.Add(item);
        }

        foreach (var equippedItem in player.Inventory.All.Where(i => i.IsEquipped))
        {
            var stigmaTpl = _dataManager.Items.GetTemplate(equippedItem.ItemId);
            if (stigmaTpl?.Stigma is { } stigma)
                foreach (var (skillLevel, skillId) in stigma.GetSkills())
                    player.Skills.AddSkill(skillId, skillLevel, isStigma: true);
        }

        player.Inventory.Capacity = player.CubeCapacity;

        var warehouseItems = await _itemDao.FindWarehouseItemsAsync(objectId, ct);
        foreach (var item in warehouseItems)
            player.Warehouse.Add(item);

        var accWhItems = await _itemDao.FindAccountWarehouseAsync(conn.AccountId, ct);
        foreach (var item in accWhItems)
            player.AccountWarehouse.Add(item);

        var allItemIds = player.Inventory.All
            .Concat(player.Warehouse.All)
            .Concat(player.AccountWarehouse.All)
            .Select(i => i.UniqueId)
            .ToList();
        var allStones = await _manastoneDao.LoadByItemIdsAsync(allItemIds, ct);
        foreach (var stone in allStones)
        {
            var item = player.Inventory.All.FirstOrDefault(i => i.UniqueId == stone.ItemUniqueId)
                    ?? player.Warehouse.All.FirstOrDefault(i => i.UniqueId == stone.ItemUniqueId)
                    ?? player.AccountWarehouse.All.FirstOrDefault(i => i.UniqueId == stone.ItemUniqueId);
            item?.ManaStones.Add(stone);
        }

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

        if (player.TitleId > 0)
        {
            var titleTpl = _dataManager.Titles.GetTemplate(player.TitleId);
            if (titleTpl is not null)
            {
                player.TitleBonusMaxHp = titleTpl.GetAddStat("MAXHP");
                player.TitleBonusMaxMp = titleTpl.GetAddStat("MAXMP");
            }
        }

        float ssMult  = player.SoulSicknessMultiplier;
        player.MaxHp  = (int)(((tpl?.MaxHp ?? 1000) + player.BonusMaxHp + player.TitleBonusMaxHp) * ssMult);
        player.MaxMp  = (int)(((tpl?.MaxMp ?? 500)  + player.BonusMaxMp + player.TitleBonusMaxMp) * ssMult);
        player.CurrentHp = player.CurrentHp > 0 ? Math.Min(player.CurrentHp, player.MaxHp) : player.MaxHp;
        player.CurrentMp = player.CurrentMp > 0 ? Math.Min(player.CurrentMp, player.MaxMp) : player.MaxMp;
        player.CurrentFp = player.CurrentFp > 0 ? Math.Min(player.CurrentFp, player.MaxFp) : player.MaxFp;

        var questEntries = await _questDao.LoadByPlayerIdAsync(objectId, ct);
        foreach (var entry in questEntries)
            player.Quests.Add(entry);

        var macros = await _macroDao.LoadByPlayerIdAsync(objectId, ct);
        foreach (var kv in macros)
            player.Macros[kv.Key] = kv.Value;

        var (uiSettings, shortcuts, houseBuddies) = await _settingsDao.LoadAsync(objectId, ct);
        player.UiSettings   = uiSettings;
        player.Shortcuts    = shortcuts;
        player.HouseBuddies = houseBuddies;

        var motions = await _motionDao.LoadByPlayerIdAsync(objectId, ct);
        foreach (var (slot, id) in motions)
            player.ActiveMotions[slot] = id;

        var legionResult = await _legionDao.GetMemberLegionAsync(player.ObjectId, ct);
        if (legionResult.HasValue)
        {
            var (dbLegion, _) = legionResult.Value;
            var existing = _legionService.GetById(dbLegion.LegionId);
            if (existing is null)
            {
                var whItems = await _legionDao.FindWarehouseItemsAsync(dbLegion.LegionId, ct);
                foreach (var whItem in whItems)
                    dbLegion.WarehouseItems.Add(whItem);
                _legionService.AddLegion(dbLegion);
                player.Legion = dbLegion;
            }
            else
            {
                if (existing.Members.TryGetValue(player.ObjectId, out var liveMember))
                    liveMember.IsOnline = true;
                player.Legion = existing;
            }
        }

        _world.Add(player);
        _connRegistry.Register(player.ObjectId, conn);
        await _playerDao.UpdateOnlineAsync(player.ObjectId, online: true, ct);

        await conn.SendAsync(new SM_CHARACTER_SELECT(0), ct);
        await conn.SendAsync(new SM_SKILL_LIST(player.Skills.AllSkills), ct);
        await conn.SendAsync(new SM_SKILL_COOLDOWN(_dataManager.Skills, player.SkillCooldowns), ct);
        await conn.SendAsync(new SM_QUEST_COMPLETED_LIST(player.Quests.Completed), ct);
        await conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);
        await conn.SendAsync(SM_TITLE_INFO.ActiveTitle(player.TitleId), ct);
        await conn.SendAsync(SM_TITLE_INFO.BonusTitle(player.BonusTitleId), ct);
        await conn.SendAsync(SM_MOTION.OwnList(player.ActiveMotions), ct);
        await conn.SendAsync(new SM_CUSTOM_SETTINGS(player.ObjectId, player.DisplaySettings, player.DenySettings), ct);
        await conn.SendAsync(new SM_ENTER_WORLD_CHECK(), ct);

        if (player.UiSettings   is not null) await conn.SendAsync(new SM_UI_SETTINGS(0, player.UiSettings),   ct);
        if (player.Shortcuts    is not null) await conn.SendAsync(new SM_UI_SETTINGS(1, player.Shortcuts),    ct);
        if (player.HouseBuddies is not null) await conn.SendAsync(new SM_UI_SETTINGS(2, player.HouseBuddies), ct);

        byte npcExpands   = (byte)player.NpcExpands;
        byte questExpands = (byte)player.QuestExpands;
        await conn.SendAsync(new SM_INVENTORY_INFO(isFirst: true, player.Inventory.All.Where(i => !i.IsEquipped), npcExpands, questExpands), ct);
        await conn.SendAsync(new SM_INVENTORY_INFO(isFirst: false, [], npcExpands, questExpands), ct);
        await conn.SendAsync(new SM_WAREHOUSE_INFO(player.Warehouse.All), ct);
        await conn.SendAsync(new SM_STATS_INFO(player, tpl, _dataManager.ExpTable), ct);
        await conn.SendAsync(new SM_DP_INFO(player.ObjectId, player.Dp), ct);
        await conn.SendAsync(SM_CUBE_UPDATE.StigmaSlots(0), ct);
        await conn.SendAsync(new SM_INSTANCE_INFO(player), ct);
        await conn.SendAsync(new SM_CHANNEL_INFO(), ct);
        await conn.SendAsync(new SM_PLAYER_SPAWN(player), ct);
        await conn.SendAsync(new SM_GAME_TIME(), ct);

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
        await conn.SendAsync(new SM_BIND_POINT_INFO(bindPos), ct);

        await conn.SendAsync(SM_TITLE_INFO.TitleList(player.OwnedTitles), ct);
        await conn.SendAsync(new SM_EMOTION_LIST(0), ct);
        await conn.SendAsync(new SM_PRICES(), ct);
        await conn.SendAsync(SM_ABYSS_RANK.ForPlayer(player), ct);
        await conn.SendAsync(new SM_FLY_TIME(player.CurrentFp, player.MaxFp), ct);
        await conn.SendAsync(new SM_PACKAGE_INFO_NOTIFY(), ct);

        await conn.SendAsync(new SM_MACRO_LIST(player.ObjectId, player.Macros.Where(kv => kv.Key <= 24)), ct);
        await conn.SendAsync(new SM_MACRO_LIST(player.ObjectId, player.Macros.Where(kv => kv.Key > 24)), ct);

        foreach (var id in _dataManager.Recipes.GetAutoLearnIds(player.Race.ToString()))
            player.KnownRecipes.Add(id);
        var dbRecipes = await _recipeDao.LoadByPlayerIdAsync(player.ObjectId, ct);
        foreach (var id in dbRecipes)
            player.KnownRecipes.Add(id);
        await conn.SendAsync(new SM_RECIPE_LIST(player.KnownRecipes), ct);

        var mails  = await _mailDao.GetReceivedMailsAsync(player.ObjectId, ct);
        int unread = mails.Count(m => !m.IsRead);
        await conn.SendAsync(new SM_MAIL_SERVICE(mails.Count, unread), ct);

        var friends = await _socialDao.GetFriendsAsync(player.ObjectId, ct);
        var blocks  = await _socialDao.GetBlocksAsync(player.ObjectId, ct);
        var onlineIds = new HashSet<int>(_connRegistry.GetAll()
            .Select(c => c.ActivePlayer?.ObjectId ?? 0)
            .Where(id => id != 0));
        await conn.SendAsync(new SM_FRIEND_LIST(friends, onlineIds), ct);
        await conn.SendAsync(new SM_BLOCK_LIST(blocks), ct);

        if (friends.Count > 0)
        {
            var loginNotify = new SM_FRIEND_NOTIFY(SM_FRIEND_NOTIFY.Login, player.Name);
            foreach (var f in friends)
            {
                var fc = _connRegistry.Get(f.PlayerId);
                if (fc?.ActivePlayer is null) continue;
                try { await fc.SendAsync(loginNotify, ct); } catch { }
                var friendFriends = await _socialDao.GetFriendsAsync(f.PlayerId, ct);
                await fc.SendAsync(new SM_FRIEND_LIST(friendFriends, onlineIds), ct);
            }
        }

        if (player.Legion is { } legion)
        {
            await conn.SendAsync(new SM_LEGION_INFO(legion), ct);
            await conn.SendAsync(new SM_LEGION_MEMBERLIST(legion.Members.Values), ct);

            if (legion.Members.TryGetValue(player.ObjectId, out var selfMember))
            {
                await conn.SendAsync(new SM_LEGION_UPDATE_TITLE(
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
