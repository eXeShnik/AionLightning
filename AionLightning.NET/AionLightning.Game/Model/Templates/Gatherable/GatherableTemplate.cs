using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Gatherable;

[XmlRoot("gatherable_template")]
public sealed class GatherableTemplate
{
    [XmlAttribute("id")]           public int    TemplateId   { get; set; }
    [XmlAttribute("name")]         public string Name         { get; set; } = "";
    [XmlAttribute("nameId")]       public int    NameId       { get; set; }
    [XmlAttribute("harvestCount")] public int    HarvestCount { get; set; } = 1;
    [XmlAttribute("skillLevel")]   public int    SkillLevel   { get; set; }
    [XmlAttribute("harvestSkill")] public int    HarvestSkill { get; set; }
    [XmlAttribute("lvlLimit")]     public int    LevelLimit   { get; set; }

    [XmlArray("materials")]
    [XmlArrayItem("material")]
    public List<GatherableMaterial> Materials { get; set; } = new();

    public GatherableMaterial? PickMaterial()
    {
        if (Materials.Count == 0) return null;
        int roll = Random.Shared.Next(10_000_000);
        int cum  = 0;
        foreach (var m in Materials)
        {
            cum += m.Rate;
            if (roll < cum) return m;
        }
        return Materials[^1];
    }
}
