using System.Xml.Serialization;
using AionLightning.Game.Model.Templates.CuringZones;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

// XML binding type for data/static_data/curing_objects/curing_objects.xml (Java
// dataholders.CuringObjectsData port).
[XmlRoot("curing_objects")]
public sealed class CuringObjectsFileXml
{
    [XmlElement("curing_object")] public List<CuringTemplate> CuringObject { get; set; } = new();
}

/// <summary>Java dataholders.CuringObjectsData — loads the curing-point spot templates (heal/DoT zones
/// that are a point+range check, not a zone polygon) from curing_objects.xml.</summary>
public sealed class CuringObjectsData
{
    private static readonly XmlSerializer _serializer = new(typeof(CuringObjectsFileXml));
    private readonly List<CuringTemplate> _objects = new();

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "curing_objects", "curing_objects.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("CuringObjectsData: curing_objects.xml not found at {Path}", path);
            return;
        }

        using var fs = File.OpenRead(path);
        if (_serializer.Deserialize(fs) is CuringObjectsFileXml file)
            _objects.AddRange(file.CuringObject);

        log.LogInformation("CuringObjectsData: loaded {Count} curing objects", _objects.Count);
    }

    public IReadOnlyList<CuringTemplate> CuringObjects => _objects;
    public int Count => _objects.Count;
}
