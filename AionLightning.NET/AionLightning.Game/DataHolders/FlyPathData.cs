using System.Globalization;
using System.Xml;
using AionLightning.Game.Model.Templates.FlyPath;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

/// <summary>
/// Loads <c>flypath_template.xml</c> — flight-master glide routes keyed by id (Java
/// <c>dataholders.FlyPathData</c>). The id matches the <c>loc_id</c> of the FLIGHT-type
/// &lt;telelocation&gt; entry in <c>npc_teleporter.xml</c> that offers the destination, so callers
/// look the entry up directly by that loc_id (see <see cref="TeleportData.GetDestination"/>).
/// </summary>
public sealed class FlyPathData
{
    private readonly Dictionary<short, FlyPathEntry> _byId = new();

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "flypath_template.xml");
        if (!File.Exists(path)) { log.LogWarning("FlyPathData: flypath_template.xml not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "flypath_location") continue;
            if (!short.TryParse(reader.GetAttribute("id"), out short id)) continue;

            var entry = new FlyPathEntry(
                id,
                ParseF(reader.GetAttribute("sx")), ParseF(reader.GetAttribute("sy")), ParseF(reader.GetAttribute("sz")),
                ParseI(reader.GetAttribute("sworld")),
                ParseF(reader.GetAttribute("ex")), ParseF(reader.GetAttribute("ey")), ParseF(reader.GetAttribute("ez")),
                ParseI(reader.GetAttribute("eworld")),
                (int)(ParseF(reader.GetAttribute("time")) * 1000));

            _byId[id] = entry;
        }

        log.LogInformation("FlyPathData: loaded {Count} fly paths", _byId.Count);
    }

    public FlyPathEntry? GetPathTemplate(short id) => _byId.GetValueOrDefault(id);

    public int Count => _byId.Count;

    private static float ParseF(string? s)
        => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;

    private static int ParseI(string? s) => int.TryParse(s, out int v) ? v : 0;
}
