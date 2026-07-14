using AionLightning.Commons.Events;
using AionLightning.Commons.Network;
using AionLightning.Game.Combat;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Model.Skill;
using AionLightning.Game.Model.Templates.Item;
using AionLightning.Game.Model.Templates.Skill;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using GameWorld = AionLightning.Game.World.World;
using QuestEngineType = AionLightning.Game.QuestEngine.QuestEngine;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client uses a consumable item. Opcode 0xC7.</summary>
public sealed class CM_USE_ITEM : AionClientPacket
{
    // Maps skilluse skill IDs from item_templates.xml to (hpRestorePercent, mpRestorePercent).
    // Values represent percentage of max HP/MP restored instantly.
    private static readonly Dictionary<int, (int Hp, int Mp)> SkillEffects = new()
    {
        { 10202, (15,  0) }, // Minor Life Elixir   lv10
        { 10203, (20,  0) }, // Lesser Life Elixir  lv20
        { 10204, (25,  0) }, // Regular Life Elixir lv30
        { 10205, (30,  0) }, // Greater Life Elixir lv40
        { 10206, (35,  0) }, // Pure Life Elixir    lv50
        { 10207, (40,  0) }, // Odium Life Elixir   lv55
        { 10208, (45,  0) }, // Unique Life Elixir  lv60
        { 10262, ( 0, 15) }, // Minor Mana Elixir   lv10
        { 10263, ( 0, 20) }, // Lesser Mana Elixir  lv20
        { 10264, ( 0, 25) }, // Regular Mana Elixir lv30
        { 10265, ( 0, 30) }, // Greater Mana Elixir lv40
        { 10266, ( 0, 35) }, // Pure Mana Elixir    lv50
        { 10267, ( 0, 40) }, // Odium Mana Elixir   lv55
        { 10268, ( 0, 45) }, // Unique Mana Elixir  lv60
    };

    private readonly GsClientConnection       _conn;
    private readonly IItemDao                 _itemDao;
    private readonly IDataManager             _dataManager;
    private readonly IRecipeDao               _recipeDao;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly SkillLearnService        _skillLearn;
    private readonly IPlayerTitleDao          _titleDao;
    private readonly GameWorld                _world;
    private readonly NpcAiService             _npcAi;
    private readonly IEventBus                _eventBus;
    private readonly QuestEngineType          _questEngine;
    private readonly KiskService              _kiskService;

    private int _uniqueItemId;
    private int _type;
    private int _targetItemId;

    public CM_USE_ITEM(GsClientConnection conn, IItemDao itemDao, IDataManager dataManager,
        IRecipeDao recipeDao, PlayerConnectionRegistry connRegistry, SkillLearnService skillLearn,
        IPlayerTitleDao titleDao, GameWorld world, NpcAiService npcAi,
        IEventBus eventBus, QuestEngineType questEngine, KiskService kiskService)
    {
        _conn         = conn;
        _itemDao      = itemDao;
        _dataManager  = dataManager;
        _recipeDao    = recipeDao;
        _connRegistry = connRegistry;
        _skillLearn   = skillLearn;
        _titleDao     = titleDao;
        _world        = world;
        _npcAi        = npcAi;
        _eventBus     = eventBus;
        _questEngine  = questEngine;
        _kiskService  = kiskService;
    }

    public override void Read(ref PacketReader r)
    {
        _uniqueItemId = r.ReadD();
        _type         = r.ReadC();
        if (_type == 2) _targetItemId = r.ReadD();
        if (_type == 6) _targetItemId = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || player.IsAlreadyDead) return;

        var item = player.Inventory.Get(_uniqueItemId);
        if (item is null) return;

        var template = _dataManager.Items.GetTemplate(item.ItemId);
        if (template is null) return;

        // Quest item use (Java onItemUseEvent): a registered quest may claim this item's use
        // (e.g. start a quest-item quest, play its dialog). If handled, skip normal item use.
        if (await _questEngine.OnItemUseAsync(player, item.ItemId, _conn, ct))
            return;

        // Dye item used on a target item
        if (_type == 2 && template.Actions?.Dye is not null)
        {
            await HandleDyeAsync(player, item, template.Actions.Dye, ct);
            return;
        }

        // Ride (mount) item — fires the quest hook (Java RideAction.act() -> QuestEngine.rideAction,
        // passing the item's own template id). note: only the quest-hook firing is wired here; the
        // full mount visuals (PlayerMode.RIDE, SM_EMOTION RIDE, RideData/RideInfo lookup, cast-time
        // delay, dismount-on-hit observers) are not ported — see RideAction in ItemTemplate.cs.
        if (template.IsRideItem)
        {
            await _questEngine.OnRideAsync(player, item.ItemId, _conn, ct);
            return;
        }

        // Kisk (bindstone) deploy item — spawns a kisk NPC at the player's position (Java
        // ToyPetSpawnAction). note: Java delayed the actual spawn behind a 10s windup cast
        // (cancellable via an ItemUseObserver on move); this port resolves immediately, matching how
        // the Ride/Dye branches above already collapse Java's windup delays. Java's canAct also checked
        // a per-zone "canPutKisk" flag — no such zone data is ported, so only the flying/instance/
        // already-have-a-kisk gates below apply.
        if (template.KiskSpawnNpcId is int kiskNpcId)
        {
            await HandleKiskDeployAsync(player, item, template, kiskNpcId, ct);
            return;
        }

        // Selectable reward box — opens item-choice dialog
        if (template.IsSelectableBox)
        {
            var selectItems = _dataManager.SelectItems.GetSelectItems(player.PlayerClass, item.ItemId);
            if (selectItems is not null)
                await _conn.SendAsync(new SM_SELECT_ITEM_LIST((int)item.UniqueId, selectItems, _dataManager), ct);
            return;
        }

        // Skill book — teaches a skill permanently
        if (template.SkillLearnId is int learnSkillId)
        {
            await HandleSkillBookAsync(player, item, template, learnSkillId, ct);
            return;
        }

        // Recipe book — teaches a crafting recipe permanently
        if (template.CraftLearnRecipeId is int recipeId)
        {
            await HandleRecipeBookAsync(player, item, recipeId, ct);
            return;
        }

        // Title item — grants ownership of a title
        if (template.TitleAddId is int titleId)
        {
            await HandleTitleAddAsync(player, item, titleId, ct);
            return;
        }

        if (template.UseSkillId is not int skillId) return;

        // Buff/food/heal items whose skill isn't a hardcoded HP/MP potion — drive via skill template
        if (!SkillEffects.TryGetValue(skillId, out var effect))
        {
            var skillTpl = _dataManager.Skills.GetTemplate(skillId);
            if (skillTpl?.Effects is not null)
            {
                // M251: item-heal — skills with <healinstant>/<mphealinstant>/<fphealinstant> apply HP/MP/FP restore
                if (skillTpl.Effects.HealEffects is { Count: > 0 } itemHealEffects)
                {
                    await HandleItemHealAsync(player, item, template, skillId, itemHealEffects, ct);
                    return;
                }

                // M273: item-damage — caster-AoE consumables (fire bombs, "Taloc's Tears" style)
                if (skillTpl.Effects.DamageEffects is { Count: > 0 } itemDmgFx
                    && string.Equals(skillTpl.TargetRelation, "ENEMY", StringComparison.OrdinalIgnoreCase)
                    && skillTpl.IsCasterAoe)
                {
                    await HandleItemDamageAsync(player, item, template, skillId, skillTpl, itemDmgFx, ct);
                    return;
                }

                int buffDurationMs = skillTpl.Duration > 0
                    ? skillTpl.Duration
                    : skillTpl.Effects.EffectDuration;
                if (buffDurationMs > 0)
                {
                    await HandleItemBuffAsync(player, item, template, skillId, skillTpl, buffDurationMs, ct);
                    return;
                }
            }
            return;
        }

        // Enforce item use cooldown (delayId groups — e.g. all HP potions share delayId 11)
        var limits = template.UseLimits;
        if (limits is not null && limits.DelayId > 0 && player.IsItemOnCooldown(limits.DelayId))
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.ItemCantUseUntilDelayTime(), ct);
            return;
        }

        // Broadcast item use animation to self and zone peers
        var anim = new SM_ITEM_USAGE_ANIMATION(player.ObjectId, (int)item.UniqueId, item.ItemId);
        try { await _conn.SendAsync(anim, ct); } catch { }
        int worldId = player.Position.WorldId;
        foreach (var peer in _connRegistry.GetAllExcept(player.ObjectId))
            if (peer.ActivePlayer?.Position.WorldId == worldId)
                try { await peer.SendAsync(anim, ct); } catch { }

        bool hpMpChanged = false;
        int itemWorldId  = player.Position.WorldId;

        // Apply HP restore — broadcast so zone peers and group HP bars see the change
        if (effect.Hp > 0 && player.MaxHp > 0)
        {
            int restore = Math.Max(1, player.MaxHp * effect.Hp / 100);
            player.CurrentHp = Math.Min(player.MaxHp, player.CurrentHp + restore);
            var hpStatus = new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.NaturalHp, skillId, restore, SM_ATTACK_STATUS.LogId.Heal);
            try { await _conn.SendAsync(hpStatus, ct); } catch { }
            foreach (var peer in _connRegistry.GetAllExcept(player.ObjectId))
                if (peer.ActivePlayer?.Position.WorldId == itemWorldId)
                    try { await peer.SendAsync(hpStatus, ct); } catch { }
            hpMpChanged = true;
        }

        // Apply MP restore — broadcast so zone peers and group HP bars see the change
        if (effect.Mp > 0 && player.MaxMp > 0)
        {
            int restore = Math.Max(1, player.MaxMp * effect.Mp / 100);
            player.CurrentMp = Math.Min(player.MaxMp, player.CurrentMp + restore);
            var mpStatus = new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.NaturalMp, skillId, restore, SM_ATTACK_STATUS.LogId.MpHeal);
            try { await _conn.SendAsync(mpStatus, ct); } catch { }
            foreach (var peer in _connRegistry.GetAllExcept(player.ObjectId))
                if (peer.ActivePlayer?.Position.WorldId == itemWorldId)
                    try { await peer.SendAsync(mpStatus, ct); } catch { }
            hpMpChanged = true;
        }

        // Update group HP bars for this player
        if (hpMpChanged && player.Group is { } grp)
        {
            var groupUpdate = new SM_GROUP_MEMBER_INFO(grp.GroupId, player, SM_GROUP_MEMBER_INFO.GroupEvent.Update);
            foreach (var m in grp.Members)
            {
                if (m.ObjectId == player.ObjectId) continue;
                var mc = _connRegistry.Get(m.ObjectId);
                if (mc is not null) try { await mc.SendAsync(groupUpdate, ct); } catch { }
            }
        }

        var statTpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
        await _conn.SendAsync(new SM_STATS_INFO(player, statTpl, _dataManager.ExpTable), ct);

        // Start cooldown for this delay group
        if (limits is not null && limits.DelayId > 0 && limits.DelayMs > 0)
            player.SetItemCooldown(limits.DelayId, limits.DelayMs);

        // Consume one charge
        item.Count--;
        if (item.Count <= 0)
        {
            player.Inventory.Remove(item.UniqueId);
            await _itemDao.DeleteAsync(item.UniqueId, ct);
            await _conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
        }
        else
        {
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
        }
    }

    private async ValueTask HandleSkillBookAsync(Player player, Item item, ItemTemplate template,
        int skillId, CancellationToken ct)
    {
        var learnAction = template.Actions!.SkillLearn!;

        // Class restriction: "ALL" or empty means anyone can use it
        if (!string.IsNullOrEmpty(learnAction.ClassRestriction)
            && learnAction.ClassRestriction != "ALL"
            && !player.PlayerClass.ToString().Equals(learnAction.ClassRestriction, StringComparison.OrdinalIgnoreCase))
            return;

        // Level restriction
        if (player.Level < learnAction.RequiredLevel) return;

        // Already known
        if (player.Skills.IsPresent(skillId)) return;

        int skillLevel = _dataManager.SkillTree.GetMaxSkillLevel(skillId, player.PlayerClass, player.Race, player.Level);
        await _skillLearn.LearnSkillAsync(player, skillId, skillLevel, ct: ct);

        string skillName = _dataManager.SkillTree.GetSkillName(skillId) ?? template.Name;
        await _conn.SendAsync(new SM_SKILL_LIST(
            [new PlayerSkillEntry(skillId, skillLevel)],
            isNew: true, msgId: 1300050, skillName: skillName, skillLevel: skillLevel), ct);

        var anim = new SM_ITEM_USAGE_ANIMATION(player.ObjectId, (int)item.UniqueId, item.ItemId);
        try { await _conn.SendAsync(anim, ct); } catch { }
        int bookWorldId = player.Position.WorldId;
        foreach (var peer in _connRegistry.GetAllExcept(player.ObjectId))
            if (peer.ActivePlayer?.Position.WorldId == bookWorldId)
                try { await peer.SendAsync(anim, ct); } catch { }

        // Skill books are non-stackable — always delete on use
        player.Inventory.Remove(item.UniqueId);
        await _itemDao.DeleteAsync(item.UniqueId, ct);
        await _conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
    }

    private async ValueTask HandleRecipeBookAsync(Player player, Item item, int recipeId, CancellationToken ct)
    {
        // Already known
        if (player.KnownRecipes.Contains(recipeId)) return;

        // Must exist in data
        if (_dataManager.Recipes.GetTemplate(recipeId) is null) return;

        player.KnownRecipes.Add(recipeId);
        await _recipeDao.AddRecipeAsync(player.ObjectId, recipeId, ct);
        await _conn.SendAsync(new SM_RECIPE_LIST(player.KnownRecipes), ct);

        var anim = new SM_ITEM_USAGE_ANIMATION(player.ObjectId, (int)item.UniqueId, item.ItemId);
        try { await _conn.SendAsync(anim, ct); } catch { }
        int recipeWorldId = player.Position.WorldId;
        foreach (var peer in _connRegistry.GetAllExcept(player.ObjectId))
            if (peer.ActivePlayer?.Position.WorldId == recipeWorldId)
                try { await peer.SendAsync(anim, ct); } catch { }

        // Recipe books are non-stackable — always delete on use
        player.Inventory.Remove(item.UniqueId);
        await _itemDao.DeleteAsync(item.UniqueId, ct);
        await _conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
    }

    private async ValueTask HandleTitleAddAsync(Player player, Item item, int titleId, CancellationToken ct)
    {
        if (player.OwnedTitles.Contains(titleId)) return;

        player.OwnedTitles.Add(titleId);
        await _titleDao.AddTitleAsync(player.ObjectId, titleId, ct);
        await _conn.SendAsync(SM_TITLE_INFO.AddTitle(titleId), ct);

        var anim = new SM_ITEM_USAGE_ANIMATION(player.ObjectId, (int)item.UniqueId, item.ItemId);
        try { await _conn.SendAsync(anim, ct); } catch { }
        int worldId = player.Position.WorldId;
        foreach (var peer in _connRegistry.GetAllExcept(player.ObjectId))
            if (peer.ActivePlayer?.Position.WorldId == worldId)
                try { await peer.SendAsync(anim, ct); } catch { }

        player.Inventory.Remove(item.UniqueId);
        await _itemDao.DeleteAsync(item.UniqueId, ct);
        await _conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
    }

    private async ValueTask HandleItemBuffAsync(Player player, Item item, ItemTemplate template,
        int skillId, SkillTemplate skillTpl, int durationMs, CancellationToken ct)
    {
        var limits = template.UseLimits;
        if (limits is not null && limits.DelayId > 0 && player.IsItemOnCooldown(limits.DelayId))
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.ItemCantUseUntilDelayTime(), ct);
            return;
        }

        var fx = skillTpl.Effects!;
        int maxHpDelta           = fx.MaxHpStatUpDelta;
        int maxMpDelta           = fx.MaxMpStatUpDelta;
        int mBoostDelta          = fx.MagicBoostStatUpDelta;
        int healBoostDelta       = fx.HealBoostStatUpDelta;
        int physAccDelta         = fx.PhysAccStatUpDelta;
        int magicAccDelta        = fx.MagicAccStatUpDelta;
        int parryDelta           = fx.ParryStatUpDelta;
        int blockDelta           = fx.BlockStatUpDelta;
        int physCritDelta        = fx.PhysCritStatUpDelta;
        int magicCritDelta       = fx.MagicCritStatUpDelta;
        int physCritResistDelta  = fx.PhysCritResistStatUpDelta;
        int magicCritResistDelta = fx.MagicCritResistStatUpDelta;
        int strikeFortDelta      = fx.StrikeFortitudeStatUpDelta;
        int spellFortDelta       = fx.SpellFortitudeStatUpDelta;
        int castTimeDelta        = fx.CastTimeStatUpDelta;
        int concDelta            = fx.ConcentrationStatUpDelta;
        int magicSuppDelta       = fx.MagicSuppressionStatUpDelta;
        int pdefDelta            = fx.PdefStatUpDelta;
        int magicDefDelta        = fx.MagicDefStatUpDelta;
        int patkDelta            = fx.PhysAtkStatUpDelta;
        int magicAtkDelta        = fx.MagicAtkStatUpDelta;
        int evasionDelta         = fx.EvasionStatUpDelta;
        int mresistDelta         = fx.MResistStatUpDelta;
        int atkSpeedDelta        = fx.AtkSpeedStatUpDelta;
        int speedPct             = fx.SpeedStatUpPct;

        var effect = new AbnormalState
        {
            SkillId                 = skillId,
            SkillLevel              = 1,
            EffectorId              = player.ObjectId,
            Expiry                  = DateTime.UtcNow.AddMilliseconds(durationMs),
            MaxHpDelta              = maxHpDelta,
            MaxMpDelta              = maxMpDelta,
            MagicBoostDeltaVal      = mBoostDelta,
            HealBoostDeltaVal       = healBoostDelta,
            PhysAccDeltaVal         = physAccDelta,
            MagicAccDeltaVal        = magicAccDelta,
            ParryDeltaVal           = parryDelta,
            BlockDeltaVal           = blockDelta,
            PhysCritDeltaVal        = physCritDelta,
            MagicCritDeltaVal       = magicCritDelta,
            PhysCritResistDeltaVal  = physCritResistDelta,
            MagicCritResistDeltaVal = magicCritResistDelta,
            StrikeFortitudeDeltaVal = strikeFortDelta,
            SpellFortitudeDeltaVal  = spellFortDelta,
            CastTimeDeltaVal        = castTimeDelta,
            ConcentrationDeltaVal   = concDelta,
            MagicSuppressionDeltaVal = magicSuppDelta,
            PdefStatUpDeltaVal      = pdefDelta,
            MagicDefDeltaVal        = magicDefDelta,
            PatkStatUpDeltaVal      = patkDelta,
            MagicAtkStatUpDeltaVal  = magicAtkDelta,
            EvasionStatUpDeltaVal   = evasionDelta,
            MResistStatUpDeltaVal   = mresistDelta,
            AtkSpeedStatUpDeltaVal  = atkSpeedDelta,
            SpeedStatUpPct          = speedPct,
            PreBuffMovSpeed         = player.MovementSpeed,
        };
        player.AddEffect(effect);

        if (atkSpeedDelta != 0)
        {
            var emo = new SM_EMOTION(player, EmotionType.START_EMOTE2);
            int emoWorld = player.Position.WorldId;
            foreach (var c in _connRegistry.GetAll())
                if (c.ActivePlayer?.Position.WorldId == emoWorld)
                    try { await c.SendAsync(emo, ct); } catch { }
        }
        if (speedPct != 0)
        {
            player.MovementSpeed = Math.Min(12.0f, player.MovementSpeed * (100 + speedPct) / 100f);
            var speedEmo = new SM_EMOTION(player, EmotionType.START_EMOTE2);
            int speedWorld = player.Position.WorldId;
            foreach (var c in _connRegistry.GetAll())
                if (c.ActivePlayer?.Position.WorldId == speedWorld)
                    try { await c.SendAsync(speedEmo, ct); } catch { }
        }

        bool anyStatChange = maxHpDelta != 0 || maxMpDelta != 0 || mBoostDelta != 0 || healBoostDelta != 0 ||
            physAccDelta != 0 || magicAccDelta != 0 || parryDelta != 0 || blockDelta != 0 ||
            physCritDelta != 0 || magicCritDelta != 0 || physCritResistDelta != 0 || magicCritResistDelta != 0 ||
            strikeFortDelta != 0 || spellFortDelta != 0 || castTimeDelta != 0 || concDelta != 0 ||
            magicSuppDelta != 0 || pdefDelta != 0 || magicDefDelta != 0 || patkDelta != 0 ||
            magicAtkDelta != 0 || evasionDelta != 0 || mresistDelta != 0 || atkSpeedDelta != 0;

        if (anyStatChange)
        {
            var statTpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
            await _conn.SendAsync(new SM_STATS_INFO(player, statTpl, _dataManager.ExpTable), ct);
        }

        int buffWorld = player.Position.WorldId;
        var abnormal = new SM_ABNORMAL_EFFECT(player.ObjectId, isPlayer: true, player.GetActiveEffects());
        foreach (var c in _connRegistry.GetAll())
            if (c.ActivePlayer?.Position.WorldId == buffWorld)
                try { await c.SendAsync(abnormal, ct); } catch { }

        // Broadcast item use animation
        var anim = new SM_ITEM_USAGE_ANIMATION(player.ObjectId, (int)item.UniqueId, item.ItemId);
        try { await _conn.SendAsync(anim, ct); } catch { }
        foreach (var peer in _connRegistry.GetAllExcept(player.ObjectId))
            if (peer.ActivePlayer?.Position.WorldId == buffWorld)
                try { await peer.SendAsync(anim, ct); } catch { }

        if (limits is not null && limits.DelayId > 0 && limits.DelayMs > 0)
            player.SetItemCooldown(limits.DelayId, limits.DelayMs);

        item.Count--;
        if (item.Count <= 0)
        {
            player.Inventory.Remove(item.UniqueId);
            await _itemDao.DeleteAsync(item.UniqueId, ct);
            await _conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
        }
        else
        {
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
        }

        // Schedule buff expiry
        var expiryEffect = effect;
        _ = Task.Run(async () =>
        {
            await Task.Delay(durationMs);
            bool statChanged = expiryEffect.MaxHpDelta != 0 || expiryEffect.MaxMpDelta != 0 ||
                expiryEffect.MagicBoostDeltaVal != 0 || expiryEffect.HealBoostDeltaVal != 0 ||
                expiryEffect.PhysAccDeltaVal != 0 || expiryEffect.MagicAccDeltaVal != 0 ||
                expiryEffect.ParryDeltaVal != 0 || expiryEffect.BlockDeltaVal != 0 ||
                expiryEffect.PhysCritDeltaVal != 0 || expiryEffect.MagicCritDeltaVal != 0 ||
                expiryEffect.PhysCritResistDeltaVal != 0 || expiryEffect.MagicCritResistDeltaVal != 0 ||
                expiryEffect.StrikeFortitudeDeltaVal != 0 || expiryEffect.SpellFortitudeDeltaVal != 0 ||
                expiryEffect.CastTimeDeltaVal != 0 || expiryEffect.ConcentrationDeltaVal != 0 ||
                expiryEffect.MagicSuppressionDeltaVal != 0 || expiryEffect.PdefStatUpDeltaVal != 0 ||
                expiryEffect.MagicDefDeltaVal != 0 || expiryEffect.PatkStatUpDeltaVal != 0 ||
                expiryEffect.MagicAtkStatUpDeltaVal != 0 || expiryEffect.EvasionStatUpDeltaVal != 0 ||
                expiryEffect.MResistStatUpDeltaVal != 0 || expiryEffect.AtkSpeedStatUpDeltaVal != 0;

            player.RemoveEffectBySkillId(expiryEffect.SkillId);

            if (expiryEffect.MaxHpDelta != 0)
            {
                int newMaxHp = Math.Max(1, player.MaxHp + player.MaxHpBonusDelta);
                if (player.CurrentHp > newMaxHp) player.CurrentHp = newMaxHp;
            }
            if (expiryEffect.MaxMpDelta != 0)
            {
                int newMaxMp = Math.Max(1, player.MaxMp + player.MaxMpBonusDelta);
                if (player.CurrentMp > newMaxMp) player.CurrentMp = newMaxMp;
            }
            if (expiryEffect.AtkSpeedStatUpDeltaVal != 0)
            {
                var restoreEmo = new SM_EMOTION(player, EmotionType.START_EMOTE2);
                int restoreWorld = player.Position.WorldId;
                foreach (var c in _connRegistry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == restoreWorld)
                        try { await c.SendAsync(restoreEmo); } catch { }
            }
            if (expiryEffect.SpeedStatUpPct != 0)
            {
                var restoreSpeedEmo = new SM_EMOTION(player, EmotionType.START_EMOTE2);
                int restoreSpeedWorld = player.Position.WorldId;
                foreach (var c in _connRegistry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == restoreSpeedWorld)
                        try { await c.SendAsync(restoreSpeedEmo); } catch { }
            }
            if (statChanged)
            {
                var expiryStatTpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
                var expiryConn = _connRegistry.GetAll().FirstOrDefault(c => c.ActivePlayer == player);
                if (expiryConn is not null) try { await expiryConn.SendAsync(new SM_STATS_INFO(player, expiryStatTpl, _dataManager.ExpTable)); } catch { }
            }
            var expired = new SM_ABNORMAL_EFFECT(player.ObjectId, isPlayer: true, player.GetActiveEffects());
            int expiredWorld = player.Position.WorldId;
            foreach (var c in _connRegistry.GetAll())
                if (c.ActivePlayer?.Position.WorldId == expiredWorld)
                    try { await c.SendAsync(expired); } catch { }
        });
    }

    // M251: items whose UseSkillId points to a heal skill (<healinstant>/<mphealinstant>/<fphealinstant>)
    // apply HP/MP/FP restore using the same value/percent semantics as CM_CASTSPELL heal path.
    private async ValueTask HandleItemHealAsync(Player player, Item item, ItemTemplate template,
        int skillId, IReadOnlyList<SkillHealInfo> healEffects, CancellationToken ct)
    {
        var limits = template.UseLimits;
        if (limits is not null && limits.DelayId > 0 && player.IsItemOnCooldown(limits.DelayId))
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.ItemCantUseUntilDelayTime(), ct);
            return;
        }

        // Item use animation broadcast
        var anim = new SM_ITEM_USAGE_ANIMATION(player.ObjectId, (int)item.UniqueId, item.ItemId);
        try { await _conn.SendAsync(anim, ct); } catch { }
        int worldId = player.Position.WorldId;
        foreach (var peer in _connRegistry.GetAllExcept(player.ObjectId))
            if (peer.ActivePlayer?.Position.WorldId == worldId)
                try { await peer.SendAsync(anim, ct); } catch { }

        bool stateChanged = false;
        foreach (var he in healEffects)
        {
            int valueWithDelta = he.BaseValue + he.Delta * 1; // item-skills are level 1
            int maxStat = he.HealType switch
            {
                "hp" => player.MaxHp,
                "mp" => player.MaxMp,
                "fp" => player.EffectiveMaxFp,
                "dp" => 6000,
                "vp" => 6000,
                _    => 0,
            };
            int heal = he.IsPercent ? maxStat * valueWithDelta / 100 : valueWithDelta;
            if (heal <= 0) continue;

            if (he.HealType == "hp")
            {
                heal = Math.Min(heal, player.MaxHp - player.CurrentHp);
                if (heal <= 0) continue;
                player.CurrentHp += heal;
                var pkt = new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.NaturalHp, skillId, heal, SM_ATTACK_STATUS.LogId.Heal);
                try { await _conn.SendAsync(pkt, ct); } catch { }
                foreach (var peer in _connRegistry.GetAllExcept(player.ObjectId))
                    if (peer.ActivePlayer?.Position.WorldId == worldId)
                        try { await peer.SendAsync(pkt, ct); } catch { }
                stateChanged = true;
            }
            else if (he.HealType == "fp")
            {
                heal = Math.Min(heal, player.EffectiveMaxFp - player.CurrentFp);
                if (heal <= 0) continue;
                player.CurrentFp += heal;
                var pkt = new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.NaturalFp, skillId, heal, SM_ATTACK_STATUS.LogId.FpHeal);
                try { await _conn.SendAsync(pkt, ct); } catch { }
                foreach (var peer in _connRegistry.GetAllExcept(player.ObjectId))
                    if (peer.ActivePlayer?.Position.WorldId == worldId)
                        try { await peer.SendAsync(pkt, ct); } catch { }
                stateChanged = true;
            }
            else if (he.HealType == "dp")
            {
                // M252: item-driven DP restore (DP packs / dp scrolls)
                heal = Math.Min(heal, 6000 - player.Dp);
                if (heal <= 0) continue;
                player.Dp += heal;
                try { await _conn.SendAsync(new SM_DP_INFO(player.ObjectId, player.Dp), ct); } catch { }
                stateChanged = true;
            }
            else if (he.HealType == "vp")
            {
                // M272: item-driven VP restore — server-side only (no SM_VP_INFO yet)
                heal = Math.Min(heal, 6000 - player.Vp);
                if (heal <= 0) continue;
                player.Vp += heal;
                stateChanged = true;
            }
            else // mp
            {
                heal = Math.Min(heal, player.MaxMp - player.CurrentMp);
                if (heal <= 0) continue;
                player.CurrentMp += heal;
                var pkt = new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.NaturalMp, skillId, heal, SM_ATTACK_STATUS.LogId.MpHeal);
                try { await _conn.SendAsync(pkt, ct); } catch { }
                foreach (var peer in _connRegistry.GetAllExcept(player.ObjectId))
                    if (peer.ActivePlayer?.Position.WorldId == worldId)
                        try { await peer.SendAsync(pkt, ct); } catch { }
                stateChanged = true;
            }
        }

        // Group HP bar refresh
        if (stateChanged && player.Group is { } grp)
        {
            var groupUpdate = new SM_GROUP_MEMBER_INFO(grp.GroupId, player, SM_GROUP_MEMBER_INFO.GroupEvent.Update);
            foreach (var m in grp.Members)
            {
                if (m.ObjectId == player.ObjectId) continue;
                var mc = _connRegistry.Get(m.ObjectId);
                if (mc is not null) try { await mc.SendAsync(groupUpdate, ct); } catch { }
            }
        }

        var statTpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
        await _conn.SendAsync(new SM_STATS_INFO(player, statTpl, _dataManager.ExpTable), ct);

        // Cooldown
        if (limits is not null && limits.DelayId > 0 && limits.DelayMs > 0)
            player.SetItemCooldown(limits.DelayId, limits.DelayMs);

        // Consume one charge
        item.Count--;
        if (item.Count <= 0)
        {
            player.Inventory.Remove(item.UniqueId);
            await _itemDao.DeleteAsync(item.UniqueId, ct);
            await _conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
        }
        else
        {
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
        }
    }

    // M273: items with caster-AoE damage skills (target_relation=ENEMY, target_type=AREA, first_target=ME)
    // Routes through ApplyDamageAndPublishAsync so observers (Shield/Reflector/etc.) plug in.
    private async ValueTask HandleItemDamageAsync(Player player, Item item, ItemTemplate template,
        int skillId, SkillTemplate skillTpl, IReadOnlyList<SkillDamageInfo> dmgFx, CancellationToken ct)
    {
        var limits = template.UseLimits;
        if (limits is not null && limits.DelayId > 0 && player.IsItemOnCooldown(limits.DelayId))
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.ItemCantUseUntilDelayTime(), ct);
            return;
        }

        var anim = new SM_ITEM_USAGE_ANIMATION(player.ObjectId, (int)item.UniqueId, item.ItemId);
        try { await _conn.SendAsync(anim, ct); } catch { }
        int worldId = player.Position.WorldId;
        foreach (var peer in _connRegistry.GetAllExcept(player.ObjectId))
            if (peer.ActivePlayer?.Position.WorldId == worldId)
                try { await peer.SendAsync(anim, ct); } catch { }

        // Compute base damage from first damage effect (item-skills are level 1, no delta scaling)
        int baseDmg = dmgFx[0].BaseValue + dmgFx[0].Delta;
        if (baseDmg <= 0) return;
        bool isMagical = dmgFx[0].DamageType != "physical";
        var kind = isMagical ? DamageKind.MagicalSkill : DamageKind.PhysicalSkill;

        float aoeR = skillTpl.EffectiveRange;
        if (aoeR <= 0f) aoeR = 12f; // sane default for caster-AoE consumables
        float aoeAlt = Math.Max(1f, skillTpl.EffectiveAltitude);
        int maxHits = skillTpl.TargetMaxCount;
        int hits = 0;

        foreach (var creature in _world.GetAllNpcs())
        {
            if (hits >= maxHits) break;
            if (creature.IsAlreadyDead) continue;
            if (creature.Position.WorldId != worldId) continue;
            float dx = creature.Position.X - player.Position.X;
            float dy = creature.Position.Y - player.Position.Y;
            float dz = creature.Position.Z - player.Position.Z;
            if (dx * dx + dy * dy > aoeR * aoeR) continue;
            if (Math.Abs(dz) > aoeAlt) continue;

            await ((Creature)creature).ApplyDamageAndPublishAsync(player, baseDmg, kind, skillId, _eventBus, ct);
            _npcAi.ForceEngage(creature, player);

            var statusPkt = new SM_ATTACK_STATUS(creature, SM_ATTACK_STATUS.AttackType.Damage, skillId, baseDmg, SM_ATTACK_STATUS.LogId.SpellAtk);
            foreach (var c in _connRegistry.GetAll())
                if (c.ActivePlayer?.Position.WorldId == worldId)
                    try { await c.SendAsync(statusPkt, ct); } catch { }
            hits++;
        }

        if (limits is not null && limits.DelayId > 0 && limits.DelayMs > 0)
            player.SetItemCooldown(limits.DelayId, limits.DelayMs);

        item.Count--;
        if (item.Count <= 0)
        {
            player.Inventory.Remove(item.UniqueId);
            await _itemDao.DeleteAsync(item.UniqueId, ct);
            await _conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
        }
        else
        {
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
        }
    }

    private async ValueTask HandleDyeAsync(Player player, Model.Item.Item dyeItem,
        Model.Templates.Item.DyeAction dye, CancellationToken ct)
    {
        var target = player.Inventory.Get(_targetItemId)
                  ?? player.Inventory.All.FirstOrDefault(i => i.IsEquipped && i.UniqueId == _targetItemId);
        if (target is null) return;

        var targetTemplate = _dataManager.Items.GetTemplate(target.ItemId);
        if (targetTemplate is null) return;

        bool isRemove = string.Equals(dye.Color, "no", StringComparison.OrdinalIgnoreCase);

        if (isRemove && target.DyeColor == 0)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.DyeCannotRemove(), ct);
            return;
        }

        if (!isRemove && !targetTemplate.IsItemDyePermitted)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.DyeCannotDye(), ct);
            return;
        }

        target.DyeColor = isRemove ? 0 : dyeItem.ItemId;

        dyeItem.Count--;
        if (dyeItem.Count <= 0)
        {
            player.Inventory.Remove(dyeItem.UniqueId);
            await _itemDao.DeleteAsync(dyeItem.UniqueId, ct);
            await _conn.SendAsync(new SM_DELETE_ITEM(dyeItem.UniqueId), ct);
        }
        else
        {
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([dyeItem]), ct);
        }

        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([target]), ct);
        await _conn.SendAsync(isRemove ? SM_SYSTEM_MESSAGE.DyeRemoved() : SM_SYSTEM_MESSAGE.DyeApplied(), ct);

        if (target.IsEquipped)
        {
            int worldId    = player.Position.WorldId;
            var appearance = new SM_UPDATE_PLAYER_APPEARANCE(player.ObjectId, player.Inventory.All);
            try { await _conn.SendAsync(appearance, ct); } catch { }
            foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
                if (other.ActivePlayer?.Position.WorldId == worldId)
                    try { await other.SendAsync(appearance, ct); } catch { }
        }
    }

    private async ValueTask HandleKiskDeployAsync(Player player, Model.Item.Item item, ItemTemplate template,
        int kiskNpcId, CancellationToken ct)
    {
        if (player.FlyState != 0)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.CannotUseBindstoneItemWhileFlying(), ct);
            return;
        }

        // Java also barred placement inside an instance (STR_CANNOT_REGISTER_BINDSTONE_FAR_FROM_NPC).
        if (player.Position.InstanceId != 0)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.CannotRegisterBindstoneFarFromNpc(), ct);
            return;
        }

        var limits = template.UseLimits;
        if (limits is not null && limits.DelayId > 0 && player.IsItemOnCooldown(limits.DelayId))
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.ItemCantUseUntilDelayTime(), ct);
            return;
        }

        // KiskService already notifies the player when they already have an active kisk deployed.
        var kisk = await _kiskService.SpawnKiskAsync(player, kiskNpcId, ct);
        if (kisk is null) return;

        var anim = new SM_ITEM_USAGE_ANIMATION(player.ObjectId, (int)item.UniqueId, item.ItemId);
        try { await _conn.SendAsync(anim, ct); } catch { }
        int worldId = player.Position.WorldId;
        foreach (var peer in _connRegistry.GetAllExcept(player.ObjectId))
            if (peer.ActivePlayer?.Position.WorldId == worldId)
                try { await peer.SendAsync(anim, ct); } catch { }

        if (limits is not null && limits.DelayId > 0 && limits.DelayMs > 0)
            player.SetItemCooldown(limits.DelayId, limits.DelayMs);

        item.Count--;
        if (item.Count <= 0)
        {
            player.Inventory.Remove(item.UniqueId);
            await _itemDao.DeleteAsync(item.UniqueId, ct);
            await _conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
        }
        else
        {
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
        }
    }
}
