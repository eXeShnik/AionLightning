using System.Xml;
using AionLightning.Game.Model;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

/// <summary>
/// Holds tribe relation data from tribe_relations.xml.
/// Used by NpcAiService to determine whether an NPC is aggressive toward a player
/// based on the Java TribeRelationService.isAggressive() rules.
/// </summary>
public sealed class TribeData
{
    private sealed class TribeRelation
    {
        public string       Base   { get; init; } = string.Empty;
        public HashSet<string> Aggro  { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Hostile { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private readonly Dictionary<string, TribeRelation> _data =
        new(StringComparer.OrdinalIgnoreCase);

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "tribe", "tribe_relations.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("TribeData: tribe_relations.xml not found at {Path}", path);
            return;
        }

        using var reader = XmlReader.Create(path, new XmlReaderSettings
        {
            IgnoreComments   = true,
            IgnoreWhitespace = true,
        });

        TribeRelation? current = null;
        string?        name    = null;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.LocalName)
                {
                    case "tribe":
                        name    = reader.GetAttribute("name");
                        current = name is not null
                            ? new TribeRelation { Base = reader.GetAttribute("base") ?? name }
                            : null;
                        break;

                    case "aggro" when current is not null && !reader.IsEmptyElement:
                        var aggroText = reader.ReadElementContentAsString();
                        foreach (var t in aggroText.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                            current.Aggro.Add(t);
                        continue; // ReadElementContentAsString already advanced

                    case "hostile" when current is not null && !reader.IsEmptyElement:
                        var hostileText = reader.ReadElementContentAsString();
                        foreach (var t in hostileText.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                            current.Hostile.Add(t);
                        continue;
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "tribe")
            {
                if (name is not null && current is not null)
                    _data[name] = current;
                name    = null;
                current = null;
            }
        }

        log.LogInformation("TribeData: loaded {Count} tribe relations", _data.Count);
    }

    // Player tribe strings, matching the Java model
    private static string PlayerTribe(Race race) => race == Race.ELYOS ? "PC" : "PC_DARK";

    /// <summary>
    /// Returns true if an NPC with <paramref name="npcTribe"/> is aggressive toward a player of
    /// <paramref name="playerRace"/>, using the Java TribeRelationService.isAggressive() logic.
    /// </summary>
    public bool IsAggressiveToPlayer(string npcTribe, Race playerRace)
    {
        string playerTribe = PlayerTribe(playerRace);

        // Resolve the NPC's base tribe
        string baseTribe = _data.TryGetValue(npcTribe, out var rel)
            ? rel.Base
            : npcTribe; // no explicit entry → base = tribe name itself

        // Hardcoded GUARD/GUARD_DARK base-type rules (mirrors Java TribeRelationService)
        switch (baseTribe.ToUpperInvariant())
        {
            case "GUARD":
                // Elyos guards attack Asmodian players (and GUARD_DARK, GENERAL_DARK)
                if (playerRace == Race.ASMODIANS) return true;
                break;
            case "GUARD_DARK":
                // Asmodian guards attack Elyos players (and GUARD, GENERAL)
                if (playerRace == Race.ELYOS) return true;
                break;
            case "GUARD_DRAGON":
                // Balaur guards attack everyone
                return true;
        }

        // Explicit aggro/hostile set lookup
        if (rel is not null)
            return rel.Aggro.Contains(playerTribe) || rel.Hostile.Contains(playerTribe);

        // Unknown tribe — treat MONSTER base as aggressive, everything else as passive
        return string.Equals(baseTribe, "MONSTER", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns true if an NPC of <paramref name="helperTribe"/> will assist an NPC of
    /// <paramref name="victimTribe"/> that is under attack. Same tribe or same base tribe = support.
    /// </summary>
    public bool IsSupport(string helperTribe, string victimTribe)
    {
        if (string.Equals(helperTribe, victimTribe, StringComparison.OrdinalIgnoreCase)) return true;
        string helperBase = _data.TryGetValue(helperTribe, out var hr) ? hr.Base : helperTribe;
        string victimBase = _data.TryGetValue(victimTribe, out var vr) ? vr.Base : victimTribe;
        return string.Equals(helperBase, victimBase, StringComparison.OrdinalIgnoreCase);
    }

    public int Count => _data.Count;
}
