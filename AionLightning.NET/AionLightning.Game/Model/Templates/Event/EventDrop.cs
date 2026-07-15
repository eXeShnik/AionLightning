using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Event;

/// <summary>
/// Java model.templates.event.EventDrop — a single event-themed loot roll (item id/count/chance, gated
/// to an optional level-difficulty band via minDiff/maxDiff).
/// note: no <c>ItemService.dropItemToInventory</c>/loot-roll equivalent for this specific event-drop
/// table exists in this port (the drop pipeline only knows about DropData/GlobalDropData); this class
/// loads the data faithfully but nothing currently rolls against it — see <see cref="AionLightning.Game.Services.EventService"/>'s
/// doc comment.
/// </summary>
[XmlRoot("event_drop")]
public sealed class EventDrop
{
    [XmlAttribute("item_id")] public int   ItemId  { get; set; }
    [XmlAttribute("count")]   public long  Count   { get; set; }
    [XmlAttribute("chance")]  public float Chance  { get; set; }
    [XmlAttribute("minDiff")] public int   MinDiff { get; set; }
    [XmlAttribute("maxDiff")] public int   MaxDiff { get; set; }
}
