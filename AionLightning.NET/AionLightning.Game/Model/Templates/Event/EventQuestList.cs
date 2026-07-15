using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Event;

/// <summary>
/// Java model.templates.event.EventQuestList — the &lt;quests&gt; element listing which quest ids an
/// event auto-starts on player login (<see cref="StartableQuests"/>) versus which it merely keeps
/// re-registered for players who already hold them (<see cref="MaintainQuests"/>), each a
/// semicolon-separated id list in the XML.
/// </summary>
[XmlRoot("quests")]
public sealed class EventQuestList
{
    [XmlElement("startable")]    public string? Startable    { get; set; }
    [XmlElement("maintainable")] public string? Maintainable { get; set; }

    [XmlIgnore] public List<int> StartableQuests => ParseIds(Startable);
    [XmlIgnore] public List<int> MaintainQuests  => ParseIds(Maintainable);

    private static List<int> ParseIds(string? data)
    {
        if (string.IsNullOrWhiteSpace(data)) return new List<int>();

        var result = new List<int>();
        foreach (var token in data.Split(';', StringSplitOptions.RemoveEmptyEntries))
            if (int.TryParse(token, out var id) && !result.Contains(id))
                result.Add(id);

        return result;
    }
}
