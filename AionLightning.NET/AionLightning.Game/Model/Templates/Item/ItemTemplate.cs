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
    [XmlAttribute("mask")]            public int    Mask          { get; set; }

    [XmlElement("actions")]           public ItemActions?   Actions     { get; set; }
    [XmlElement("weapon_stats")]      public WeaponStats?   WeaponStats { get; set; }
    [XmlElement("modifiers")]         public ItemModifiers? Modifiers   { get; set; }
    [XmlElement("godstone")]          public GodstoneInfo?  Godstone    { get; set; }

    public int? UseSkillId        => Actions?.SkillUse?.SkillId;
    public int? SkillLearnId      => Actions?.SkillLearn?.SkillId;
    public int? CraftLearnRecipeId => Actions?.CraftLearn?.RecipeId;
    public bool IsWeapon           => EquipmentType == "WEAPON";
    public bool IsArmor            => EquipmentType == "ARMOR";
    public int PhysicalDefense     => Modifiers?.GetStat("PHYSICAL_DEFENSE") ?? 0;
    public int MagicDefense        => Modifiers?.GetStat("MAGICAL_DEFEND")   ?? 0;
    public int MaxHpBonus          => Modifiers?.GetStat("MAXHP")            ?? 0;
    public int MaxMpBonus          => Modifiers?.GetStat("MAXMP")            ?? 0;

    // CAN_PROC_ENCHANT = 1 << 10 = 1024 (Java ItemMask)
    public bool CanSocketGodstone  => (Mask & 1024) != 0;
}

public sealed class ItemModifiers
{
    [XmlElement("add")] public List<ItemModifier> Add { get; set; } = new();

    // Returns sum of all non-percentage (flat) values for the given stat name.
    public int GetStat(string name) =>
        Add.Where(m => m.Name == name && !m.Bonus).Sum(m => m.Value);
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
}

public sealed class ItemActions
{
    [XmlElement("skilluse")]    public SkillUseAction?    SkillUse    { get; set; }
    [XmlElement("skilllearn")]  public SkillLearnAction?  SkillLearn  { get; set; }
    [XmlElement("craftlearn")]  public CraftLearnAction?  CraftLearn  { get; set; }
}

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
