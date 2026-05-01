using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Recipe;

[XmlRoot("recipe_template")]
public sealed class RecipeTemplate
{
    [XmlAttribute("id")]          public int    Id         { get; set; }
    [XmlAttribute("skillid")]     public int    SkillId    { get; set; }
    [XmlAttribute("skillpoint")]  public int    SkillPoint { get; set; }
    [XmlAttribute("productid")]   public int    ProductId  { get; set; }
    [XmlAttribute("quantity")]    public int    Quantity   { get; set; } = 1;
    [XmlAttribute("autolearn")]   public int    AutoLearn  { get; set; }
    [XmlAttribute("race")]        public string Race       { get; set; } = "ELYOS";

    [XmlElement("component")]
    public List<RecipeComponent> Components { get; set; } = new();
}

public sealed class RecipeComponent
{
    [XmlAttribute("itemid")]   public int ItemId   { get; set; }
    [XmlAttribute("quantity")] public int Quantity { get; set; } = 1;
}
