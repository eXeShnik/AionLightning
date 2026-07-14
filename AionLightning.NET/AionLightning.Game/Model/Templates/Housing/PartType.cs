namespace AionLightning.Game.Model.Templates.Housing;

/// <summary>Java model.templates.housing.PartType — the "type" attribute of a &lt;house_part&gt; element
/// in house_parts.xml, and the decoration slot a <see cref="Model.GameObjects.HouseDecoration"/> fills.
/// Java's per-type start/end packet-line-number range (used to size a flat 28-slot array covering every
/// floor) isn't needed here — this port's <see cref="Model.House.HouseRegistry"/> keys default parts by
/// (PartType, floor) directly instead of a flattened index.</summary>
public enum PartType
{
    ROOF,
    OUTWALL,
    FRAME,
    DOOR,
    GARDEN,
    FENCE,
    INWALL_ANY,
    INFLOOR_ANY,
    ADDON,
}
