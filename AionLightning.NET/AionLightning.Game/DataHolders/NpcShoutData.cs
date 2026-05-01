using System.Xml;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

/// <summary>
/// Holds NPC shout entries from npc_shouts.xml.
/// Indexed by (npcId, eventType) for fast lookup during NPC AI events.
/// </summary>
public sealed class NpcShoutData
{
    public enum ShoutEventType { SEE, IDLE, ATTACK, ATTACK_BEGIN, ATTACK_END, DIED, CAST_K, ATTACK_K,
        PLAYER_MAGIC, PLAYER_SNARE, PLAYER_DEBUFF, PLAYER_SLAVE, PLAYER_BLOW, SWITCH_TARGET, GOD_HELP, WAKEUP, OTHER }

    public readonly record struct ShoutEntry(int StringId, int RestrictWorld);

    // npcId → eventType → list of shout entries
    private readonly Dictionary<int, Dictionary<ShoutEventType, List<ShoutEntry>>> _data = new();

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "npc_shouts", "npc_shouts.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("NpcShoutData: npc_shouts.xml not found at {Path}", path);
            return;
        }

        using var reader = XmlReader.Create(path, new XmlReaderSettings
        {
            IgnoreComments   = true,
            IgnoreWhitespace = true,
        });

        int[]    currentNpcIds     = Array.Empty<int>();
        int      currentRestrictWorld = 0;
        int      totalShouts       = 0;

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element && reader.NodeType != XmlNodeType.EndElement)
                continue;

            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.LocalName)
                {
                    case "shout_npcs":
                        currentNpcIds       = ParseNpcIds(reader.GetAttribute("npc_ids"));
                        currentRestrictWorld = int.TryParse(reader.GetAttribute("restrict_world"), out int rw) ? rw : 0;
                        break;

                    case "shout" when currentNpcIds.Length > 0:
                        var whenStr = reader.GetAttribute("when") ?? string.Empty;
                        if (!int.TryParse(reader.GetAttribute("string_id"), out int stringId) || stringId == 0)
                            break;

                        var evt = ParseEventType(whenStr);
                        var entry = new ShoutEntry(stringId, currentRestrictWorld);

                        foreach (int npcId in currentNpcIds)
                        {
                            if (!_data.TryGetValue(npcId, out var evtMap))
                            {
                                evtMap = new Dictionary<ShoutEventType, List<ShoutEntry>>();
                                _data[npcId] = evtMap;
                            }
                            if (!evtMap.TryGetValue(evt, out var list))
                            {
                                list = new List<ShoutEntry>();
                                evtMap[evt] = list;
                            }
                            list.Add(entry);
                        }
                        totalShouts++;
                        break;
                }
            }
            else if (reader.LocalName == "shout_npcs")
            {
                currentNpcIds        = Array.Empty<int>();
                currentRestrictWorld = 0;
            }
        }

        log.LogInformation("NpcShoutData: loaded {Shouts} shout entries for {Npcs} NPCs", totalShouts, _data.Count);
    }

    /// <summary>Returns a random shout entry for the given NPC + event + world, or null if none configured.</summary>
    public ShoutEntry? GetRandomShout(int npcId, ShoutEventType evt, int worldId)
    {
        if (!_data.TryGetValue(npcId, out var evtMap)) return null;
        if (!evtMap.TryGetValue(evt, out var list)) return null;

        // Filter by world restriction
        var eligible = worldId != 0
            ? list.Where(e => e.RestrictWorld == 0 || e.RestrictWorld == worldId).ToList()
            : list;

        if (eligible.Count == 0) return null;
        return eligible[Random.Shared.Next(eligible.Count)];
    }

    public bool HasShouts(int npcId) => _data.ContainsKey(npcId);

    private static int[] ParseNpcIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<int>();
        var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var ids   = new List<int>(parts.Length);
        foreach (var p in parts)
            if (int.TryParse(p, out int id))
                ids.Add(id);
        return ids.ToArray();
    }

    private static ShoutEventType ParseEventType(string when) => when.ToUpperInvariant() switch
    {
        "SEE"           => ShoutEventType.SEE,
        "IDLE"          => ShoutEventType.IDLE,
        "ATTACK"        => ShoutEventType.ATTACK,
        "ATTACK_BEGIN"  => ShoutEventType.ATTACK_BEGIN,
        "ATTACK_END"    => ShoutEventType.ATTACK_END,
        "DIED"          => ShoutEventType.DIED,
        "CAST_K"        => ShoutEventType.CAST_K,
        "ATTACK_K"      => ShoutEventType.ATTACK_K,
        "PLAYER_MAGIC"  => ShoutEventType.PLAYER_MAGIC,
        "PLAYER_SNARE"  => ShoutEventType.PLAYER_SNARE,
        "PLAYER_DEBUFF" => ShoutEventType.PLAYER_DEBUFF,
        "PLAYER_SLAVE"  => ShoutEventType.PLAYER_SLAVE,
        "PLAYER_BLOW"   => ShoutEventType.PLAYER_BLOW,
        "SWITCH_TARGET" => ShoutEventType.SWITCH_TARGET,
        "GOD_HELP"      => ShoutEventType.GOD_HELP,
        "WAKEUP"        => ShoutEventType.WAKEUP,
        _               => ShoutEventType.OTHER,
    };

    public int Count => _data.Count;
}
