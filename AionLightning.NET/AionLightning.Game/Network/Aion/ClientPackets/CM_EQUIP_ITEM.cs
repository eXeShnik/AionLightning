using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Model.Templates.Item;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client equips or unequips an item. Opcode 0xC4.</summary>
public sealed class CM_EQUIP_ITEM : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IItemDao           _itemDao;
    private readonly IDataManager       _dataManager;
    private readonly PlayerConnectionRegistry _connRegistry;

    private byte _action;   // 0 = equip, 1 = unequip
    private long _slot;
    private int _itemUniqueId;

    public CM_EQUIP_ITEM(GsClientConnection conn, IItemDao itemDao,
        IDataManager dataManager, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _itemDao      = itemDao;
        _dataManager  = dataManager;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        _action       = (byte)r.ReadC();
        _slot         = r.ReadQ();
        _itemUniqueId = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var item = player.Inventory.Get(_itemUniqueId);
        if (item is null) return;

        var template = _dataManager.Items.GetTemplate(item.ItemId);

        // Stigma items have a separate equip flow — skill grant/removal instead of stat recalculation
        if (template?.IsStigmaItem == true)
        {
            await HandleStigmaAsync(player, item, template, ct);
            return;
        }

        Item? displaced = null;
        if (_action == 0)
        {
            // Equip: if another item already occupies this slot, move it to the bag first
            displaced = player.Inventory.All
                .FirstOrDefault(i => i.UniqueId != item.UniqueId && i.IsEquipped && i.Slot == (int)_slot);
            if (displaced is not null)
            {
                displaced.Slot       = -1;
                displaced.IsEquipped = false;
            }

            item.Slot       = (int)_slot;
            item.IsEquipped = true;
        }
        else
        {
            // Unequip: move to bag
            item.Slot       = -1;
            item.IsEquipped = false;
        }

        // Update WeaponEquipped state and weapon combat stats
        var mainHandItem = player.Inventory.All.FirstOrDefault(i => i.IsEquipped && i.Slot == 1);
        if (mainHandItem is not null)
        {
            player.State |= CreatureState.WeaponEquipped;
            var weaponTpl = _dataManager.Items.GetTemplate(mainHandItem.ItemId);
            if (weaponTpl?.WeaponStats is { } ws)
            {
                int baseSpd = ws.AttackSpeed > 0 ? ws.AttackSpeed : 1500;
                player.BaseAttackSpeed      = baseSpd;
                player.CurrentAttackSpeed   = baseSpd;
                player.WeaponCastTimeBonus  = weaponTpl.CastTimeBonusPct;
                if (weaponTpl.IsMagicalWeapon)
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
                player.MainHandWeaponType = weaponTpl.WeaponTypeName;
            }
        }
        else
        {
            player.State              &= ~CreatureState.WeaponEquipped;
            player.MainHandMinDmg      = 0;
            player.MainHandMaxDmg      = 0;
            player.MainHandHitCount    = 1;
            player.MainHandWeaponType  = string.Empty;
            player.MainHandMagicalAtk  = 0;
            player.BaseAttackSpeed     = 1500;
            player.CurrentAttackSpeed  = 1500;
            player.WeaponCastTimeBonus = 0;
        }

        // Off-hand weapon (slot 2 = ItemSlot.SUB_HAND): daggers/swords/maces only; shields/orbs clear the stats
        var subHandItem = player.Inventory.All.FirstOrDefault(i => i.IsEquipped && i.Slot == 2);
        var subTpl = subHandItem is not null ? _dataManager.Items.GetTemplate(subHandItem.ItemId) : null;
        if (subTpl?.IsWeapon == true && subTpl.WeaponStats is { } sws2)
        {
            player.OffHandMinDmg     = sws2.MinDamage;
            player.OffHandMaxDmg     = sws2.MaxDamage;
            player.OffHandHitCount   = sws2.HitCount > 0 ? sws2.HitCount : 1;
            player.OffHandWeaponType = subTpl.WeaponTypeName;
            // Java PlayerGameStats.getAttackSpeed: dual-wield adds offHand.attackSpeed / 4 to base
            if (sws2.AttackSpeed > 0)
                player.BaseAttackSpeed += sws2.AttackSpeed / 4;
        }
        else
        {
            player.OffHandMinDmg     = 0;
            player.OffHandMaxDmg     = 0;
            player.OffHandHitCount   = 1;
            player.OffHandWeaponType = string.Empty;
        }

        var equipStats = EquipStatsCalculator.Compute(player.Inventory.All.Where(i => i.IsEquipped), _dataManager);
        player.PhysicalDefense             = equipStats.PhysicalDefense;
        player.MagicDefense                = equipStats.MagicDefense;
        player.BonusMaxHp                  = equipStats.BonusMaxHp;
        player.BonusMaxMp                  = equipStats.BonusMaxMp;
        player.BonusPhysicalAtk            = equipStats.PhysicalAttackBonus;
        player.BonusMagicResist            = equipStats.MagicResistBonus;
        player.BonusMagicAtk               = equipStats.MagicAttackBonus;
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

        if (player.TitleId > 0)
        {
            var titleTpl = _dataManager.Titles.GetTemplate(player.TitleId);
            if (titleTpl is not null)
                TitleStatsApplicator.Apply(player, titleTpl);
        }

        // M288: recompute ArmorMastery bonus after any armor type change
        {
            int mastPct = PassiveArmorMasteryHelper.ComputePct(player, _dataManager);
            if (mastPct > 0)
                player.PhysicalDefense = (int)(player.PhysicalDefense * (1.0 + mastPct / 100.0));
        }

        player.CurrentAttackSpeed = player.BonusAttackSpeedPct > 0
            ? player.BaseAttackSpeed * 1000 / (1000 + player.BonusAttackSpeedPct)
            : player.BaseAttackSpeed;

        var statTpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
        player.MovementSpeed = (statTpl?.RunSpeed ?? 6.0f) * (1000 + player.BonusMovementSpeedPct + player.PassiveBonusMovementSpeedPct) / 1000f;
        float ssMult = player.SoulSicknessMultiplier;
        player.MaxHp = (int)(((statTpl?.MaxHp ?? 1000) + player.BonusMaxHp + player.PassiveBonusMaxHp + player.TitleBonusMaxHp) * ssMult);
        player.MaxMp = (int)(((statTpl?.MaxMp ?? 500)  + player.BonusMaxMp + player.PassiveBonusMaxMp + player.TitleBonusMaxMp) * ssMult);

        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);

        // Notify self of updated item state(s)
        var changed = displaced is not null ? new[] { item, displaced } : new[] { item };
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM(changed), ct);

        // Refresh stats panel on the client (reflects any changes after equip toggle)
        await _conn.SendAsync(new SM_STATS_INFO(player, statTpl, _dataManager.ExpTable), ct);

        // Broadcast appearance change to players in the same zone
        var appearance = new SM_UPDATE_PLAYER_APPEARANCE(player.ObjectId, player.Inventory.All);
        await _conn.SendAsync(appearance, ct);
        int worldId = player.Position.WorldId;
        foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
            if (other.ActivePlayer?.Position.WorldId == worldId)
                try { await other.SendAsync(appearance, ct); } catch { }
    }

    // Handles equip/unequip of stigma stones: shard consumption, skill grant/removal, skill list update.
    // Stigma items do not affect combat stats or appearance — only the player's skill list.
    private async ValueTask HandleStigmaAsync(Player player, Item item, ItemTemplate template, CancellationToken ct)
    {
        var stigma = template.Stigma!;

        if (_action == 0) // equip
        {
            // Validate shard count before modifying any state
            if (stigma.Shard > 0)
            {
                const int ShardItemId = 141000001;
                var shardItem = player.Inventory.FindByItemId(ShardItemId);
                if (shardItem is null || shardItem.Count < stigma.Shard)
                {
                    await _conn.SendAsync(SM_SYSTEM_MESSAGE.StigmaNotEnoughShards(), ct);
                    return;
                }
            }

            // If another stigma is already in this slot, remove its skills first
            var displaced = player.Inventory.All
                .FirstOrDefault(i => i.UniqueId != item.UniqueId && i.IsEquipped && i.Slot == (int)_slot);
            if (displaced is not null)
            {
                var dispTpl = _dataManager.Items.GetTemplate(displaced.ItemId);
                if (dispTpl?.Stigma is { } dispStigma)
                    foreach (var (_, skillId) in dispStigma.GetSkills())
                        player.Skills.RemoveStigmaSkill(skillId);
                displaced.Slot       = -1;
                displaced.IsEquipped = false;
            }

            item.Slot       = (int)_slot;
            item.IsEquipped = true;

            // Consume shards (already validated above, so !null && .Count >= shard)
            if (stigma.Shard > 0)
            {
                const int ShardItemId = 141000001;
                var shardItem = player.Inventory.FindByItemId(ShardItemId)!;
                if (shardItem.Count == stigma.Shard)
                {
                    player.Inventory.Remove(shardItem.UniqueId);
                    await _itemDao.DeleteAsync(shardItem.UniqueId, ct);
                }
                else
                    shardItem.Count -= stigma.Shard;
            }

            // Grant stigma skills and collect the newly added entries for the response packet
            var granted = new List<Model.Skill.PlayerSkillEntry>();
            foreach (var (skillLevel, skillId) in stigma.GetSkills())
            {
                player.Skills.AddSkill(skillId, skillLevel, isStigma: true);
                var entry = player.Skills.GetEntry(skillId);
                if (entry is not null) granted.Add(entry);
            }

            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
            if (granted.Count > 0)
                await _conn.SendAsync(new SM_SKILL_LIST(granted, isNew: true), ct);
        }
        else // unequip
        {
            foreach (var (_, skillId) in stigma.GetSkills())
                player.Skills.RemoveStigmaSkill(skillId);

            item.Slot       = -1;
            item.IsEquipped = false;

            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
            // Full skill list refresh so the client removes the stigma skills from the skill bar
            await _conn.SendAsync(new SM_SKILL_LIST(player.Skills.AllSkills, isNew: false), ct);
        }
    }
}
