using System.Xml.Serialization;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

[XmlRoot("player_experience_table")]
internal sealed class PlayerExperienceTableXml
{
    [XmlElement("exp")] public long[] Experience { get; set; } = Array.Empty<long>();
}

public sealed class PlayerExperienceTable
{
    private long[] _experience = Array.Empty<long>();
    private static readonly XmlSerializer _serializer = new(typeof(PlayerExperienceTableXml));

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "player_experience_table.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("PlayerExperienceTable: file not found: {Path}", path);
            return;
        }

        using var fs = File.OpenRead(path);
        var xml = (PlayerExperienceTableXml)_serializer.Deserialize(fs)!;
        _experience = xml.Experience;

        log.LogInformation("PlayerExperienceTable: loaded {Count} levels", _experience.Length);
    }

    public int MaxLevel => _experience.Length;

    public long GetStartExpForLevel(int level)
    {
        if (level <= 0) return 0;
        if (level > _experience.Length) return _experience[^1];
        return _experience[level - 1];
    }

    public int GetLevelForExp(long exp)
    {
        for (int i = _experience.Length; i > 0; i--)
        {
            if (exp >= _experience[i - 1])
            {
                int level = i;
                return level >= MaxLevel ? MaxLevel - 1 : level;
            }
        }
        return 1;
    }
}
