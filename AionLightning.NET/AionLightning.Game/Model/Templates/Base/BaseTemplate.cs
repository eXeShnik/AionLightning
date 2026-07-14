using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Base;

/// <summary>
/// Java model.templates.base.BaseTemplate, XML-bound to the &lt;base_location&gt; element in
/// data/static_data/base/base_locations.xml.
/// </summary>
public sealed class BaseTemplate
{
    [XmlAttribute("id")] public int Id { get; set; }
    [XmlAttribute("world")] public int World { get; set; }
    [XmlAttribute("name_id")] public int NameId { get; set; }
    [XmlAttribute("name")] public string Name { get; set; } = string.Empty;
}
