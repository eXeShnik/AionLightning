using System.Xml;
using AionLightning.Game.Model.Templates.World;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class WorldMapData
{
    private readonly Dictionary<int, WorldMapTemplate> _templates = new();

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "world_maps.xml");
        if (!File.Exists(path)) { log.LogWarning("WorldMapData: world_maps.xml not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "map") continue;
            if (!int.TryParse(reader.GetAttribute("id"), out int mapId)) continue;

            var template = new WorldMapTemplate(
                WorldId: mapId,
                Name: reader.GetAttribute("name") ?? string.Empty,
                IsInstance: string.Equals(reader.GetAttribute("instance"), "true", StringComparison.OrdinalIgnoreCase),
                WorldType: reader.GetAttribute("world_type") ?? string.Empty,
                WorldSize: int.TryParse(reader.GetAttribute("world_size"), out int size) ? size : 0,
                Flags: reader.GetAttribute("flags") ?? string.Empty,
                DropType: reader.GetAttribute("drop_type") ?? string.Empty,
                DeathLevel: int.TryParse(reader.GetAttribute("death_level"), out int death) ? death : 0,
                WaterLevel: int.TryParse(reader.GetAttribute("water_level"), out int water) ? water : 0);

            _templates[mapId] = template;
        }

        int instanceCount = _templates.Values.Count(t => t.IsInstance);
        log.LogInformation("WorldMapData: loaded {Count} world maps ({Instances} instanced)", _templates.Count, instanceCount);
    }

    public WorldMapTemplate? GetTemplate(int worldId) => _templates.GetValueOrDefault(worldId);

    public bool IsInstance(int worldId) => _templates.TryGetValue(worldId, out var t) && t.IsInstance;

    public string GetDropType(int worldId) => _templates.TryGetValue(worldId, out var t) ? t.DropType : string.Empty;
}
