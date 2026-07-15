using System.Xml.Serialization;
using AionLightning.Game.Model.Templates.Event;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

// XML binding for data/static_data/events_config/events_config.xml — mirrors Java dataholders.EventData's
// JAXB binding: <events_config><active>Name1;Name2;...</active><events><event name=".." start=".."
// end=".." theme="..">...</event>...</events></events_config>.
[XmlRoot("events_config")]
public sealed class EventsConfigFileXml
{
    [XmlElement("active")] public string Active { get; set; } = string.Empty;

    [XmlArray("events")]
    [XmlArrayItem("event")]
    public List<EventTemplate> Events { get; set; } = new();
}

/// <summary>
/// Loads data/static_data/events_config/events_config.xml (Java dataholders.EventData) into a lookup
/// keyed by event name, consumed by <see cref="AionLightning.Game.Services.EventService"/>.
/// note: matches Java's own (slightly surprising but faithful) semantics — <see cref="GetAllEvents"/>
/// returns every &lt;event&gt; template regardless of the file's &lt;active&gt; list, because Java's
/// EventService.checkEvents() itself iterates ALL events and only consults isActive()'s date window to
/// decide which to start; the &lt;active&gt; list only gates <see cref="Contains"/>, which
/// EventService uses purely to decide whether a currently-running event should stop (e.g. an operator
/// removed it from the active list without editing its date window).
/// </summary>
public sealed class EventData
{
    private static readonly XmlSerializer Serializer = new(typeof(EventsConfigFileXml));

    private readonly Dictionary<string, EventTemplate> _allEvents = new(StringComparer.Ordinal);
    private readonly HashSet<string> _activeNames = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, EventTemplate> AllEvents => _allEvents;

    public int Count => _allEvents.Count;

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "events_config", "events_config.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("EventData: events_config.xml not found at {Path}", path);
            return;
        }

        EventsConfigFileXml? data;
        try
        {
            using var fs = File.OpenRead(path);
            data = (EventsConfigFileXml?)Serializer.Deserialize(fs);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "EventData: failed to parse events_config.xml");
            return;
        }

        if (data?.Events is null)
        {
            log.LogWarning("EventData: events_config.xml produced no templates");
            return;
        }

        foreach (var name in data.Active.Split(';', StringSplitOptions.RemoveEmptyEntries))
            _activeNames.Add(name.Trim());

        foreach (var template in data.Events)
        {
            if (string.IsNullOrEmpty(template.Name)) continue;
            _allEvents[template.Name] = template;
        }

        log.LogInformation("EventData: loaded {Count} event template(s), {Active} named in the active list",
            _allEvents.Count, _activeNames.Count);
    }

    /// <summary>Java EventData.getAllEvents() — every parsed event template, regardless of active-list
    /// membership or current date window (see this class's doc comment).</summary>
    public IReadOnlyList<EventTemplate> GetAllEvents() => _allEvents.Values.ToList();

    public EventTemplate? GetEvent(string name) => _allEvents.GetValueOrDefault(name);

    /// <summary>Java EventData.Contains(name) — whether the named event is still listed in the config's
    /// &lt;active&gt; list.</summary>
    public bool Contains(string eventName) => _activeNames.Contains(eventName);
}
