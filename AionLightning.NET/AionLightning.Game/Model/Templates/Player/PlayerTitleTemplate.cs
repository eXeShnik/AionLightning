using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Player;

[XmlRoot("title")]
public sealed class PlayerTitleTemplate
{
    [XmlAttribute("id")]     public int    Id          { get; set; }
    [XmlAttribute("nameId")] public int    NameId      { get; set; }
    [XmlAttribute("desc")]   public string Description { get; set; } = string.Empty;
    [XmlAttribute("race")]   public string Race        { get; set; } = string.Empty;

    [XmlElement("modifiers")] public TitleModifiers? Modifiers { get; set; }

    public int GetAddStat(string name)
        => Modifiers?.Add.Where(m => m.Name == name).Sum(m => m.Value) ?? 0;

    public int GetRateStat(string name)
        => Modifiers?.Rate.Where(m => m.Name == name).Sum(m => m.Value) ?? 0;
}

public sealed class TitleModifiers
{
    [XmlElement("add")]  public List<TitleModifier> Add  { get; set; } = new();
    [XmlElement("rate")] public List<TitleModifier> Rate { get; set; } = new();
}

public sealed class TitleModifier
{
    [XmlAttribute("name")]  public string Name  { get; set; } = string.Empty;
    [XmlAttribute("value")] public int    Value { get; set; }
    [XmlAttribute("bonus")] public bool   Bonus { get; set; }
}
