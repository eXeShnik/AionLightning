using System.Xml;
using AionLightning.Game.Model.Templates.Housing;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

/// <summary>
/// Loads housing/house_parts.xml (Java dataholders.HousePartsData) — the catalogue of placeable
/// decoration templates (id, <see cref="PartType"/> slot, quality, building-tag applicability).
/// note: this port's <see cref="Building"/>/<see cref="HouseParts"/> template already carries literal
/// default part ids per building directly (from house_buildings.xml's &lt;parts&gt; child — see
/// <see cref="HousingData"/>), so default-part rendering (<see cref="Model.House.HouseRegistry"/>) does
/// not need to resolve anything through this catalogue. It is kept for id/tag lookups a future
/// custom-furniture/placement phase would need (Java's tag-matched "parts available for this building"
/// query, <see cref="GetPartsForTag"/>), and is otherwise unused in this phase.
/// </summary>
public sealed class HousePartsData
{
    private readonly Dictionary<int, HousePart> _partsById = new();
    private readonly Dictionary<string, List<HousePart>> _partsByTag = new(StringComparer.OrdinalIgnoreCase);

    public int Count => _partsById.Count;

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "housing", "house_parts.xml");
        if (!File.Exists(path))
        {
            // note: house_parts.xml ships with the repo's static data; this fallback only guards a
            // stripped-down deployment missing it. Default-part rendering does not depend on this
            // catalogue being populated (see class doc), so an empty set is safe here.
            log.LogWarning("HousePartsData: house_parts.xml not found at {Path} — loading empty catalogue", path);
            return;
        }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "house_part") continue;

            int id = int.TryParse(reader.GetAttribute("id"), out int pid) ? pid : 0;
            if (id == 0) continue;

            var type = ParsePartType(reader.GetAttribute("type"));
            if (type is null) continue;

            var part = new HousePart(id, reader.GetAttribute("name"), type.Value,
                reader.GetAttribute("quality"), ParseTags(reader.GetAttribute("building_tags")));

            _partsById[id] = part;
            foreach (var tag in part.Tags)
            {
                if (!_partsByTag.TryGetValue(tag, out var list))
                    _partsByTag[tag] = list = new List<HousePart>();
                list.Add(part);
            }
        }

        log.LogInformation("HousePartsData: loaded {Count} house part(s)", _partsById.Count);
    }

    public HousePart? GetPartById(int partId) => _partsById.GetValueOrDefault(partId);

    public IReadOnlyList<HousePart> GetPartsForTag(string buildingTag) =>
        _partsByTag.TryGetValue(buildingTag, out var list) ? list : Array.Empty<HousePart>();

    private static List<string> ParseTags(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? new List<string>()
            : value.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

    private static PartType? ParsePartType(string? value) => value switch
    {
        "ROOF" => PartType.ROOF,
        "OUTWALL" => PartType.OUTWALL,
        "FRAME" => PartType.FRAME,
        "DOOR" => PartType.DOOR,
        "GARDEN" => PartType.GARDEN,
        "FENCE" => PartType.FENCE,
        "INWALL_ANY" => PartType.INWALL_ANY,
        "INFLOOR_ANY" => PartType.INFLOOR_ANY,
        "ADDON" => PartType.ADDON,
        _ => null,
    };
}
