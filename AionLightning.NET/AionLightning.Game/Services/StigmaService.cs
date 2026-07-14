using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Skill;
using AionLightning.Game.Model.Templates.Item;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>
/// Stigma stone equip/unequip (Java services.StigmaService). Stigmas are socketed into fixed slot
/// bitmasks (Java ItemSlot STIGMA1..6 / ADV_STIGMA1..6) that unlock progressively by level and quest
/// completion, and advanced stigmas require the player to already know a prerequisite skill group.
/// Equipping/unequipping grants or removes the stone's skills through the same in-memory
/// <see cref="PlayerSkillList"/> the rest of the skill system uses (stigma skills are never written to
/// player_skills — they are re-derived from equipped items at every login, see
/// PlayerEnterWorldService.EnterWorldAsync).
/// </summary>
public sealed class StigmaService
{
    public const int ShardItemId = 141000001;

    // Java ItemSlot bit positions. STIGMA3..6 and every ADV_STIGMA slot sit above bit 31, which is why
    // Model.Item.Item.Slot is a long rather than an int (see Item.cs).
    private static readonly long[] RegularSlotMasks  = [1L << 30, 1L << 31, 1L << 32, 1L << 33, 1L << 34, 1L << 35];
    private static readonly long[] AdvancedSlotMasks  = [1L << 47, 1L << 48, 1L << 49, 1L << 50, 1L << 51, 1L << 52];

    // Java Equipment.hasDualWeaponEquipped(ItemSlot.LEFT_HAND) — SUB_HAND / SUB_OFF_HAND bit positions.
    private const long SubHandSlot    = 1L << 1;
    private const long SubOffHandSlot = 1L << 18;

    private readonly IItemDao              _itemDao;
    private readonly IDataManager          _dataManager;
    private readonly ILogger<StigmaService> _logger;

    public StigmaService(IItemDao itemDao, IDataManager dataManager, ILogger<StigmaService> logger)
    {
        _itemDao     = itemDao;
        _dataManager = dataManager;
        _logger      = logger;
    }

    public static bool IsRegularStigmaSlot(long slot)  => Array.IndexOf(RegularSlotMasks, slot)  >= 0;
    public static bool IsAdvancedStigmaSlot(long slot) => Array.IndexOf(AdvancedSlotMasks, slot) >= 0;
    public static bool IsStigmaSlot(long slot)         => IsRegularStigmaSlot(slot) || IsAdvancedStigmaSlot(slot);

    /// <summary>Number of unlocked regular stigma slots (Java StigmaService.getPossibleStigmaCount).
    /// note: Java also grants 6 unconditionally for players holding MembershipConfig.STIGMA_SLOT_QUEST
    /// permission (a donator/GM bypass) — this port has no membership-tier config, so that bypass is
    /// not ported.</summary>
    public int GetRegularStigmaSlotCount(Player player)
    {
        if (player.Level < 20) return 0;

        // Stigma quest: Elyos 1929, Asmodian 2900. Java also accepts the quest still in-progress at a
        // specific step (98/99) — the step just before the final reward dialog.
        bool questUnlocked = player.Race == Race.ELYOS
            ? IsCompleteOrAtStep(player, 1929, 98)
            : IsCompleteOrAtStep(player, 2900, 99);
        if (!questUnlocked) return 0;

        return player.Level switch
        {
            < 30 => 2,
            < 40 => 3,
            < 50 => 4,
            < 55 => 5,
            _    => 6,
        };
    }

    /// <summary>Number of unlocked advanced stigma slots (Java StigmaService.getPossibleAdvencedStigmaCount).</summary>
    public int GetAdvancedStigmaSlotCount(Player player)
    {
        if (player.Level < 45) return 0;

        if (player.Race == Race.ELYOS)
        {
            if (!IsCompleteQuest(player, 1929)) return 0;
            if (IsCompleteQuest(player, 11550)) return 6;
            if (IsCompleteQuest(player, 30217) || IsCompleteQuest(player, 11276)) return 5;
            if (IsCompleteQuest(player, 11049)) return 4;
            if (IsCompleteQuest(player, 3932)) return 3;
            if (IsCompleteQuest(player, 3931)) return 2;
            if (IsCompleteQuest(player, 3930)) return 1;
        }
        else
        {
            if (!IsCompleteQuest(player, 2900)) return 0;
            if (IsCompleteQuest(player, 21550)) return 6;
            if (IsCompleteQuest(player, 30317) || IsCompleteQuest(player, 21278)) return 5;
            if (IsCompleteQuest(player, 21049)) return 4;
            if (IsCompleteQuest(player, 4936)) return 3;
            if (IsCompleteQuest(player, 4935)) return 2;
            if (IsCompleteQuest(player, 4934)) return 1;
        }
        return 0;
    }

    /// <summary>Unlocked slot count relevant to <paramref name="slot"/> — regular or advanced, whichever
    /// bitmask the slot belongs to. Requested by the task as a single entry point; Java keeps the two
    /// counts (getPossibleStigmaCount / getPossibleAdvencedStigmaCount) separate since they gate
    /// different quest chains and level thresholds.</summary>
    public int GetStigmaSlotSize(Player player, long slot) =>
        IsAdvancedStigmaSlot(slot) ? GetAdvancedStigmaSlotCount(player) : GetRegularStigmaSlotCount(player);

    /// <summary>True when the given stigma slot index is within the player's currently unlocked count
    /// (Java StigmaService.isPossibleEquippedStigma).</summary>
    public bool IsSlotUnlocked(Player player, long slot)
    {
        int idx = Array.IndexOf(RegularSlotMasks, slot);
        if (idx >= 0) return idx < GetRegularStigmaSlotCount(player);

        idx = Array.IndexOf(AdvancedSlotMasks, slot);
        if (idx >= 0) return idx < GetAdvancedStigmaSlotCount(player);

        return false;
    }

    /// <summary>True when every prerequisite group on an advanced stigma is satisfied — the player must
    /// already know at least one skill from each declared &lt;require_skill&gt; group (Java
    /// StigmaService.notifyEquipAction's neededSkillsCount loop). Regular stigmas have no groups, so this
    /// is vacuously true for them.</summary>
    public static bool HasRequiredPrerequisiteSkills(Player player, StigmaTemplate stigma)
    {
        foreach (var group in stigma.RequireSkill)
        {
            bool anyKnown = false;
            foreach (var skillId in group.SkillIds)
            {
                if (player.Skills.IsPresent(skillId)) { anyKnown = true; break; }
            }
            if (!anyKnown) return false;
        }
        return true;
    }

    /// <summary>Full equip-time validity check for an already-equipped stigma (slot unlock, prerequisite
    /// skills, class restriction) — reused both by <see cref="EquipStigmaAsync"/> and by the login-time
    /// hack/desync guard in PlayerEnterWorldService (Java StigmaService.onPlayerLogin).</summary>
    public bool IsValidForPlayer(Player player, Item item, ItemTemplate template)
    {
        if (template.Stigma is not { } stigma) return false;
        return IsSlotUnlocked(player, item.Slot)
            && HasRequiredPrerequisiteSkills(player, stigma)
            && template.IsClassSpecific(player.PlayerClass);
    }

    /// <summary>Equips a stigma stone into <paramref name="slot"/> (Java StigmaService.notifyEquipAction,
    /// invoked from Equipment.equipItem). Validates slot unlock, class restriction, shard availability and
    /// advanced-stigma prerequisites; on success, displaces whatever stone currently sits in that slot,
    /// consumes shards, grants the new stone's skills via the same in-memory skill list the rest of the
    /// skill system uses, persists the inventory, and notifies the client.</summary>
    public async ValueTask<bool> EquipStigmaAsync(Player player, Item item, ItemTemplate template, long slot,
        GsClientConnection conn, CancellationToken ct)
    {
        if (template.Stigma is not { } stigma)
            return false;

        if (!IsSlotUnlocked(player, slot))
        {
            _logger.LogWarning("Possible client hack: player {PlayerId} tried to equip stigma into locked slot {Slot}",
                player.ObjectId, slot);
            return false;
        }

        if (!template.IsClassSpecific(player.PlayerClass))
        {
            await conn.SendAsync(SM_SYSTEM_MESSAGE.CannotUseItemInvalidClass(), ct);
            return false;
        }

        if (stigma.Shard > 0)
        {
            var shardItem = player.Inventory.FindByItemId(ShardItemId);
            if (shardItem is null || shardItem.Count < stigma.Shard)
            {
                await conn.SendAsync(SM_SYSTEM_MESSAGE.StigmaNotEnoughShards(), ct);
                return false;
            }
        }

        if (!HasRequiredPrerequisiteSkills(player, stigma))
        {
            _logger.LogWarning("Possible client hack: player {PlayerId} tried to equip advanced stigma {ItemId} without its prerequisite skill",
                player.ObjectId, item.ItemId);
            return false;
        }

        // If another stigma is already in this slot, remove its granted skills before overwriting it.
        var displaced = player.Inventory.All
            .FirstOrDefault(i => i.UniqueId != item.UniqueId && i.IsEquipped && i.Slot == slot);
        if (displaced is not null)
        {
            var dispTpl = _dataManager.Items.GetTemplate(displaced.ItemId);
            if (dispTpl?.Stigma is { } dispStigma)
                foreach (var (skillLevel, skillId) in dispStigma.GetSkills())
                {
                    player.Skills.RemoveStigmaSkill(skillId);
                    player.RemoveEffectBySkillId(skillId);
                    try { await conn.SendAsync(new SM_SKILL_REMOVE(skillId, skillLevel, isStigma: true), ct); } catch { }
                }
            displaced.Slot       = -1;
            displaced.IsEquipped = false;
        }

        item.Slot       = slot;
        item.IsEquipped = true;

        if (stigma.Shard > 0)
        {
            var shardItem = player.Inventory.FindByItemId(ShardItemId)!;
            if (shardItem.Count == stigma.Shard)
            {
                player.Inventory.Remove(shardItem.UniqueId);
                await _itemDao.DeleteAsync(shardItem.UniqueId, ct);
            }
            else
            {
                shardItem.Count -= stigma.Shard;
                try { await conn.SendAsync(new SM_INVENTORY_UPDATE_ITEM(shardItem, SM_INVENTORY_UPDATE_ITEM.UpdateType.DecItemUse), ct); } catch { }
            }
        }

        var granted = new List<PlayerSkillEntry>();
        foreach (var (skillLevel, skillId) in stigma.GetSkills())
        {
            player.Skills.AddSkill(skillId, skillLevel, isStigma: true);
            var entry = player.Skills.GetEntry(skillId);
            if (entry is not null) granted.Add(entry);
        }

        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await conn.SendAsync(new SM_INVENTORY_ADD_ITEM(displaced is not null ? [item, displaced] : [item]), ct);
        if (granted.Count > 0)
            await conn.SendAsync(new SM_SKILL_LIST(granted, isNew: true), ct);
        await conn.SendAsync(SM_CUBE_UPDATE.StigmaSlots((byte)GetAdvancedStigmaSlotCount(player)), ct);

        return true;
    }

    /// <summary>Unequips a stigma stone (Java StigmaService.notifyUnequipAction, invoked from
    /// Equipment.unEquipItem). note: Java does NOT cascade-remove other advanced stigmas whose
    /// prerequisite would become unmet — it BLOCKS the unequip instead (system message 1300410) until
    /// the dependent stone is removed first. This port follows the real Java behavior rather than
    /// cascading, since silently stripping a player's other stigma skills as a side effect would be a
    /// deviation from retail, not a faithful port.</summary>
    public async ValueTask<bool> UnequipStigmaAsync(Player player, Item item, ItemTemplate template,
        GsClientConnection conn, CancellationToken ct)
    {
        if (template.Stigma is not { } stigma)
            return false;

        // Dual-wield stigma stones (Rush Trinity / Dual Wielding) cannot be removed while a second
        // one-handed weapon is still equipped in the off-hand.
        if ((item.ItemId == 140000007 || item.ItemId == 140000005) && HasOffHandWeaponEquipped(player))
        {
            await conn.SendAsync(SM_SYSTEM_MESSAGE.StigmaCannotUnequipWhileDualWielding(), ct);
            return false;
        }

        if (BlocksOtherEquippedStigma(player, item, stigma, out var dependentName))
        {
            await conn.SendAsync(SM_SYSTEM_MESSAGE.StigmaRequiredForOtherStigma(template.Name, dependentName), ct);
            return false;
        }

        foreach (var (skillLevel, skillId) in stigma.GetSkills())
        {
            player.Skills.RemoveStigmaSkill(skillId);
            player.RemoveEffectBySkillId(skillId);
            var skillName = _dataManager.Skills.GetTemplate(skillId)?.Name ?? string.Empty;
            try { await conn.SendAsync(SM_SYSTEM_MESSAGE.StigmaSkillRemoved(skillName), ct); } catch { }
            try { await conn.SendAsync(new SM_SKILL_REMOVE(skillId, skillLevel, isStigma: true), ct); } catch { }
        }

        item.Slot       = -1;
        item.IsEquipped = false;

        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
        await conn.SendAsync(new SM_SKILL_LIST(player.Skills.AllSkills, isNew: false), ct);
        await conn.SendAsync(SM_CUBE_UPDATE.StigmaSlots((byte)GetAdvancedStigmaSlotCount(player)), ct);

        return true;
    }

    private bool HasOffHandWeaponEquipped(Player player)
    {
        foreach (var equipped in player.Inventory.All)
        {
            if (!equipped.IsEquipped || (equipped.Slot != SubHandSlot && equipped.Slot != SubOffHandSlot)) continue;
            var tpl = _dataManager.Items.GetTemplate(equipped.ItemId);
            if (tpl is { IsWeapon: true, IsTwoHandWeapon: false }) return true;
        }
        return false;
    }

    // Java: for every other equipped stigma, if one of its require_skill groups references a skill this
    // stone grants, block the unequip (the dependent stone would lose its prerequisite).
    private bool BlocksOtherEquippedStigma(Player player, Item resultItem, StigmaTemplate resultStigma, out string dependentItemName)
    {
        var grantedSkillIds = resultStigma.GetSkills().Select(s => s.SkillId).ToList();

        foreach (var other in player.Inventory.All.Where(i => i.IsEquipped && i.UniqueId != resultItem.UniqueId))
        {
            var otherTpl = _dataManager.Items.GetTemplate(other.ItemId);
            if (otherTpl?.Stigma is not { } otherStigma) continue;

            foreach (var group in otherStigma.RequireSkill)
            {
                if (grantedSkillIds.Any(group.SkillIds.Contains))
                {
                    dependentItemName = otherTpl.Name;
                    return true;
                }
            }
        }

        dependentItemName = string.Empty;
        return false;
    }

    private static bool IsCompleteQuest(Player player, int questId) =>
        player.Quests.Get(questId)?.Status == QuestStatus.COMPLETE;

    // Java: isCompleteQuest(id) || (status == START && questVars.getQuestVars() == step). The packed
    // step value is QuestEntry.Step (Java QuestVars.getQuestVars()), not a single slot var.
    private static bool IsCompleteOrAtStep(Player player, int questId, int step)
    {
        var entry = player.Quests.Get(questId);
        if (entry is null) return false;
        return entry.Status == QuestStatus.COMPLETE || (entry.Status == QuestStatus.START && entry.Step == step);
    }
}
