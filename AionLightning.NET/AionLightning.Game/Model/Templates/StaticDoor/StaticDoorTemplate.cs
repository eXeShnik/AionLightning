using System.Globalization;
using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.StaticDoor;

/// <summary>Java model.templates.staticdoor.DoorType.</summary>
public enum DoorType
{
    DOOR,
    ABYSS,
    HOUSE,
}

/// <summary>
/// Java model.templates.staticdoor.StaticDoorState EnumSet flags. Packed into the low nibble of the
/// door's XML "state" attribute and of the CM_OPEN_STATICDOOR/SM_EMOTION state field.
/// </summary>
[Flags]
public enum StaticDoorState
{
    None      = 0,
    Opened    = 1 << 0,
    Clickable = 1 << 1,
    Closeable = 1 << 2,
    OneWay    = 1 << 3,
}

/// <summary>Java model.templates.staticdoor.StaticDoorBounds. Click/collision bounds for the door mesh —
/// unused until a geodata engine exists to test against them (see migration_plan.md geodata gap); kept so
/// the XML round-trips losslessly and is ready once that engine lands.</summary>
public sealed class StaticDoorBounds
{
    [XmlAttribute("x1")] public float X1 { get; set; }
    [XmlAttribute("y1")] public float Y1 { get; set; }
    [XmlAttribute("z1")] public float Z1 { get; set; }
    [XmlAttribute("x2")] public float X2 { get; set; }
    [XmlAttribute("y2")] public float Y2 { get; set; }
    [XmlAttribute("z2")] public float Z2 { get; set; }
}

/// <summary>Java model.templates.staticdoor.StaticDoorTemplate — one door definition from
/// static_doors/staticdoor_templates.xml (per-world "staticdoor" element).</summary>
[XmlRoot("staticdoor")]
public sealed class StaticDoorTemplate
{
    [XmlAttribute("type")]   public DoorType Type    { get; set; } = DoorType.DOOR;
    [XmlAttribute("x")]      public float X           { get; set; }
    [XmlAttribute("y")]      public float Y           { get; set; }
    [XmlAttribute("z")]      public float Z           { get; set; }
    [XmlAttribute("doorid")] public int DoorId        { get; set; }
    [XmlAttribute("keyid")]  public int KeyId         { get; set; }
    [XmlAttribute("mesh")]   public string? MeshFile  { get; set; }
    [XmlAttribute("state")]  public string? StatesHex { get; set; }
    [XmlElement("box")]      public StaticDoorBounds? Box { get; set; }

    private StaticDoorState? _initialStates;

    /// <summary>Java StaticDoorTemplate.getInitialStates() — lazily parsed once from
    /// <see cref="StatesHex"/> ("0x2" hex, or a plain decimal number when there is no "0x" prefix).</summary>
    public StaticDoorState InitialStates => _initialStates ??= ParseStates(StatesHex);

    private static StaticDoorState ParseStates(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return StaticDoorState.None;

        bool isHex = raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        string digits = isHex ? raw[2..] : raw;
        var style = isHex ? NumberStyles.HexNumber : NumberStyles.Integer;
        if (!int.TryParse(digits, style, CultureInfo.InvariantCulture, out int flags))
            return StaticDoorState.None;

        return (StaticDoorState)(flags & 0xF);
    }
}
