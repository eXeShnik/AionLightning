using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Options;
using GameWorld = AionLightning.Game.World.World;
using QuestEngineType = AionLightning.Game.QuestEngine.QuestEngine;

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
    private readonly PetService               _petService;
    private readonly IMotionDao               _motionDao;
    private readonly ISkillDao                _skillDao;
    private readonly IManastoneDao            _manastoneDao;
    private readonly IPlayerTitleDao          _titleDao;
    private readonly QuestEngineType          _questEngine;
    private readonly SkillLearnService        _skillLearn;
    private readonly SiegeService             _siegeService;
    private readonly HousingService           _housingService;
    private readonly HousingBidService        _housingBidService;
    private readonly IOptions<HousingOptions> _housingOptions;
    private readonly IHouseObjectCooldownsDao _houseObjectCooldownsDao;
    private readonly StigmaService            _stigmaService;
    private readonly TownService              _townService;
    private readonly IOptions<TownOptions>    _townOptions;
    private readonly DisputeLandService       _disputeLandService;

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
        IPlayerTitleDao titleDao,
        QuestEngineType questEngine,
        SkillLearnService skillLearn,
        PetService petService,
        SiegeService siegeService,
        HousingService housingService,
        HousingBidService housingBidService,
        IOptions<HousingOptions> housingOptions,
        IHouseObjectCooldownsDao houseObjectCooldownsDao,
        StigmaService stigmaService,
        TownService townService,
        IOptions<TownOptions> townOptions,
        DisputeLandService disputeLandService)
    {
        _townService   = townService;
        _townOptions   = townOptions;
        _disputeLandService = disputeLandService;
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
        _questEngine   = questEngine;
        _skillLearn    = skillLearn;
        _petService    = petService;
        _siegeService  = siegeService;
        _housingService = housingService;
        _housingBidService = housingBidService;
        _housingOptions = housingOptions;
        _houseObjectCooldownsDao = houseObjectCooldownsDao;
        _stigmaService  = stigmaService;
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

        // Java PlayerController.validateLoginZone() — relocate a player who logged in inside a hostile-
        // owned/besieged fortress zone (or without rift access to Tiamaranta's Eye) to their bind point.
        // SiegeService.ValidateLoginZone self-gates on GameServer:Siege:Enable (no-op/true when disabled).
        if (!_siegeService.ValidateLoginZone(player))
            player.Position = bindPos;

        var tpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);

        player.BasePhysicalAttack   = tpl?.MainHandAttack    ?? 0;
        player.BasePhysicalAccuracy = tpl?.MainHandAccuracy ?? 200;
        player.BaseCritRating       = tpl?.MainHandCritRate  ?? 100;
        player.BaseEvasion          = tpl?.Evasion           ?? 200;
        player.BaseMagicAccuracy    = tpl?.MagicAccuracy     ?? 100;
        player.BaseParry            = tpl?.Parry             ?? 0;
        player.BaseBlock            = tpl?.Block             ?? 0;
        player.BaseMagicCritRating  = 0; // Java: MAGICAL_CRITICAL base from class stats; 0 until per-class data available
        player.Appearance  = appearance;
        conn.ActivePlayer  = player;
        conn.State         = GsClientConnection.AionState.IN_GAME;

        // Deterministic auto-learn from the skill tree (levels 1..current); re-derived each login, not persisted.
        _skillLearn.ApplyAutoLearn(player, 1, player.Level);

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

        // Java StigmaService.onPlayerLogin's second pass: after every equipped stigma's skills are
        // granted (above), re-validate each one against the player's current level/quest-unlocked slot
        // count, prerequisite skills and class — auto-unequip anything that no longer qualifies (DB row
        // edited by hand, or slots that unlocked via a quest that was later reset).
        bool stigmaCorrected = false;
        foreach (var equippedItem in player.Inventory.All.Where(i => i.IsEquipped && StigmaService.IsStigmaSlot(i.Slot)).ToList())
        {
            var stigmaTpl = _dataManager.Items.GetTemplate(equippedItem.ItemId);
            if (stigmaTpl is null || !_stigmaService.IsValidForPlayer(player, equippedItem, stigmaTpl))
            {
                equippedItem.IsEquipped = false;
                equippedItem.Slot       = -1;
                stigmaCorrected = true;
            }
        }
        if (stigmaCorrected)
            await _itemDao.SaveAllAsync(objectId, player.Inventory.All, ct);

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

        player.MovementSpeed = tpl?.RunSpeed ?? 6.0f;

        var equippedMainHand = storedItems.FirstOrDefault(i => i.IsEquipped && i.Slot == 1);
        if (equippedMainHand is not null)
        {
            var wpnTpl = _dataManager.Items.GetTemplate(equippedMainHand.ItemId);
            if (wpnTpl?.WeaponStats is { } ws)
            {
                int baseSpd = ws.AttackSpeed > 0 ? ws.AttackSpeed : 1500;
                player.BaseAttackSpeed      = baseSpd;
                player.CurrentAttackSpeed   = baseSpd;
                player.WeaponCastTimeBonus  = wpnTpl.CastTimeBonusPct;
                if (wpnTpl.IsMagicalWeapon)
                {
                    player.MainHandMagicalAtk = (ws.MinDamage + ws.MaxDamage) / 2;
                    player.MainHandMinDmg     = 0;
                    player.MainHandMaxDmg     = 0;
                }
                else
                {
                    player.MainHandMinDmg     = ws.MinDamage;
                    player.MainHandMaxDmg     = ws.MaxDamage;
                    player.MainHandMagicalAtk = 0;
                }
                player.MainHandHitCount   = ws.HitCount > 0 ? ws.HitCount : 1;
                player.MainHandWeaponType = wpnTpl.WeaponTypeName;
            }
        }

        // Off-hand weapon (slot 2 = ItemSlot.SUB_HAND): daggers/swords/maces; shields/orbs/nothing → clear stats
        var equippedSubHand = storedItems.FirstOrDefault(i => i.IsEquipped && i.Slot == 2);
        var subWpnTpl = equippedSubHand is not null ? _dataManager.Items.GetTemplate(equippedSubHand.ItemId) : null;
        if (subWpnTpl?.IsWeapon == true && subWpnTpl.WeaponStats is { } sw2)
        {
            player.OffHandMinDmg     = sw2.MinDamage;
            player.OffHandMaxDmg     = sw2.MaxDamage;
            player.OffHandHitCount   = sw2.HitCount > 0 ? sw2.HitCount : 1;
            player.OffHandWeaponType = subWpnTpl.WeaponTypeName;
            if (sw2.AttackSpeed > 0)
                player.BaseAttackSpeed += sw2.AttackSpeed / 4;
        }
        else
        {
            player.OffHandMinDmg     = 0;
            player.OffHandMaxDmg     = 0;
            player.OffHandHitCount   = 1;
            player.OffHandWeaponType = string.Empty;
        }

        var equipStats = EquipStatsCalculator.Compute(player.Inventory.All.Where(i => i.IsEquipped), _dataManager);
        player.PhysicalDefense              = equipStats.PhysicalDefense;
        player.MagicDefense                 = equipStats.MagicDefense;
        player.BonusMaxHp                   = equipStats.BonusMaxHp;
        player.BonusMaxMp                   = equipStats.BonusMaxMp;
        player.BonusPhysicalAtk             = equipStats.PhysicalAttackBonus;
        player.BonusMagicResist             = equipStats.MagicResistBonus;
        player.BonusMagicAtk                = equipStats.MagicAttackBonus;
        player.BonusEvasion                = equipStats.Evasion;
        player.BonusPhysicalAccuracy       = equipStats.PhysicalAccuracy;
        player.BonusPhysicalCritical       = equipStats.PhysicalCritical;
        player.BonusPhysicalCriticalResist = equipStats.PhysicalCriticalResist;
        player.BonusMagicalAccuracy        = equipStats.MagicalAccuracy;
        player.BonusMagicalCritical        = equipStats.MagicalCritical;
        player.BonusMagicalCriticalResist  = equipStats.MagicalCriticalResist;
        player.BonusAttackSpeedPct         = equipStats.AttackSpeedBonus;
        player.BonusConcentration          = equipStats.Concentration;
        player.BonusMagicBoost             = equipStats.MagicBoost;
        player.BonusMagicSuppression       = equipStats.MagicSuppression;
        player.BonusHealBoost              = equipStats.HealBoost;
        player.BonusParry                  = equipStats.Parry;
        player.BonusBlock                  = equipStats.Block;
        player.BonusStrikeFortitude        = equipStats.StrikeFortitude;
        player.BonusSpellFortitude         = equipStats.SpellFortitude;
        player.BonusMovementSpeedPct       = equipStats.MovementSpeedBonus;
        player.BonusFlySpeedPct            = equipStats.FlySpeedBonus;

        // Apply title stat bonuses on top of equipment bonuses (all combat stats + speed rates)
        if (player.TitleId > 0)
        {
            var titleTpl = _dataManager.Titles.GetTemplate(player.TitleId);
            if (titleTpl is not null)
            {
                player.TitleBonusMaxHp = titleTpl.GetAddStat("MAXHP");
                player.TitleBonusMaxMp = titleTpl.GetAddStat("MAXMP");
                TitleStatsApplicator.Apply(player, titleTpl);
            }
        }

        // M226: accumulate passive skill stat bonuses — activation="PASSIVE" skills apply permanently each session
        foreach (var skillEntry in player.Skills.AllSkills)
        {
            var passiveTpl = _dataManager.Skills.GetTemplate(skillEntry.SkillId);
            if (passiveTpl is null
                || !string.Equals(passiveTpl.Activation, "PASSIVE", StringComparison.OrdinalIgnoreCase)
                || passiveTpl.Effects is null) continue;
            var fx = passiveTpl.Effects;
            int pv;
            if ((pv = fx.MaxHpStatUpDelta)           != 0) player.PassiveBonusMaxHp           += pv;
            if ((pv = fx.MaxMpStatUpDelta)            != 0) player.PassiveBonusMaxMp           += pv;
            if ((pv = fx.PhysAtkStatUpDelta)          != 0) player.PatkStatUpDelta       += pv;
            if ((pv = fx.MagicAtkStatUpDelta)         != 0) player.MagicAtkStatUpDelta   += pv;
            if ((pv = fx.PdefStatUpDelta)             != 0) player.PdefStatUpDelta       += pv;
            if ((pv = fx.EvasionStatUpDelta)          != 0) player.EvasionStatUpDelta    += pv;
            if ((pv = fx.MResistStatUpDelta)          != 0) player.MResistStatUpDelta    += pv;
            if ((pv = fx.PhysAccStatUpDelta)          != 0) player.PhysAccDelta          += pv;
            if ((pv = fx.MagicAccStatUpDelta)         != 0) player.MagicAccDelta         += pv;
            if ((pv = fx.ParryStatUpDelta)            != 0) player.ParryDelta            += pv;
            if ((pv = fx.BlockStatUpDelta)            != 0) player.BlockDelta            += pv;
            if ((pv = fx.PhysCritStatUpDelta)         != 0) player.PhysCritDelta         += pv;
            if ((pv = fx.MagicCritStatUpDelta)        != 0) player.MagicCritDelta        += pv;
            if ((pv = fx.PhysCritResistStatUpDelta)   != 0) player.PhysCritResistDelta   += pv;
            if ((pv = fx.MagicCritResistStatUpDelta)  != 0) player.MagicCritResistDelta  += pv;
            if ((pv = fx.StrikeFortitudeStatUpDelta)  != 0) player.StrikeFortitudeDelta  += pv;
            if ((pv = fx.SpellFortitudeStatUpDelta)   != 0) player.SpellFortitudeDelta   += pv;
            if ((pv = fx.MagicBoostStatUpDelta)       != 0) player.MagicBoostDelta       += pv;
            if ((pv = fx.HealBoostStatUpDelta)        != 0) player.HealBoostDelta        += pv;
            if ((pv = fx.MagicDefStatUpDelta)         != 0) player.MagicDefDelta         += pv;
            if ((pv = fx.ConcentrationStatUpDelta)    != 0) player.ConcentrationDelta    += pv;
            if ((pv = fx.MagicSuppressionStatUpDelta) != 0) player.MagicSuppressionDelta += pv;
            if ((pv = fx.CastTimeStatUpDelta)         != 0) player.CastTimeDelta         += pv;
            if ((pv = fx.AtkSpeedStatUpDelta)         != 0) player.AtkSpeedStatUpDelta   += pv;
            if ((pv = fx.SpeedStatUpPct)              != 0) player.PassiveBonusMovementSpeedPct += pv;
            if ((pv = fx.MaxHpPercentStatUpDelta)     != 0) player.PassiveBonusMaxHpPct         += pv;
            if ((pv = fx.MaxMpPercentStatUpDelta)     != 0) player.PassiveBonusMaxMpPct         += pv;
            if ((pv = fx.BoostHealSkillBoostPct)      != 0) player.PassiveBonusHealSkillBoostPct += pv;
            if ((pv = fx.BoostSpellAttackPct)         != 0) player.PassiveBonusSpellAttackPct    += pv;
        }

        // M288: ArmorMastery — apply conditional pdef% bonus from passive armor proficiency skills
        int armMasteryPct = PassiveArmorMasteryHelper.ComputePct(player, _dataManager);
        if (armMasteryPct > 0)
            player.PhysicalDefense = (int)(player.PhysicalDefense * (1.0 + armMasteryPct / 100.0));

        // M344: ShieldMastery — apply BLOCK% passive bonus when a shield is in slot 2
        {
            var shieldItem = player.Inventory.All.FirstOrDefault(i => i.IsEquipped && i.Slot == 2);
            var shieldTpl  = shieldItem is not null ? _dataManager.Items.GetTemplate(shieldItem.ItemId) : null;
            bool shieldEquipped = shieldTpl?.ArmorTypeName == "SHIELD";
            int smPct = shieldEquipped ? PassiveShieldMasteryHelper.ComputePct(player, _dataManager) : 0;
            player.PassiveBonusBlock = smPct > 0 ? player.BaseBlock * smPct / 100 : 0;
        }

        // M291: WeaponMastery — apply conditional physAtk%/magAtk% bonus from weapon proficiency skills
        var (wpnPhysPct, wpnMagPct) = PassiveWeaponMasteryHelper.Compute(player, _dataManager);
        if (wpnPhysPct > 0)
            player.BasePhysicalAttack = (int)(player.BasePhysicalAttack * (1.0 + wpnPhysPct / 100.0));
        if (wpnMagPct > 0)
            player.MainHandMagicalAtk = (int)(player.MainHandMagicalAtk * (1.0 + wpnMagPct / 100.0));

        // M296: WeaponDual — off-hand effectiveness % from the highest-value wpndual passive skill
        player.DualWieldEffectPct = player.Skills.AllSkills
            .Select(s => _dataManager.Skills.GetTemplate(s.SkillId)?.Effects?.WpnDualEffectPct ?? 0)
            .DefaultIfEmpty(0).Max();

        // Compute derived stats after equipment + title bonuses are fully applied
        player.CurrentAttackSpeed = player.BonusAttackSpeedPct > 0
            ? player.BaseAttackSpeed * 1000 / (1000 + player.BonusAttackSpeedPct)
            : player.BaseAttackSpeed;
        player.MovementSpeed = (tpl?.RunSpeed ?? 6.0f) * (1000 + player.BonusMovementSpeedPct + player.PassiveBonusMovementSpeedPct) / 1000f;

        float ssMult  = player.SoulSicknessMultiplier;
        float hpPctMult = 1f + player.PassiveBonusMaxHpPct / 100f;
        float mpPctMult = 1f + player.PassiveBonusMaxMpPct / 100f;
        player.MaxHp  = (int)(((tpl?.MaxHp ?? 1000) + player.BonusMaxHp + player.PassiveBonusMaxHp + player.TitleBonusMaxHp) * hpPctMult * ssMult);
        player.MaxMp  = (int)(((tpl?.MaxMp ?? 500)  + player.BonusMaxMp + player.PassiveBonusMaxMp + player.TitleBonusMaxMp) * mpPctMult * ssMult);

        // Re-apply soul sickness debuff so skull icon appears on login (Java skill 8291, level = stack count)
        if (player.SoulSicknessCount > 0)
            player.AddEffect(new AbnormalState
            {
                SkillId    = 8291,
                SkillLevel = player.SoulSicknessCount,
                EffectorId = player.ObjectId,
                Expiry     = DateTime.MaxValue
            });
        player.CurrentHp = player.CurrentHp > 0 ? Math.Min(player.CurrentHp, player.MaxHp) : player.MaxHp;
        player.CurrentMp = player.CurrentMp > 0 ? Math.Min(player.CurrentMp, player.MaxMp) : player.MaxMp;
        player.CurrentFp = player.CurrentFp > 0 ? Math.Min(player.CurrentFp, player.EffectiveMaxFp) : player.EffectiveMaxFp;

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
        if (player.ItemCooldowns.Count > 0)
            await conn.SendAsync(new SM_ITEM_COOLDOWN(player.ItemCooldowns), ct);
        await conn.SendAsync(new SM_QUEST_COMPLETED_LIST(player.Quests.Completed), ct);
        await conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);
        await conn.SendAsync(new SM_NEARBY_QUESTS(_questEngine.ComputeNearbyQuests(player)), ct);
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
        await conn.SendAsync(SM_CUBE_UPDATE.StigmaSlots((byte)_stigmaService.GetAdvancedStigmaSlotCount(player)), ct);
        await conn.SendAsync(new SM_INSTANCE_INFO(player), ct);
        await conn.SendAsync(new SM_CHANNEL_INFO(), ct);
        await conn.SendAsync(new SM_PLAYER_SPAWN(player), ct);
        await conn.SendAsync(new SM_GAME_TIME(), ct);

        await conn.SendAsync(new SM_BIND_POINT_INFO(bindPos), ct);

        await conn.SendAsync(SM_TITLE_INFO.TitleList(player.OwnedTitles), ct);
        await conn.SendAsync(new SM_EMOTION_LIST(0), ct);
        await conn.SendAsync(new SM_PRICES(), ct);
        await conn.SendAsync(SM_ABYSS_RANK.ForPlayer(player), ct);
        await conn.SendAsync(new SM_FLY_TIME(player.CurrentFp, player.EffectiveMaxFp), ct);
        await conn.SendAsync(new SM_PACKAGE_INFO_NOTIFY(), ct);

        await conn.SendAsync(new SM_MACRO_LIST(player.ObjectId, player.Macros.Where(kv => kv.Key <= 24)), ct);
        await conn.SendAsync(new SM_MACRO_LIST(player.ObjectId, player.Macros.Where(kv => kv.Key > 24)), ct);

        foreach (var id in _dataManager.Recipes.GetAutoLearnIds(player.Race.ToString()))
            player.KnownRecipes.Add(id);
        var dbRecipes = await _recipeDao.LoadByPlayerIdAsync(player.ObjectId, ct);
        foreach (var id in dbRecipes)
            player.KnownRecipes.Add(id);
        await conn.SendAsync(new SM_RECIPE_LIST(player.KnownRecipes), ct);

        // Toy pets: load owned pets from DB and send the list (Java PetService.onPlayerLogin → SM_PET(0)).
        await _petService.LoadPetsAsync(player, ct);
        await _petService.SendListAsync(player, conn, ct);

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

        // Java TownService.onEnterWorld — gated behind TownOptions.SendTownListOnLogin (default false)
        // until SM_TOWNS_LIST's opcode is confirmed against a live 4.6 client capture; the town-level
        // lookup itself (used by SM_HOUSE_OWNER_INFO) is always functional regardless of this flag.
        if (_townOptions.Value.SendTownListOnLogin)
        {
            var raceTowns = _townService.GetTownsForRace(player.Race);
            if (raceTowns.Count > 0)
                await conn.SendAsync(new SM_TOWNS_LIST(raceTowns), ct);
        }

        // Java DisputeLandService.onLogin — gated behind GameServer:DisputeLand:Enable (default false);
        // no-op until the feature is explicitly turned on, so this never touches the login flow above.
        await _disputeLandService.OnLoginAsync(conn, ct);

        await _siegeService.OnPlayerLoginAsync(player, conn, ct);
        await _housingService.OnPlayerLoginAsync(player, conn, ct);
        if (_housingOptions.Value.Enable)
        {
            await _housingBidService.OnPlayerLoginAsync(player, conn, ct);

            // P3: Java Player.houseObjectCooldownList — loaded once at login, upserted immediately by
            // CM_USE_HOUSE_OBJECT whenever a cooldown is set (see Player.SetHouseObjectCooldown's doc).
            var cooldowns = await _houseObjectCooldownsDao.LoadAsync(player.ObjectId, ct);
            foreach (var (cooldownObjectId, reuseTimeMs) in cooldowns)
                player.HouseObjectCooldowns[cooldownObjectId] = reuseTimeMs;
        }
    }
}
