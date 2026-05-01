using System.Xml;
using AionLightning.Game.Model;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class InstanceExitData
{
    public readonly record struct ExitLocation(int ExitWorldId, float X, float Y, float Z, byte Heading);

    // (instanceWorldId, "ELYOS"|"ASMODIANS") → exit location
    private readonly Dictionary<(int, string), ExitLocation> _exits = new();

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "instance_exit", "instance_exit.xml");
        if (!File.Exists(path)) { log.LogWarning("InstanceExitData: instance_exit.xml not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
        int count = 0;
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "instance_exit") continue;
            if (!int.TryParse(reader.GetAttribute("instance_id"), out int instanceId)) continue;
            if (!int.TryParse(reader.GetAttribute("exit_world"),  out int exitWorld))  continue;
            float.TryParse(reader.GetAttribute("x"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x);
            float.TryParse(reader.GetAttribute("y"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y);
            float.TryParse(reader.GetAttribute("z"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z);
            byte.TryParse(reader.GetAttribute("h"), out byte h);
            var race = reader.GetAttribute("race") ?? string.Empty;
            _exits[(instanceId, race)] = new ExitLocation(exitWorld, x, y, z, h);
            count++;
        }

        log.LogInformation("InstanceExitData: loaded {Count} instance exit locations", count);
    }

    /// <summary>Returns the exit location for a player leaving the given instance world.</summary>
    public ExitLocation? GetExit(int instanceWorldId, Race playerRace)
    {
        var raceStr = playerRace == Race.ELYOS ? "ELYOS" : "ASMODIANS";
        if (_exits.TryGetValue((instanceWorldId, raceStr), out var loc)) return loc;
        return null;
    }
}
