namespace AionLightning.Game.Model.Templates.Housing;

/// <summary>Java model.templates.housing.PartType — the "type" attribute of a &lt;house_part&gt; element
/// in house_parts.xml, and the decoration slot a <see cref="Model.GameObjects.HouseDecoration"/> fills.
/// Java's per-type start/end packet-line-number range (used to size a flat 28-slot array covering every
/// floor) isn't needed for P2's default-part rendering — this port's <see cref="Model.House.HouseRegistry"/>
/// keys default parts by (PartType, floor) directly instead of a flattened index. P3 (furniture placement)
/// does need the line-number range after all, to decode CM_HOUSE_DECORATE's client-sent line number back
/// into a (PartType, floor) pair — see <see cref="PartTypeLineNumbers.GetForLineNr"/>.</summary>
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

/// <summary>Java PartType's packet-line-number range fields/getForLineNr, factored out as a companion
/// static class since C# enums can't carry per-value data directly.</summary>
public static class PartTypeLineNumbers
{
    private static readonly (PartType Type, int Start, int End)[] Ranges =
    [
        (PartType.ROOF, 1, 1),
        (PartType.OUTWALL, 2, 2),
        (PartType.FRAME, 3, 3),
        (PartType.DOOR, 4, 4),
        (PartType.GARDEN, 5, 5),
        (PartType.FENCE, 6, 6),
        (PartType.INWALL_ANY, 8, 13),
        (PartType.INFLOOR_ANY, 14, 19),
        (PartType.ADDON, 27, 27),
    ];

    public static int GetStartLineNr(this PartType type) => Ranges.First(r => r.Type == type).Start;

    /// <summary>Java PartType.getForLineNr(int) — returns null when the line number falls in a gap
    /// (e.g. 7, 20-26) not covered by any slot.</summary>
    public static PartType? GetForLineNr(int lineNr)
    {
        foreach (var (type, start, end) in Ranges)
            if (lineNr >= start && lineNr <= end)
                return type;
        return null;
    }
}
