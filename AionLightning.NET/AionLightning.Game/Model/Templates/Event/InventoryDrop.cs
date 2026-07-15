using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Event;

/// <summary>
/// Java model.templates.event.InventoryDrop — a periodic "everyone above this level gets a free item"
/// grant (Java: <c>ThreadPoolManager</c>-scheduled, visiting every online player every <see cref="Interval"/>
/// minutes via <c>ItemService.dropItemToInventory</c>).
/// note: no <c>ItemService.dropItemToInventory</c> equivalent exists in this port, so this class loads
/// the data faithfully (including ignoring the XML's extra endlevel/maxCountOfDay/cleanTime attributes,
/// exactly as Java's own 3-field JAXB binding does) but <see cref="AionLightning.Game.Services.EventService"/>
/// does not schedule a runtime grant against it — see that class's doc comment.
/// </summary>
[XmlRoot("inventory_drop")]
public sealed class InventoryDrop
{
    [XmlAttribute("startlevel")] public int StartLevel { get; set; }
    [XmlAttribute("interval")]   public int Interval    { get; set; }
    [XmlText]                    public int DropItem    { get; set; }
}
