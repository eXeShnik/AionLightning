using AionLightning.Game.Model.Group;
using AionLightning.Game.Model.Item;
using LegionModel = AionLightning.Game.Model.Legion.Legion;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Skill;

namespace AionLightning.Game.Model;

public sealed class Player : Creature
{
    public int AccountId { get; init; }
    public Race Race { get; init; }
    public Gender Gender { get; init; }
    public PlayerClass PlayerClass { get; set; }
    public byte Level { get; set; }
    public long Exp { get; set; }
    public int TitleId      { get; set; } = -1;
    public int BonusTitleId { get; set; } = -1;

    // Flight points — not persisted, starts full each login
    public int MaxFp     { get; set; } = 4000;
    public int CurrentFp { get; set; } = 4000;
    public PlayerAppearance Appearance { get; set; } = new();
    public PlayerSkillList  Skills          { get; } = new();
    public PlayerInventory  Inventory       { get; } = new();
    public PlayerInventory  Warehouse       { get; } = new();
    public PlayerInventory  AccountWarehouse { get; } = new();
    public PlayerQuestList  Quests       { get; } = new();
    public HashSet<int>     KnownRecipes  { get; } = new();
    public HashSet<int>     OwnedTitles   { get; } = new();
    public DateTime CreationDate { get; init; }
    public DateTime? LastOnline { get; set; }
    public DateTime? DeletionDate { get; set; }

    /// <summary>Unix timestamp sent to client; 0 means not pending deletion.</summary>
    public int DeletionTime => DeletionDate.HasValue
        ? (int)new DateTimeOffset(DeletionDate.Value).ToUnixTimeSeconds()
        : 0;

    // Party group — null when not in a group
    public PlayerGroup? Group { get; set; }

    // Legion (guild) — null when not in a legion
    public LegionModel? Legion { get; set; }

    // Bind point (Obelisk) — null means no bind, CM_REVIVE stays in place
    public Position? BindPosition { get; set; }

    // Client display/social settings (from CM_CUSTOM_SETTINGS)
    public int DisplaySettings { get; set; }
    public int DenySettings    { get; set; }

    // Bio note visible to friends in their friend list
    public string Note { get; set; } = string.Empty;

    // UI settings blobs — persisted via IPlayerSettingsDao, sent as SM_UI_SETTINGS on login
    public byte[]? UiSettings   { get; set; }
    public byte[]? Shortcuts    { get; set; }
    public byte[]? HouseBuddies { get; set; }

    // Macros — position (1-48) → macro XML blob sent by client
    public Dictionary<int, string> Macros { get; } = new();

    // Movement state — updated by CM_MOVE, read by SM_MOVE broadcast
    public byte MovementMask { get; set; }
    public float VectorX { get; set; }
    public float VectorY { get; set; }
    public float VectorZ { get; set; }
    public float TargetX2 { get; set; }
    public float TargetY2 { get; set; }
    public float TargetZ2 { get; set; }

    // Abyss rank system
    public long AbyssPoints { get; set; }
    public int  AbyssRank   { get; set; } = 1;
    public int  AbyssMaxRank    { get; set; } = 1;
    public int  AbyssAllKill    { get; set; }
    public int  AbyssDailyKill  { get; set; }
    public long AbyssDailyAp    { get; set; }
    public int  AbyssWeeklyKill { get; set; }
    public long AbyssWeeklyAp   { get; set; }
    public int  AbyssLastKill   { get; set; }
    public long AbyssLastAp     { get; set; }

    // Base physical attack from class/level stat template (updated on login and level-up)
    public int BasePhysicalAttack { get; set; }

    // Base attack speed from the equipped weapon template (unmodified by accessories); 1500 when no weapon
    public int BaseAttackSpeed { get; set; } = 1500;

    // BOOST_CASTING_TIME percentage bonus from equipped weapon (bonus="true" in XML); 0 when no weapon
    public int WeaponCastTimeBonus { get; set; }

    // Sum of ATTACK_SPEED percentage bonuses from all equipped accessories (bonus="true" in XML)
    public int BonusAttackSpeedPct { get; set; }

    // Equipped main-hand weapon damage range; both 0 when no weapon is equipped or a magical weapon
    public int MainHandMinDmg { get; set; }
    public int MainHandMaxDmg { get; set; }

    // Magical attack bonus from equipped magical weapon (staff/orb/mace/etc.); average of min/max weapon damage
    public int MainHandMagicalAtk { get; set; }

    // Sum of PHYSICAL_DEFENSE from all equipped items; applied in mitigation formula
    public int PhysicalDefense { get; set; }

    // Sum of MAGICAL_DEFEND from all equipped items; applied in magic mitigation formula
    public int MagicDefense { get; set; }

    // Flat HP/MP bonus from equipped items (MAXHP/MAXMP modifiers, non-percentage)
    public int BonusMaxHp { get; set; }
    public int BonusMaxMp { get; set; }

    // Equipment stat bonuses (sum of PHYSICAL_ATTACK, MAGICAL_RESIST, MAGICAL_ATTACK from all equipped items)
    public int BonusPhysicalAtk { get; set; }
    public int BonusMagicResist { get; set; }
    public int BonusMagicAtk    { get; set; }

    // Equipment stat bonuses (EVASION, PHYSICAL_ACCURACY, PHYSICAL_CRITICAL, PHYSICAL_CRITICAL_RESIST)
    public int BonusEvasion                { get; set; }
    public int BonusPhysicalAccuracy       { get; set; }
    public int BonusPhysicalCritical       { get; set; }
    public int BonusPhysicalCriticalResist { get; set; }

    // Equipment stat bonuses (MAGICAL_ACCURACY, MAGICAL_CRITICAL, MAGICAL_CRITICAL_RESIST)
    public int BonusMagicalAccuracy       { get; set; }
    public int BonusMagicalCritical       { get; set; }
    public int BonusMagicalCriticalResist { get; set; }

    // Equipment stat bonuses (CONCENTRATION, BOOST_MAGICAL_SKILL, MAGIC_SKILL_BOOST_RESIST, HEAL_BOOST)
    public int BonusConcentration    { get; set; }
    public int BonusMagicBoost       { get; set; }
    public int BonusMagicSuppression { get; set; }
    public int BonusHealBoost        { get; set; }

    // Base combat stats from class stat template (set at login, constant until level-up)
    public int BasePhysicalAccuracy { get; set; }
    public int BaseCritRating       { get; set; }
    public int BaseEvasion          { get; set; }
    public int BaseMagicAccuracy    { get; set; }
    public int BaseMagicCritRating  { get; set; }

    // Flat HP/MP bonus from the active title (from player_titles.xml <add> modifiers)
    public int TitleBonusMaxHp { get; set; }
    public int TitleBonusMaxMp { get; set; }

    // Divine Power — drained to 0 on bind revive (cleared via CM_REVIVE), max 8000
    public int Dp { get; set; }

    // Soul sickness stacks (0-10); each stack reduces MaxHp/MaxMp by 5 % (Java deathCount)
    public int SoulSicknessCount { get; set; }

    /// <summary>Multiplier applied to base MaxHp/MaxMp per soul sickness stack (5% reduction each, min 0.5).</summary>
    public float SoulSicknessMultiplier => Math.Max(0.5f, 1f - SoulSicknessCount * 0.05f);

    // Inventory cube expansion — NPC-purchased expands; each adds 9 slots (Java CUBE_SPACE=9)
    public int NpcExpands   { get; set; }
    public int QuestExpands { get; set; }

    /// <summary>Total cube capacity: 27 base + 9 slots per expand level.</summary>
    public int CubeCapacity => 27 + (NpcExpands + QuestExpands) * 9;

    // Motion slots 1-5 (combat animation style); motionId 0 = none
    public Dictionary<byte, short> ActiveMotions { get; } = new();

    // Private store — null when store is closed
    public List<PrivateStoreItem>? StoreItems { get; set; }
    public string StoreName { get; set; } = string.Empty;

    // Recoverable XP lost from death (soul sickness) — visible on XP bar as the "grey" portion.
    // Cleared as the player earns XP back. Capped at 25% of expNeeded-for-next-level.
    public long ExpRecoverable { get; set; }

    // Item use cooldowns: delayId → expiry UTC time (mirrors Java addItemCoolDown / isItemUseDisabled)
    public Dictionary<int, DateTime> ItemCooldowns { get; } = new();

    public bool IsItemOnCooldown(int delayId) =>
        ItemCooldowns.TryGetValue(delayId, out var expiry) && DateTime.UtcNow < expiry;

    public void SetItemCooldown(int delayId, int delayMs) =>
        ItemCooldowns[delayId] = DateTime.UtcNow.AddMilliseconds(delayMs);

    // Skill cooldowns: effectiveCooldownId → expiry UTC time (mirrors Java skillCoolDowns FastMap)
    // Effective cooldown ID = template.CooldownId > 0 ? template.CooldownId : template.SkillId
    public Dictionary<int, DateTime> SkillCooldowns { get; } = new();

    public bool IsSkillOnCooldown(int effectiveCdId) =>
        SkillCooldowns.TryGetValue(effectiveCdId, out var expiry) && DateTime.UtcNow < expiry;

    public void SetSkillCooldown(int effectiveCdId, int cooldownMs) =>
        SkillCooldowns[effectiveCdId] = DateTime.UtcNow.AddMilliseconds(cooldownMs);
}
