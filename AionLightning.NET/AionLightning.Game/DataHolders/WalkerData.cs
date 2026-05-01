using System.Xml;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class WalkerData
{
    public readonly record struct RouteStep(float X, float Y, float Z);

    private readonly Dictionary<string, RouteStep[]> _routes = new(StringComparer.OrdinalIgnoreCase);

    public void Load(string dataRoot, ILogger log)
    {
        var dir = Path.Combine(dataRoot, "npc_walker");
        if (!Directory.Exists(dir))
        {
            log.LogWarning("WalkerData: npc_walker directory not found at {Dir}", dir);
            return;
        }

        int total = 0;
        foreach (var file in Directory.EnumerateFiles(dir, "*.xml", SearchOption.TopDirectoryOnly))
            total += LoadFile(file, log);

        log.LogInformation("WalkerData: loaded {Count} walker routes", total);
    }

    private int LoadFile(string path, ILogger log)
    {
        int loaded = 0;
        try
        {
            using var reader = XmlReader.Create(path, new XmlReaderSettings
            {
                IgnoreComments   = true,
                IgnoreWhitespace = true,
            });

            string?           routeId  = null;
            List<RouteStep>?  steps    = null;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName == "walker_template")
                    {
                        routeId = reader.GetAttribute("route_id");
                        steps   = routeId is not null ? new List<RouteStep>() : null;
                    }
                    else if (reader.LocalName == "routestep" && steps is not null)
                    {
                        if (float.TryParse(reader.GetAttribute("x"), System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out float x)
                            && float.TryParse(reader.GetAttribute("y"), System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out float y)
                            && float.TryParse(reader.GetAttribute("z"), System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out float z))
                        {
                            steps.Add(new RouteStep(x, y, z));
                        }
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "walker_template")
                {
                    if (routeId is not null && steps is { Count: > 0 })
                    {
                        _routes[routeId] = steps.ToArray();
                        loaded++;
                    }
                    routeId = null;
                    steps   = null;
                }
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "WalkerData: failed to parse {File}", Path.GetFileName(path));
        }
        return loaded;
    }

    public RouteStep[]? GetRoute(string routeId)
        => _routes.TryGetValue(routeId, out var route) ? route : null;

    public int Count => _routes.Count;
}
