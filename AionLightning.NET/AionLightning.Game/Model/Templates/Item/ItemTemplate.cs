using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Item;

[XmlRoot("item_template")]
public sealed class ItemTemplate
{
    [XmlAttribute("id")]              public int    Id            { get; set; }
    [XmlAttribute("name")]            public string Name          { get; set; } = string.Empty;
    [XmlAttribute("max_stack_count")] public int    MaxStackCount { get; set; } = 1;
    [XmlAttribute("category")]        public string Category      { get; set; } = string.Empty;
    [XmlAttribute("slot")]            public int    Slot          { get; set; }
    [XmlAttribute("level")]           public int    Level         { get; set; }
    [XmlAttribute("quality")]         public string Quality       { get; set; } = string.Empty;
    [XmlAttribute("price")]           public long   Price         { get; set; }
    [XmlAttribute("equipment_type")]  public string EquipmentType { get; set; } = string.Empty;
    [XmlAttribute("weapon_type")]        public string WeaponTypeName    { get; set; } = string.Empty;
    [XmlAttribute("armor_type")]         public string ArmorTypeName     { get; set; } = string.Empty;
    [XmlAttribute("mask")]              public int    Mask             { get; set; }
    [XmlAttribute("option_slot_bonus")] public int   OptionSlotBonus  { get; set; }

    [XmlElement("actions")]           public ItemActions?      Actions     { get; set; }
    [XmlElement("weapon_stats")]      public WeaponStats?      WeaponStats { get; set; }
    [XmlElement("modifiers")]         public ItemModifiers?    Modifiers   { get; set; }
    [XmlElement("godstone")]          public GodstoneInfo?     Godstone    { get; set; }
    [XmlElement("enchant")]           public ItemEnchantInfo?  EnchantInfo { get; set; }
    [XmlElement("uselimits")]         public ItemUseLimits?    UseLimits   { get; set; }
    [XmlElement("stigma")]            public StigmaTemplate?   Stigma      { get; set; }

    public int? UseSkillId        => Actions?.SkillUse?.SkillId;
    public int? SkillLearnId      => Actions?.SkillLearn?.SkillId;
    public int? CraftLearnRecipeId => Actions?.CraftLearn?.RecipeId;
    public bool IsWeapon           => EquipmentType == "WEAPON";
    public bool IsArmor            => EquipmentType == "ARMOR";

    // mirrors Java isCanFuse — presence of <fusionaction> in <actions>
    public bool IsCanFuse => Actions?.FusionAction != null;
    // DYEABLE = 1 << 15 = 32768 (Java ItemMask)
    public bool IsItemDyePermitted => (Mask & 32768) != 0;
    // mirrors Java isCloth() — armor with a non-ARROW armor type
    public bool IsCloth => IsArmor && !string.IsNullOrEmpty(ArmorTypeName) && ArmorTypeName != "ARROW";
    // mirrors Java getMaxEnchantBonus() — from enchant sub-element's rnd_enchant attribute
    public int MaxEnchantBonus => EnchantInfo?.RndEnchant ?? 0;
    // selectable reward box — presence of <decompose select="true"/> in <actions>
    public bool IsSelectableBox => Actions?.Decompose?.IsSelect ?? false;
    // tuning scroll — presence of <tuning> in <actions> (mirrors Java TuningAction presence check)
    public bool IsTuningScroll  => Actions?.Tuning != null;
    // title item — presence of <titleadd> in <actions>
    public int? TitleAddId      => Actions?.TitleAdd?.TitleId is > 0 and int id ? id : null;

    // Two-hand weapon types: suffixed _2H or the BOW type (mirrors Java isTwoHandWeapon)
    private static readonly HashSet<string> TwoHandTypes = ["SWORD_2H", "POLEARM_2H", "STAFF_2H",
        "ORB_2H", "HARP_2H", "BOOK_2H", "BOW", "CANNON_2H", "KEYBLADE_2H"];
    public bool IsTwoHandWeapon => TwoHandTypes.Contains(WeaponTypeName);
    public int PhysicalDefense            => Modifiers?.GetAllStat("PHYSICAL_DEFENSE")         ?? 0;
    public int MagicDefense               => Modifiers?.GetAllStat("MAGICAL_DEFEND")            ?? 0;
    public int MaxHpBonus                 => Modifiers?.GetAllStat("MAXHP")                     ?? 0;
    public int MaxMpBonus                 => Modifiers?.GetAllStat("MAXMP")                     ?? 0;
    public int PhysicalAttackBonus        => Modifiers?.GetAllStat("PHYSICAL_ATTACK")           ?? 0;
    public int MagicResistBonus           => Modifiers?.GetAllStat("MAGICAL_RESIST")            ?? 0;
    public int MagicAttackBonus           => Modifiers?.GetAllStat("MAGICAL_ATTACK")            ?? 0;
    public int EvasionBonus                => Modifiers?.GetAllStat("EVASION")                   ?? 0;
    public int PhysicalAccuracyBonus       => Modifiers?.GetAllStat("PHYSICAL_ACCURACY")         ?? 0;
    public int PhysicalCriticalBonus       => Modifiers?.GetAllStat("PHYSICAL_CRITICAL")         ?? 0;
    public int PhysicalCriticalResistBonus => Modifiers?.GetAllStat("PHYSICAL_CRITICAL_RESIST")  ?? 0;
    public int MagicalAccuracyBonus        => Modifiers?.GetAllStat("MAGICAL_ACCURACY")          ?? 0;
    public int MagicalCriticalBonus        => Modifiers?.GetAllStat("MAGICAL_CRITICAL")          ?? 0;
    public int MagicalCriticalResistBonus  => Modifiers?.GetAllStat("MAGICAL_CRITICAL_RESIST")   ?? 0;
    // ATTACK_SPEED and BOOST_CASTING_TIME use <rate> elements in item XML (not <add>).
    public int AttackSpeedBonusPct         => Modifiers?.GetRateStat("ATTACK_SPEED")            ?? 0;
    public int CastTimeBonusPct            => Modifiers?.GetRateStat("BOOST_CASTING_TIME")      ?? 0;
    // SPEED uses <rate> elements; values are per-1000 (22 = +2.2% movement speed).
    public int SpeedBonusPct               => Modifiers?.GetRateStat("SPEED")                   ?? 0;
    public int ParryBonus                  => Modifiers?.GetAllStat("PARRY")                      ?? 0;
    public int BlockBonus                  => Modifiers?.GetAllStat("BLOCK")                      ?? 0;
    // Strike/spell fortitude — reduce crit multiplier; Java formula: coeff -= Math.round(fortitude/1000f)
    public int StrikeFortitudeBonus        => Modifiers?.GetBonusStat("PHYSICAL_CRITICAL_DAMAGE_REDUCE") ?? 0;
    public int SpellFortitudeBonus         => Modifiers?.GetBonusStat("MAGICAL_CRITICAL_DAMAGE_REDUCE")  ?? 0;
    public int ConcentrationBonus          => Modifiers?.GetAllStat("CONCENTRATION")               ?? 0;
    public int MagicBoostBonus             => Modifiers?.GetAllStat("BOOST_MAGICAL_SKILL")         ?? 0;
    public int MagicSuppressionBonus       => Modifiers?.GetAllStat("MAGIC_SKILL_BOOST_RESIST")    ?? 0;
    public int HealBoostBonus              => Modifiers?.GetAllStat("HEAL_BOOST")                  ?? 0;
    public int FlySpeedBonusPct            => Modifiers?.GetRateStat("FLY_SPEED")                  ?? 0;

    // CAN_PROC_ENCHANT = 1 << 10 = 1024 (Java ItemMask)
    public bool CanSocketGodstone  => (Mask & 1024) != 0;
    public bool IsStigmaItem       => Stigma != null;

    // Magical weapon types: their min/max damage is magical attack, not physical
    private static readonly HashSet<string> MagicalWeaponTypes =
        ["MACE_1H", "STAFF_2H", "BOOK_2H", "ORB_2H", "HARP_2H", "GUN_1H", "CANNON_2H", "KEYBLADE_2H"];
    public bool IsMagicalWeapon => MagicalWeaponTypes.Contains(WeaponTypeName);
}

public sealed class ItemModifiers
{
    [XmlElement("add")]  public List<ItemModifier> Add  { get; set; } = new();
    [XmlElement("rate")] public List<ItemModifier> Rate { get; set; } = new();

    // Sum ALL <add> entries for a stat regardless of bonus flag.
    // Java sums all AddModifiers for a stat — bonus="true" in item XML marks equipment bonuses,
    // both flat and bonus variants contribute to the same stat pool.
    public int GetAllStat(string name) =>
        Add.Where(m => m.Name == name).Sum(m => m.Value);

    // Sum of flat <add> values only (bonus=false). Use when the flat and bonus variants must be
    // distinguished (e.g. enchant calculations that reference only base armor values).
    public int GetStat(string name) =>
        Add.Where(m => m.Name == name && !m.Bonus).Sum(m => m.Value);

    // Sum of bonus <add> values only (bonus=true). Kept for stats known to only ever have bonus entries.
    public int GetBonusStat(string name) =>
        Add.Where(m => m.Name == name && m.Bonus).Sum(m => m.Value);

    // Sum of <rate> values for a stat — used for percentage-type stats (SPEED, ATTACK_SPEED, BOOST_CASTING_TIME).
    public int GetRateStat(string name) =>
        Rate.Where(m => m.Name == name).Sum(m => m.Value);
}

public sealed class ItemModifier
{
    [XmlAttribute("name")]  public string Name  { get; set; } = string.Empty;
    [XmlAttribute("value")] public int    Value { get; set; }
    [XmlAttribute("bonus")] public bool   Bonus { get; set; }
}

public sealed class WeaponStats
{
    [XmlAttribute("min_damage")]   public int MinDamage   { get; set; }
    [XmlAttribute("max_damage")]   public int MaxDamage   { get; set; }
    [XmlAttribute("attack_speed")] public int AttackSpeed { get; set; }
    [XmlAttribute("hit_count")]    public int HitCount    { get; set; } = 1;
}

public sealed class ItemActions
{
    [XmlElement("skilluse")]    public SkillUseAction?    SkillUse     { get; set; }
    [XmlElement("skilllearn")]  public SkillLearnAction?  SkillLearn   { get; set; }
    [XmlElement("craftlearn")]  public CraftLearnAction?  CraftLearn   { get; set; }
    [XmlElement("fusionaction")] public FusionAction?     FusionAction { get; set; }
    [XmlElement("dye")]          public DyeAction?        Dye          { get; set; }
    [XmlElement("decompose")]    public DecomposeAction?  Decompose    { get; set; }
    [XmlElement("tuning")]       public TuningAction?     Tuning       { get; set; }
    [XmlElement("titleadd")]    public TitleAddAction?   TitleAdd     { get; set; }
    [XmlElement("houseobject")] public HouseObjectAction? HouseObject  { get; set; }
    [XmlElement("housedeco")]   public HouseDecoAction?   HouseDeco    { get; set; }
}

public sealed class DyeAction
{
    [XmlAttribute("color")]   public string Color   { get; set; } = string.Empty; // hex RGB or "no"
    [XmlAttribute("minutes")] public int    Minutes { get; set; }                 // 0 = permanent
}

public sealed class FusionAction { } // presence signals weapon can be fused (Java: isCanFuse)

public sealed class SkillUseAction
{
    [XmlAttribute("skillid")] public int SkillId { get; set; }
    [XmlAttribute("level")]   public int Level   { get; set; }
}

public sealed class SkillLearnAction
{
    [XmlAttribute("skillid")] public int    SkillId           { get; set; }
    [XmlAttribute("class")]   public string ClassRestriction  { get; set; } = "ALL";
    [XmlAttribute("level")]   public int    RequiredLevel     { get; set; }
}

public sealed class CraftLearnAction
{
    [XmlAttribute("recipeid")] public int RecipeId { get; set; }
}

public sealed class GodstoneInfo
{
    [XmlAttribute("skillid")]          public int SkillId          { get; set; }
    [XmlAttribute("skilllvl")]         public int SkillLvl         { get; set; }
    [XmlAttribute("probability")]      public int Probability      { get; set; }
    [XmlAttribute("probabilityleft")]  public int ProbabilityLeft  { get; set; }
}

public sealed class ItemEnchantInfo
{
    [XmlAttribute("rnd_enchant")] public int RndEnchant { get; set; }
    [XmlAttribute("max_enchant")] public int MaxEnchant { get; set; }
    [XmlAttribute("wake_level")]  public int WakeLevel  { get; set; }
    [XmlAttribute("waken_id")]    public int WakenId    { get; set; }
    [XmlAttribute("m_slots")]     public int MSlots     { get; set; }
    [XmlAttribute("s_slots")]     public int SSlots     { get; set; }
    [XmlAttribute("rnd_slots")]   public int RndSlots   { get; set; }
}

public sealed class DecomposeAction
{
    [XmlAttribute("select")] public bool IsSelect { get; set; }
}

public sealed class TuningAction
{
    // target="WEAPON" | "ARMOR" | "EQUIPMENT" — which item types this scroll can re-tune
    [XmlAttribute("target")] public string Target { get; set; } = string.Empty;
}

public sealed class TitleAddAction
{
    [XmlAttribute("titleid")] public int TitleId { get; set; }
    // minutes=0 (absent) means permanent; >0 means timed (timed expiry deferred — stored as permanent)
    [XmlAttribute("minutes")] public int Minutes { get; set; }
}

/// <summary>Maps to &lt;houseobject id="N"/&gt; (Java SummonHouseObjectAction) — turning this item into a
/// placed <see cref="AionLightning.Game.Model.GameObjects.HouseObject"/> whose template id is <see cref="TemplateId"/>
/// (housing_objects.xml). See Services.Item.HouseObjectFactory.CreateFromItem.</summary>
public sealed class HouseObjectAction
{
    [XmlAttribute("id")] public int TemplateId { get; set; }
}

/// <summary>Maps to &lt;housedeco id="N"/&gt; (Java DecorateAction) — turning this item into a custom
/// building-part <see cref="AionLightning.Game.Model.GameObjects.HouseDecoration"/> whose part id is
/// <see cref="TemplateId"/> (house_parts.xml).</summary>
public sealed class HouseDecoAction
{
    [XmlAttribute("id")] public int TemplateId { get; set; }
}

/// <summary>Maps to <uselimits usedelay="..." usedelayid="..."/> — mirrors Java ItemUseLimits.</summary>
public sealed class ItemUseLimits
{
    /// <summary>Cooldown duration in milliseconds (0 = no cooldown).</summary>
    [XmlAttribute("usedelay")]   public int DelayMs  { get; set; }
    /// <summary>Cooldown group ID — items with the same ID share a single cooldown timer.</summary>
    [XmlAttribute("usedelayid")] public int DelayId  { get; set; }
}

/// <summary>
/// Maps to &lt;stigma shard="N" skill="level:skillId [level:skillId ...]"/&gt;.
/// Mirrors Java Stigma.java — describes the skills granted by a stigma stone and the
/// number of stigma shards required to socket it.
/// </summary>
public sealed class StigmaTemplate
{
    /// <summary>Number of stigma shards (item 141000001) consumed on equip.</summary>
    [XmlAttribute("shard")] public int    Shard     { get; set; }
    /// <summary>Space-separated "skillLevel:skillId" pairs, e.g. "9:11504" or "1:19 2:20".</summary>
    [XmlAttribute("skill")] public string SkillData { get; set; } = string.Empty;

    public IReadOnlyList<(int SkillLevel, int SkillId)> GetSkills()
    {
        if (string.IsNullOrEmpty(SkillData)) return [];
        var list = new List<(int, int)>();
        foreach (var part in SkillData.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var colon = part.IndexOf(':');
            if (colon > 0
                && int.TryParse(part.AsSpan(0, colon),  out int lvl)
                && int.TryParse(part.AsSpan(colon + 1), out int id))
                list.Add((lvl, id));
        }
        return list;
    }
}
