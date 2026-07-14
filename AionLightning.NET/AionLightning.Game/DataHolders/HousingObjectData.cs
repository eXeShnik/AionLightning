using System.Globalization;
using System.Xml;
using AionLightning.Game.Model.Templates.Housing;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

/// <summary>
/// Loads housing/housing_objects.xml (Java dataholders.HousingObjectData) — the catalogue of placeable
/// furniture/decoration templates (chairs, jukeboxes, storages, use-items, house NPCs, etc.) referenced by
/// <see cref="Model.GameObjects.HouseObject.TemplateId"/> and by an item template's
/// &lt;actions&gt;&lt;houseobject id="..."/&gt; action (see Services.Item.HouseObjectFactory).
/// </summary>
public sealed class HousingObjectData
{
    private readonly Dictionary<int, HousingObjectTemplate> _templatesById = new();

    public int Count => _templatesById.Count;

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "housing", "housing_objects.xml");
        if (!File.Exists(path))
        {
            // note: housing_objects.xml ships with the repo's static data; this fallback only guards a
            // stripped-down deployment missing it. HouseObjectFactory simply fails to resolve a template
            // (returns null) when this catalogue is empty — callers already treat that as "can't place".
            log.LogWarning("HousingObjectData: housing_objects.xml not found at {Path} — loading empty catalogue", path);
            return;
        }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });

        HousingObjectKind? kind = null;
        int id = 0, nameId = 0, npcId = 0, warehouseId = 0, level = 0, cd = 0, delay = 0, useDays = 0;
        float talkingDistance = 0f;
        string? quality = null, category = null, area = null, location = null, limit = null;
        bool ownerOnly = false;
        int? useCount = null, requiredItem = null;
        HousingObjectUseAction? action = null;
        bool inObject = false;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                if (!inObject && TryParseKind(reader.LocalName, out var parsedKind))
                {
                    kind = parsedKind;
                    id = ParseInt(reader.GetAttribute("id"));
                    nameId = ParseInt(reader.GetAttribute("name_id"));
                    quality = reader.GetAttribute("quality");
                    category = reader.GetAttribute("category");
                    area = reader.GetAttribute("area");
                    location = reader.GetAttribute("location");
                    talkingDistance = ParseFloat(reader.GetAttribute("talking_distance"));
                    useDays = ParseInt(reader.GetAttribute("use_days"));
                    limit = reader.GetAttribute("limit");
                    npcId = ParseInt(reader.GetAttribute("npc_id"));
                    warehouseId = ParseInt(reader.GetAttribute("warehouse_id"));
                    ownerOnly = string.Equals(reader.GetAttribute("owner"), "true", StringComparison.OrdinalIgnoreCase);
                    cd = ParseInt(reader.GetAttribute("cd"));
                    delay = ParseInt(reader.GetAttribute("delay"));
                    useCount = ParseIntOrNull(reader.GetAttribute("use_count"));
                    requiredItem = ParseIntOrNull(reader.GetAttribute("required_item"));
                    level = ParseInt(reader.GetAttribute("level"));
                    action = null;

                    bool isEmpty = reader.IsEmptyElement;
                    inObject = !isEmpty;
                    if (isEmpty) Commit();
                    continue;
                }

                if (inObject && reader.LocalName == "action")
                {
                    action = new HousingObjectUseAction(
                        ParseIntOrNull(reader.GetAttribute("final_reward_id")),
                        ParseIntOrNull(reader.GetAttribute("reward_id")),
                        ParseIntOrNull(reader.GetAttribute("remove_count")),
                        ParseIntOrNull(reader.GetAttribute("check_type")));
                    continue;
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement && inObject && TryParseKind(reader.LocalName, out _))
            {
                Commit();
                inObject = false;
            }
        }

        log.LogInformation("HousingObjectData: loaded {Count} housing object template(s)", _templatesById.Count);
        return;

        void Commit()
        {
            if (id == 0 || kind is null) return;
            _templatesById[id] = new HousingObjectTemplate(
                id, kind.Value, nameId, quality, category, area, location, talkingDistance, useDays, limit,
                npcId, warehouseId, ownerOnly, cd, delay, useCount, requiredItem, level, action);
        }
    }

    public HousingObjectTemplate? GetTemplateById(int templateId) => _templatesById.GetValueOrDefault(templateId);

    private static bool TryParseKind(string elementName, out HousingObjectKind kind)
    {
        switch (elementName)
        {
            case "passive": kind = HousingObjectKind.Passive; return true;
            case "chair": kind = HousingObjectKind.Chair; return true;
            case "jukebox": kind = HousingObjectKind.JukeBox; return true;
            case "moviejukebox": kind = HousingObjectKind.MovieJukeBox; return true;
            case "move_item": kind = HousingObjectKind.MoveableItem; return true;
            case "npc": kind = HousingObjectKind.Npc; return true;
            case "picture": kind = HousingObjectKind.Picture; return true;
            case "postbox": kind = HousingObjectKind.Postbox; return true;
            case "storage": kind = HousingObjectKind.Storage; return true;
            case "use_item": kind = HousingObjectKind.UseableItem; return true;
            case "emblem": kind = HousingObjectKind.Emblem; return true;
            default: kind = default; return false;
        }
    }

    private static int ParseInt(string? value) => int.TryParse(value, out int result) ? result : 0;

    private static int? ParseIntOrNull(string? value) => int.TryParse(value, out int result) ? result : null;

    private static float ParseFloat(string? value) =>
        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : 0f;
}
