using System.Xml;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class GlobalDropData
{
    private readonly record struct Rule(
        float Chance,
        long MinCount,
        long MaxCount,
        int MinDiff,
        int MaxDiff,
        string RestrictionRace,
        HashSet<string> Worlds,
        HashSet<string> Races,
        HashSet<string> Ratings,
        HashSet<int> Maps,
        int[] Items
    );

    private readonly List<Rule> _rules = new();

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "global_drops", "global_rules.xml");
        if (!File.Exists(path)) { log.LogWarning("GlobalDropData: global_rules.xml not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });

        Rule current = default;
        var worlds  = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var races   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ratings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var maps    = new HashSet<int>();
        var items   = new List<int>();
        bool inRule = false;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "gd_rule" && inRule)
            {
                if (items.Count > 0)
                {
                    _rules.Add(current with
                    {
                        Worlds  = new HashSet<string>(worlds,  StringComparer.OrdinalIgnoreCase),
                        Races   = new HashSet<string>(races,   StringComparer.OrdinalIgnoreCase),
                        Ratings = new HashSet<string>(ratings, StringComparer.OrdinalIgnoreCase),
                        Maps    = new HashSet<int>(maps),
                        Items   = items.ToArray(),
                    });
                }
                inRule = false;
                continue;
            }

            if (reader.NodeType != XmlNodeType.Element) continue;

            switch (reader.LocalName)
            {
                case "gd_rule":
                    worlds.Clear(); races.Clear(); ratings.Clear(); maps.Clear(); items.Clear();
                    float.TryParse(reader.GetAttribute("base_chance"), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float chance);
                    long.TryParse(reader.GetAttribute("min_count"), out long minCount);
                    long.TryParse(reader.GetAttribute("max_count"), out long maxCount);
                    if (minCount == 0) minCount = 1;
                    if (maxCount == 0) maxCount = minCount;
                    int minDiff = int.MinValue, maxDiff = int.MaxValue;
                    if (int.TryParse(reader.GetAttribute("min_diff"), out int md)) minDiff = md;
                    if (int.TryParse(reader.GetAttribute("max_diff"), out int xd)) maxDiff = xd;
                    current = new Rule(chance, minCount, maxCount, minDiff, maxDiff,
                        reader.GetAttribute("restriction_race") ?? string.Empty,
                        worlds, races, ratings, maps, Array.Empty<int>());
                    inRule = true;
                    break;

                case "gd_world" when inRule:
                    var wt = reader.GetAttribute("wd_type");
                    if (!string.IsNullOrEmpty(wt)) worlds.Add(wt);
                    break;

                case "gd_race" when inRule:
                    var rc = reader.GetAttribute("race");
                    if (!string.IsNullOrEmpty(rc)) races.Add(rc);
                    break;

                case "gd_rating" when inRule:
                    var rt = reader.GetAttribute("rating");
                    if (!string.IsNullOrEmpty(rt)) ratings.Add(rt);
                    break;

                case "gd_map" when inRule:
                    if (int.TryParse(reader.GetAttribute("map_id"), out int mapId))
                        maps.Add(mapId);
                    break;

                case "gd_item" when inRule:
                    if (int.TryParse(reader.GetAttribute("id"), out int itemId))
                        items.Add(itemId);
                    break;
            }
        }

        log.LogInformation("GlobalDropData: loaded {Count} global drop rules", _rules.Count);
    }

    public IEnumerable<(int ItemId, long Count)> GetGlobalDrops(
        int npcLevel, string npcRating, string npcRace, string worldDropType, int worldId, int playerLevel, string playerRace)
    {
        int levelDiff = playerLevel - npcLevel;
        foreach (var rule in _rules)
        {
            if (rule.Items.Length == 0) continue;
            if (rule.Worlds.Count  > 0 && !rule.Worlds.Contains(worldDropType))  continue;
            if (rule.Maps.Count    > 0 && !rule.Maps.Contains(worldId))          continue;
            if (rule.Races.Count   > 0 && !rule.Races.Contains(npcRace))         continue;
            if (rule.Ratings.Count > 0 && !rule.Ratings.Contains(npcRating))     continue;
            if (levelDiff < rule.MinDiff || levelDiff > rule.MaxDiff)             continue;
            if (!string.IsNullOrEmpty(rule.RestrictionRace) &&
                !rule.RestrictionRace.Equals(playerRace, StringComparison.OrdinalIgnoreCase)) continue;
            if (Random.Shared.NextDouble() * 100.0 >= rule.Chance)                continue;

            var itemId = rule.Items[Random.Shared.Next(rule.Items.Length)];
            long count = rule.MinCount == rule.MaxCount
                ? rule.MinCount
                : (long)Random.Shared.Next((int)rule.MinCount, (int)rule.MaxCount + 1);
            yield return (itemId, count);
        }
    }
}
