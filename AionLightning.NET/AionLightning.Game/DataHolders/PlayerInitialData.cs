using System.Xml.Serialization;
using AionLightning.Game.Model;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class PlayerInitialData
{
    public SpawnLocation ElyosSpawn    { get; private set; } = new(210010000, 1212.94f,  1044.85f, 140.76f,  32);
    public SpawnLocation AsmodianSpawn { get; private set; } = new(220010000,  571.04f, 2787.34f, 299.88f,  32);

    public void Load(string dataRoot, ILogger log)
    {
        var file = Path.Combine(dataRoot, "player_initial_data.xml");
        if (!File.Exists(file))
        {
            log.LogWarning("PlayerInitialData: file not found at {File}, using defaults", file);
            return;
        }

        var serializer = new XmlSerializer(typeof(PlayerInitialDataXml));
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
        var root = (PlayerInitialDataXml?)serializer.Deserialize(stream);
        if (root is null) return;

        if (root.ElyosSpawn is { } e)
            ElyosSpawn = new SpawnLocation(e.MapId, e.X, e.Y, e.Z, e.Heading);
        if (root.AsmodianSpawn is { } a)
            AsmodianSpawn = new SpawnLocation(a.MapId, a.X, a.Y, a.Z, a.Heading);

        log.LogInformation("PlayerInitialData: Elyos spawn map={EMap}, Asmodian spawn map={AMap}",
            ElyosSpawn.MapId, AsmodianSpawn.MapId);
    }

    public SpawnLocation GetSpawnLocation(Race race) => race switch
    {
        Race.ELYOS     => ElyosSpawn,
        Race.ASMODIANS => AsmodianSpawn,
        _              => ElyosSpawn
    };
}

public sealed record SpawnLocation(int MapId, float X, float Y, float Z, byte Heading);

[XmlRoot("player_initial_data")]
public sealed class PlayerInitialDataXml
{
    [XmlElement("elyos_spawn_location")]    public SpawnLocationXml? ElyosSpawn    { get; set; }
    [XmlElement("asmodian_spawn_location")] public SpawnLocationXml? AsmodianSpawn { get; set; }
}

public sealed class SpawnLocationXml
{
    [XmlAttribute("map_id")]  public int   MapId   { get; set; }
    [XmlAttribute("x")]       public float X       { get; set; }
    [XmlAttribute("y")]       public float Y       { get; set; }
    [XmlAttribute("z")]       public float Z       { get; set; }
    [XmlAttribute("heading")] public byte  Heading { get; set; }
}
