using System.Xml;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Tribe;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

/// <summary>
/// Faithful port of Java <c>dataholders.TribeRelationsData</c> — loads every &lt;tribe&gt; entry from
/// <c>tribe_relations.xml</c> and answers raw, XML-driven relation queries between two
/// <see cref="TribeClass"/> values. Creature-level special-case logic (e.g. "GUARD_DARK is always
/// aggressive toward players/GUARD/GENERAL") is layered on top by
/// <see cref="Services.TribeRelationService"/>, mirroring the Java split between this dataholder and
/// <c>services.TribeRelationService</c>.
/// </summary>
public sealed class TribeRelationsData
{
    private readonly Dictionary<TribeClass, TribeRelationTemplate> _byName = new();

    public int Count => _byName.Count;

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "tribe", "tribe_relations.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("TribeRelationsData: tribe_relations.xml not found at {Path}", path);
            return;
        }

        using var reader = XmlReader.Create(path, new XmlReaderSettings
        {
            IgnoreComments   = true,
            IgnoreWhitespace = true,
        });

        TribeClass? name = null;
        TribeClass  @base = TribeClass.None;
        HashSet<TribeClass>? aggro = null, hostile = null, friend = null, neutral = null, none = null, support = null;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.LocalName)
                {
                    case "tribe":
                        var nameAttr = reader.GetAttribute("name");
                        if (nameAttr is null) { name = null; break; }
                        name    = new TribeClass(nameAttr);
                        @base   = new TribeClass(reader.GetAttribute("base") ?? nameAttr);
                        aggro   = new HashSet<TribeClass>();
                        hostile = new HashSet<TribeClass>();
                        friend  = new HashSet<TribeClass>();
                        neutral = new HashSet<TribeClass>();
                        none    = new HashSet<TribeClass>();
                        support = new HashSet<TribeClass>();
                        break;

                    case "aggro" or "hostile" or "friend" or "neutral" or "none" or "support"
                        when name is not null && !reader.IsEmptyElement:
                        var set = reader.LocalName switch
                        {
                            "aggro"   => aggro,
                            "hostile" => hostile,
                            "friend"  => friend,
                            "neutral" => neutral,
                            "none"    => none,
                            _         => support,
                        };
                        var text = reader.ReadElementContentAsString();
                        foreach (var t in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                            set!.Add(new TribeClass(t));
                        continue; // ReadElementContentAsString already advanced past the end element
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "tribe")
            {
                if (name is { } tribeName)
                {
                    _byName[tribeName] = new TribeRelationTemplate
                    {
                        Name    = tribeName,
                        Base    = @base,
                        Aggro   = aggro!,
                        Hostile = hostile!,
                        Friend  = friend!,
                        Neutral = neutral!,
                        None    = none!,
                        Support = support!,
                    };
                }
                name = null;
            }
        }

        log.LogInformation("TribeRelationsData: loaded {Count} tribe relations", _byName.Count);
    }

    public TribeClass GetBaseTribe(TribeClass tribeName)
        => _byName.TryGetValue(tribeName, out var tribe) ? tribe.Base : tribeName;

    public bool HasAggressiveRelations(TribeClass tribeName) => HasRelations(tribeName, t => t.Aggro);
    public bool HasHostileRelations(TribeClass tribeName)    => HasRelations(tribeName, t => t.Hostile);
    public bool HasSupportRelations(TribeClass tribeName)    => HasRelations(tribeName, t => t.Support);
    public bool HasFriendRelations(TribeClass tribeName)     => HasRelations(tribeName, t => t.Friend);
    public bool HasNoneRelations(TribeClass tribeName)       => HasRelations(tribeName, t => t.None);
    public bool HasNeutralRelations(TribeClass tribeName)    => HasRelations(tribeName, t => t.Neutral);

    private bool HasRelations(TribeClass tribeName, Func<TribeRelationTemplate, IReadOnlySet<TribeClass>> selector)
    {
        if (!_byName.TryGetValue(tribeName, out var tribe)) return false;
        if (selector(tribe).Count > 0) return true;
        return tribe.IsBasic && _byName.TryGetValue(tribe.Base, out var baseTribe) && selector(baseTribe).Count > 0;
    }

    public bool IsAggressiveRelation(TribeClass tribeName1, TribeClass tribeName2)
        => IsRelation(tribeName1, tribeName2, t => t.Aggro);

    public bool IsSupportRelation(TribeClass tribeName1, TribeClass tribeName2)
        => IsRelation(tribeName1, tribeName2, t => t.Support);

    public bool IsFriendlyRelation(TribeClass tribeName1, TribeClass tribeName2)
        => IsRelation(tribeName1, tribeName2, t => t.Friend);

    public bool IsNeutralRelation(TribeClass tribeName1, TribeClass tribeName2)
        => IsRelation(tribeName1, tribeName2, t => t.Neutral);

    public bool IsNoneRelation(TribeClass tribeName1, TribeClass tribeName2)
        => IsRelation(tribeName1, tribeName2, t => t.None);

    public bool IsHostileRelation(TribeClass tribeName1, TribeClass tribeName2)
        => IsRelation(tribeName1, tribeName2, t => t.Hostile);

    private bool IsRelation(TribeClass tribeName1, TribeClass tribeName2,
        Func<TribeRelationTemplate, IReadOnlySet<TribeClass>> selector)
    {
        if (!_byName.TryGetValue(tribeName1, out var tribe1) || !_byName.TryGetValue(tribeName2, out var tribe2))
            return false;

        var set1 = selector(tribe1);
        var set2 = selector(tribe2);
        return set1.Contains(tribe2.Base) || set1.Contains(tribeName2)
            || set2.Contains(tribe1.Base) || set2.Contains(tribeName1);
    }

    /// <summary>True if any other loaded tribe declares a support relation toward <paramref name="tribeName"/>.</summary>
    public bool HasAnySupporter(TribeClass tribeName)
    {
        if (!_byName.ContainsKey(tribeName)) return false;
        foreach (var other in _byName.Keys)
            if (IsSupportRelation(other, tribeName)) return true;
        return false;
    }

    // Player-race convenience overloads — a player has no XML <tribe> entry of its own; Java models it
    // via Player.getTribe() returning TribeClass.PC/PC_DARK by race, so relation checks against a race
    // are equivalent to checks against that fixed tribe.
    public bool IsAggressiveRelation(TribeClass tribe, Race race) => IsAggressiveRelation(tribe, PlayerTribe(race));
    public bool IsHostileRelation(TribeClass tribe, Race race)    => IsHostileRelation(tribe, PlayerTribe(race));
    public bool IsFriendlyRelation(TribeClass tribe, Race race)   => IsFriendlyRelation(tribe, PlayerTribe(race));
    public bool IsSupportRelation(TribeClass tribe, Race race)    => IsSupportRelation(tribe, PlayerTribe(race));
    public bool IsNeutralRelation(TribeClass tribe, Race race)    => IsNeutralRelation(tribe, PlayerTribe(race));

    private static TribeClass PlayerTribe(Race race) => race == Race.ELYOS ? TribeClass.Pc : TribeClass.PcDark;
}
