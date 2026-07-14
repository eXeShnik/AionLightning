using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Siege;

/// <summary>Java model.templates.siegelocation.SiegeReward, XML-bound to &lt;siege_reward&gt;.</summary>
public sealed class SiegeReward
{
    [XmlAttribute("top")] public int Top { get; set; }
    [XmlAttribute("itemid")] public int ItemId { get; set; }
    [XmlAttribute("m_count")] public int Count { get; set; }
    [XmlAttribute("gp_count")] public int GpCount { get; set; }
}
