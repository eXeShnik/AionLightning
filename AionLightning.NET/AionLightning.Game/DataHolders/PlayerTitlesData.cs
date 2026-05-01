using System.Xml.Serialization;
using AionLightning.Game.Model.Templates.Player;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class PlayerTitlesData
{
    private readonly Dictionary<int, PlayerTitleTemplate> _data = new();

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "player_titles.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("PlayerTitlesData: file not found: {Path}", path);
            return;
        }

        var serializer = new XmlSerializer(typeof(PlayerTitleListXml));
        using var fs   = File.OpenRead(path);
        var root       = (PlayerTitleListXml?)serializer.Deserialize(fs);
        if (root is null) return;

        foreach (var t in root.Titles)
            _data[t.Id] = t;

        log.LogInformation("PlayerTitlesData: loaded {Count} title templates", _data.Count);
    }

    public PlayerTitleTemplate? GetTemplate(int id) => _data.GetValueOrDefault(id);

    public int Count => _data.Count;

    [XmlRoot("player_titles")]
    private sealed class PlayerTitleListXml
    {
        [XmlElement("title")] public List<PlayerTitleTemplate> Titles { get; set; } = new();
    }
}
