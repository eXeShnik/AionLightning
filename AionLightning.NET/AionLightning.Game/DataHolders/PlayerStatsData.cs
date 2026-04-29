using System.Xml;
using System.Xml.Serialization;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Stats;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class PlayerStatsData
{
    private readonly Dictionary<int, PlayerStatsTemplate> _templates = new();

    public void Load(string dataRoot, ILogger log)
    {
        var dir = Path.Combine(dataRoot, "stats", "player");
        if (!Directory.Exists(dir))
        {
            log.LogWarning("PlayerStatsData: directory not found at {Dir}", dir);
            return;
        }

        var serializer = new XmlSerializer(typeof(PlayerStatsTemplatesXml));
        int count = 0;

        foreach (var file in Directory.EnumerateFiles(dir, "*-templates.xml"))
        {
            using var reader = XmlReader.Create(file);
            var root = (PlayerStatsTemplatesXml?)serializer.Deserialize(reader);
            if (root?.Entries is null) continue;

            foreach (var entry in root.Entries)
            {
                if (!Enum.TryParse<PlayerClass>(entry.Class, true, out var cls)) continue;
                var t = entry.Template;
                if (t is null) continue;

                if (t.Will > 0)   t.MaxMp = (int)MathF.Round(t.MaxMp   * 100f / t.Will);
                if (t.Health > 0) t.MaxHp = (int)MathF.Round(t.MaxHp   * 100f / t.Health);
                int agilityFactor = t.Agility - 100;
                t.Evasion = (int)MathF.Round(t.Evasion - t.Evasion * agilityFactor * 0.003f);
                t.Block   = (int)MathF.Round(t.Block   - t.Block   * agilityFactor * 0.0025f);
                t.Parry   = (int)MathF.Round(t.Parry   - t.Parry   * agilityFactor * 0.0025f);

                _templates[MakeHash(cls, entry.Level)] = t;
                count++;
            }
        }

        foreach (PlayerClass cls in Enum.GetValues<PlayerClass>())
        {
            int fallbackKey = MakeHash(cls, 0);
            if (!_templates.ContainsKey(fallbackKey))
                _templates[fallbackKey] = BuildFallback(cls);
        }

        log.LogInformation("PlayerStatsData: loaded {Count} templates", count);
    }

    public PlayerStatsTemplate? GetTemplate(PlayerClass cls, int level)
    {
        if (_templates.TryGetValue(MakeHash(cls, level), out var t)) return t;
        _templates.TryGetValue(MakeHash(cls, 0), out var fallback);
        return fallback;
    }

    private static int MakeHash(PlayerClass cls, int level) => (level << 11) | (int)cls;

    private static PlayerStatsTemplate BuildFallback(PlayerClass cls) => cls switch
    {
        PlayerClass.WARRIOR or PlayerClass.GLADIATOR or PlayerClass.TEMPLAR
            => new PlayerStatsTemplate { MaxHp = 1000, MaxMp = 500, Power = 110, Health = 110, Agility = 100, Accuracy = 100, Knowledge = 90,  Will = 90,  Evasion = 74, Block = 74, Parry = 74, MainHandAttack = 19, MainHandAccuracy = 198, MainHandCritRate = 2, MagicAccuracy = 14 },
        PlayerClass.SCOUT or PlayerClass.RANGER or PlayerClass.ASSASSIN
            => new PlayerStatsTemplate { MaxHp = 900,  MaxMp = 500, Power = 100, Health = 100, Agility = 110, Accuracy = 110, Knowledge = 90,  Will = 90,  Evasion = 80, Block = 50, Parry = 50, MainHandAttack = 21, MainHandAccuracy = 210, MainHandCritRate = 3, MagicAccuracy = 14 },
        PlayerClass.MAGE or PlayerClass.SORCERER or PlayerClass.SPIRIT_MASTER
            => new PlayerStatsTemplate { MaxHp = 700,  MaxMp = 800, Power = 90,  Health = 90,  Agility = 90,  Accuracy = 90,  Knowledge = 120, Will = 120, Evasion = 60, Block = 40, Parry = 40, MainHandAttack = 15, MainHandAccuracy = 180, MainHandCritRate = 2, MagicAccuracy = 50 },
        PlayerClass.PRIEST or PlayerClass.CLERIC or PlayerClass.CHANTER
            => new PlayerStatsTemplate { MaxHp = 800,  MaxMp = 700, Power = 100, Health = 100, Agility = 95,  Accuracy = 100, Knowledge = 100, Will = 110, Evasion = 65, Block = 50, Parry = 50, MainHandAttack = 16, MainHandAccuracy = 190, MainHandCritRate = 2, MagicAccuracy = 30 },
        _ => new PlayerStatsTemplate { MaxHp = 800, MaxMp = 600, Power = 100, Health = 100, Agility = 100, Accuracy = 100, Knowledge = 100, Will = 100, Evasion = 70, Block = 60, Parry = 60, MainHandAttack = 18, MainHandAccuracy = 195, MainHandCritRate = 2, MagicAccuracy = 20 }
    };
}

[XmlRoot("player_stats_templates")]
public sealed class PlayerStatsTemplatesXml
{
    [XmlElement("player_stats")] public List<PlayerStatsEntryXml> Entries { get; set; } = new();
}

public sealed class PlayerStatsEntryXml
{
    [XmlAttribute("class")] public string Class { get; set; } = "";
    [XmlAttribute("level")] public int Level { get; set; }
    [XmlElement("stats_template")] public PlayerStatsTemplate? Template { get; set; }
}
