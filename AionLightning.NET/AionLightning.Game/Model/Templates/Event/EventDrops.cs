using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Event;

/// <summary>Java model.templates.event.EventDrops — the &lt;event_drops&gt; wrapper around a set of
/// <see cref="EventDrop"/> rolls for one event.</summary>
[XmlRoot("event_drops")]
public sealed class EventDrops
{
    [XmlElement("event_drop")] public List<EventDrop> Drops { get; set; } = new();
}
