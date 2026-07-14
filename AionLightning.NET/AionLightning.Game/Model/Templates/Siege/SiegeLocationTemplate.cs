using System.Xml.Serialization;
using AionLightning.Game.Model.Siege;

namespace AionLightning.Game.Model.Templates.Siege;

/// <summary>Java model.templates.siegelocation.SiegeLocationTemplate, XML-bound to the
/// &lt;siege_location&gt; element in siege_locations.xml.</summary>
public sealed class SiegeLocationTemplate
{
    [XmlAttribute("id")] public int Id { get; set; }
    [XmlAttribute("type")] public SiegeType Type { get; set; }
    [XmlAttribute("world")] public int World { get; set; }
    [XmlAttribute("name_id")] public int NameId { get; set; }
    [XmlAttribute("repeat_count")] public int RepeatCount { get; set; } = 1;
    [XmlAttribute("repeat_interval")] public int RepeatInterval { get; set; } = 1;
    [XmlAttribute("siege_duration")] public int SiegeDuration { get; set; }
    [XmlAttribute("influence")] public int InfluenceValue { get; set; }

    /// <summary>Java: @XmlList @XmlAttribute(name="fortress_dependency") — a space-separated list of
    /// fortress ids packed into a single XML attribute value.</summary>
    [XmlAttribute("fortress_dependency")] public string? FortressDependencyRaw { get; set; }

    [XmlIgnore]
    public List<int> FortressDependency =>
        string.IsNullOrWhiteSpace(FortressDependencyRaw)
            ? []
            : FortressDependencyRaw.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse).ToList();

    [XmlElement("artifact_activation")] public ArtifactActivation? Activation { get; set; }
    [XmlElement("siege_reward")] public List<SiegeReward> SiegeRewards { get; set; } = [];
    [XmlElement("legion_reward")] public List<SiegeLegionReward> SiegeLegionRewards { get; set; } = [];
}
