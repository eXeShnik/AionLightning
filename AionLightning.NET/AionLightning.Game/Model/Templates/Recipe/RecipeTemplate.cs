using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Recipe;

[XmlRoot("recipe_template")]
public sealed class RecipeTemplate
{
    [XmlAttribute("id")]          public int    Id         { get; set; }
    [XmlAttribute("nameid")]      public int    NameId     { get; set; }
    [XmlAttribute("skillid")]     public int    SkillId    { get; set; }
    [XmlAttribute("skillpoint")]  public int    SkillPoint { get; set; }
    [XmlAttribute("productid")]   public int    ProductId  { get; set; }
    [XmlAttribute("quantity")]    public int    Quantity   { get; set; } = 1;
    [XmlAttribute("autolearn")]   public int    AutoLearn  { get; set; }
    [XmlAttribute("race")]        public string Race       { get; set; } = "ELYOS";

    [XmlElement("component")]
    public List<RecipeComponent> Components { get; set; } = new();

    [XmlElement("comboproduct")]
    public List<RecipeComboProduct> ComboProducts { get; set; } = new();

    // First combo product ID (higher-quality item given on critical craft success), or 0 if none.
    public int ComboProductId => ComboProducts.Count > 0 ? ComboProducts[0].ItemId : 0;
}

public sealed class RecipeComponent
{
    [XmlAttribute("itemid")]   public int ItemId   { get; set; }
    [XmlAttribute("quantity")] public int Quantity { get; set; } = 1;
}

public sealed class RecipeComboProduct
{
    [XmlAttribute("itemid")] public int ItemId { get; set; }
}
