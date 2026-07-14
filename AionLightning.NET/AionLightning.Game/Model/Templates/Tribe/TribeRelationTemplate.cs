namespace AionLightning.Game.Model.Templates.Tribe;

/// <summary>
/// Faithful port of Java <c>model.templates.tribe.Tribe</c> — one &lt;tribe&gt; entry from
/// <c>tribe_relations.xml</c>: its declared relation sets toward other tribes, and its "base" tribe
/// (defaults to its own name when unset) used so XML-authored relations can target whole families of
/// tribes instead of every exact name.
/// </summary>
public sealed class TribeRelationTemplate
{
    public required TribeClass Name { get; init; }
    public required TribeClass Base { get; init; }

    public IReadOnlySet<TribeClass> Aggro   { get; init; } = new HashSet<TribeClass>();
    public IReadOnlySet<TribeClass> Hostile { get; init; } = new HashSet<TribeClass>();
    public IReadOnlySet<TribeClass> Friend  { get; init; } = new HashSet<TribeClass>();
    public IReadOnlySet<TribeClass> Neutral { get; init; } = new HashSet<TribeClass>();
    public IReadOnlySet<TribeClass> None    { get; init; } = new HashSet<TribeClass>();
    public IReadOnlySet<TribeClass> Support { get; init; } = new HashSet<TribeClass>();

    /// <summary>Java <c>Tribe.isBasic()</c> — true for the small set of "generic" tribes (PC, PC_DARK,
    /// GENERAL, GENERAL_DARK, GENERAL_DRAGON, GUARD, GUARD_DARK, GUARD_DRAGON, MONSTER, NPC, USEALL,
    /// FIELD_OBJECT_DARK, FIELD_OBJECT_LIGHT, NONE) that every other tribe's "base" attribute resolves
    /// to. Ported as a name-set check since the isBasic flag lived on the Java TribeClass enum literal
    /// itself, which this port does not replicate per-member (see <see cref="TribeClass"/>).</summary>
    public bool IsBasic => BasicTribeNames.Contains(Name.Name);

    private static readonly HashSet<string> BasicTribeNames = new(StringComparer.Ordinal)
    {
        "PC", "PC_DARK", "PC_DRAGON", "GENERAL", "GENERAL_DARK", "GENERAL_DRAGON",
        "GUARD", "GUARD_DARK", "GUARD_DRAGON", "MONSTER", "NPC", "USEALL",
        "FIELD_OBJECT_DARK", "FIELD_OBJECT_LIGHT", "NONE",
    };
}
