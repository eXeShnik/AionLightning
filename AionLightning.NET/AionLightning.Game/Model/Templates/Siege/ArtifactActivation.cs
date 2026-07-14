using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Siege;

/// <summary>Java model.templates.siegelocation.ArtifactActivation, XML-bound to the
/// &lt;artifact_activation&gt; child of a &lt;siege_location&gt; element. Java's hand-written class
/// has no field initializers (unlike the XSD-documented defaults), so none are applied here either —
/// an absent attribute means 0, exactly like the Java field default.</summary>
public sealed class ArtifactActivation
{
    [XmlAttribute("itemid")] public int ItemId { get; set; }
    [XmlAttribute("count")] public int Count { get; set; }
    [XmlAttribute("skill")] public int SkillId { get; set; }
    [XmlAttribute("cd")] public int Cd { get; set; }

    /// <summary>Java getCd() — cooldown in milliseconds.</summary>
    [XmlIgnore]
    public long CdMillis => (long)Cd * 1000;
}
