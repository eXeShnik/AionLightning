using System.Globalization;
using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Event;

// XML binding for the <spawns> element embedded directly inside an <event> (as opposed to a standalone
// spawns file) — the same spawn_map/spawn/spot shape as data/static_data/spawns/Npcs/*.xml (see
// DataHolders.SpawnsData), duplicated locally rather than reused across the DataHolders -> Model
// dependency direction (Model must not depend on DataHolders).

[XmlRoot("spot")]
public sealed class EventSpawnSpotXml
{
    [XmlAttribute("x")] public float X { get; set; }
    [XmlAttribute("y")] public float Y { get; set; }
    [XmlAttribute("z")] public float Z { get; set; }
    [XmlAttribute("h")] public byte  Heading { get; set; }
}

[XmlRoot("spawn")]
public sealed class EventSpawnEntryXml
{
    [XmlAttribute("npc_id")]       public int  NpcId       { get; set; }
    [XmlAttribute("respawn_time")] public int  RespawnTime { get; set; }
    [XmlElement("spot")]           public List<EventSpawnSpotXml> Spots { get; set; } = new();
}

[XmlRoot("spawn_map")]
public sealed class EventSpawnMapXml
{
    [XmlAttribute("map_id")] public int MapId { get; set; }
    [XmlElement("spawn")]    public List<EventSpawnEntryXml> Spawns { get; set; } = new();
}

[XmlRoot("spawns")]
public sealed class EventSpawnsXml
{
    [XmlElement("spawn_map")] public List<EventSpawnMapXml> Maps { get; set; } = new();
}

/// <summary>A single flattened event NPC spawn spot — <see cref="EventTemplate.GetSpawnPoints"/>'s
/// output, consumed directly by <see cref="AionLightning.Game.Services.SpawnService.SpawnEvent"/>.</summary>
public readonly record struct EventSpawnPoint(int MapId, int NpcId, int RespawnTime, float X, float Y, float Z, byte Heading);

/// <summary>
/// Java model.templates.event.EventTemplate — one seasonal event's date window, quest list, drop table
/// and NPC spawn set, bound to a single &lt;event&gt; element in
/// data/static_data/events_config/events_config.xml. Unlike Java (which folded live runtime state —
/// isStarted/spawnedObjects — directly into this class), this port keeps the template a pure XML DTO;
/// runtime spawn/despawn bookkeeping lives in <see cref="AionLightning.Game.Services.SpawnService"/>'s
/// event-name-tagged registry instead (mirrors the BaseTemplate/BaseLocation split already used for the
/// base subsystem).
/// note: Java's EventTemplate also carried a &lt;surveys&gt; list that toggled GuideTemplate/HTML-survey
/// activation — no such guide/HTML-survey subsystem was ported, so that element is intentionally not
/// modeled here.
/// </summary>
[XmlRoot("event")]
public sealed class EventTemplate
{
    [XmlAttribute("name")]  public string  Name  { get; set; } = string.Empty;
    [XmlAttribute("start")] public string  Start { get; set; } = string.Empty;
    [XmlAttribute("end")]   public string  End   { get; set; } = string.Empty;
    [XmlAttribute("theme")] public string? Theme { get; set; }

    [XmlElement("quests")]         public EventQuestList?  Quests         { get; set; }
    [XmlElement("event_drops")]    public EventDrops?      EventDrops     { get; set; }
    [XmlElement("spawns")]         public EventSpawnsXml?  Spawns         { get; set; }
    [XmlElement("inventory_drop")] public InventoryDrop?   InventoryDrop  { get; set; }

    [XmlIgnore] public List<int> StartableQuests    => Quests?.StartableQuests ?? new List<int>();
    [XmlIgnore] public List<int> MaintainableQuests  => Quests?.MaintainQuests  ?? new List<int>();

    /// <summary>Java EventTemplate.getTheme() — lower-cased for case-insensitive EventType lookup.</summary>
    [XmlIgnore] public string? ThemeLower => Theme?.ToLowerInvariant();

    /// <summary>Java EventTemplate.isActive() — getStartDate().isBeforeNow() &amp;&amp; getEndDate().isAfterNow().
    /// Returns false (rather than throwing) if either date attribute fails to parse.</summary>
    public bool IsActive()
    {
        if (!TryParseDate(Start, out var start) || !TryParseDate(End, out var end))
            return false;

        var now = DateTimeOffset.UtcNow;
        return now >= start && now < end;
    }

    public bool IsExpired() => !IsActive();

    /// <summary>Java EventTemplate.Start()'s spawn-side flattening — every spawn spot across every
    /// spawn_map this event defines.</summary>
    public IEnumerable<EventSpawnPoint> GetSpawnPoints()
    {
        if (Spawns is null) yield break;

        foreach (var map in Spawns.Maps)
        foreach (var spawn in map.Spawns)
        foreach (var spot in spawn.Spots)
            yield return new EventSpawnPoint(map.MapId, spawn.NpcId, spawn.RespawnTime, spot.X, spot.Y, spot.Z, spot.Heading);
    }

    private static bool TryParseDate(string value, out DateTimeOffset result) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result);
}
