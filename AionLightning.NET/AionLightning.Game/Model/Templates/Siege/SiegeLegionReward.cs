using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Siege;

/// <summary>Java model.templates.siegelocation.SiegeLegionReward, XML-bound to &lt;legion_reward&gt;.</summary>
public sealed class SiegeLegionReward
{
    [XmlAttribute("itemid")] public int ItemId { get; set; }
    [XmlAttribute("m_count")] public int Count { get; set; }
}
