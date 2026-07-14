using System.Globalization;
using System.Xml;
using AionLightning.Game.Model.AutoGroup;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

/// <summary>Loads <c>auto_group.xml</c> (Java <c>dataholders.AutoGroupData</c>) into templates keyed by mask id.</summary>
public sealed class AutoGroupData
{
    private readonly Dictionary<int, AutoGroupTemplate> _byMaskId = new();

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "auto_group", "auto_group.xml");
        if (!File.Exists(path)) { log.LogWarning("AutoGroupData: auto_group.xml not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "auto_group") continue;
            if (!int.TryParse(reader.GetAttribute("id"), out int maskId)) continue;

            int.TryParse(reader.GetAttribute("instanceId"), out int instanceMapId);
            int.TryParse(reader.GetAttribute("name_id"), out int nameId);
            int.TryParse(reader.GetAttribute("title_id"), out int titleId);
            int.TryParse(reader.GetAttribute("min_lvl"), out int minLevel);
            int.TryParse(reader.GetAttribute("max_lvl"), out int maxLevel);
            bool.TryParse(reader.GetAttribute("register_new"), out bool registerNew);
            bool.TryParse(reader.GetAttribute("register_quick"), out bool registerQuick);
            bool.TryParse(reader.GetAttribute("register_group"), out bool registerGroup);

            var npcIdsAttr = reader.GetAttribute("npc_ids") ?? string.Empty;
            var npcIds = npcIdsAttr
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : 0)
                .Where(n => n != 0)
                .ToList();

            _byMaskId[maskId] = new AutoGroupTemplate(maskId, instanceMapId, nameId, titleId, minLevel, maxLevel,
                registerNew, registerQuick, registerGroup, npcIds);
        }

        log.LogInformation("AutoGroupData: loaded {Count} auto-group templates", _byMaskId.Count);
    }

    public AutoGroupTemplate? GetByMaskId(int maskId) => _byMaskId.TryGetValue(maskId, out var t) ? t : null;

    public int Size => _byMaskId.Count;
}
