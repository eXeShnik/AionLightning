namespace AionLightning.Game.Model.Templates.Tribe;

/// <summary>
/// Faithful, data-driven port of Java <c>model.TribeClass</c> — an interned, case-insensitive tribe
/// identifier as declared in <c>tribe_relations.xml</c> and referenced by <c>npc_templates.xml</c>'s
/// <c>tribe=""</c> attribute. The Java original is a ~600-member enum whose members only carry real
/// behavior for a handful of "base tribe" special cases (see <see cref="Services.TribeRelationService"/>);
/// every other member is pure XML-authored data with no C# behavior attached. Porting all ~600 names as
/// enum literals would only duplicate the XML as compile-time constants (with real risk of transcription
/// typos silently breaking aggro), so this wrapper keeps just the well-known tribes the service switches
/// on by name as static fields and treats every other tribe as data loaded by
/// <see cref="DataHolders.TribeRelationsData"/> — consistent with how <c>NpcTemplate.Tribe</c> was
/// already represented as a plain string before this port.
/// </summary>
public readonly struct TribeClass : IEquatable<TribeClass>
{
    public static readonly TribeClass None            = new("NONE");
    public static readonly TribeClass Pc               = new("PC");
    public static readonly TribeClass PcDark           = new("PC_DARK");
    public static readonly TribeClass PcDragon         = new("PC_DRAGON");
    public static readonly TribeClass Npc              = new("NPC");
    public static readonly TribeClass General          = new("GENERAL");
    public static readonly TribeClass GeneralDark      = new("GENERAL_DARK");
    public static readonly TribeClass GeneralDragon    = new("GENERAL_DRAGON");
    public static readonly TribeClass Guard            = new("GUARD");
    public static readonly TribeClass GuardDark        = new("GUARD_DARK");
    public static readonly TribeClass GuardDragon      = new("GUARD_DRAGON");
    public static readonly TribeClass Monster          = new("MONSTER");
    public static readonly TribeClass UseAll           = new("USEALL");
    public static readonly TribeClass FieldObjectAll   = new("FIELD_OBJECT_ALL");
    public static readonly TribeClass FieldObjectLight = new("FIELD_OBJECT_LIGHT");
    public static readonly TribeClass FieldObjectDark  = new("FIELD_OBJECT_DARK");

    private readonly string? _name;

    /// <summary>Normalized (trimmed, upper-invariant) tribe name; "NONE" for an unset/default instance.</summary>
    public string Name => _name ?? "NONE";

    public TribeClass(string name) => _name = Normalize(name);

    private static string Normalize(string? name)
        => string.IsNullOrWhiteSpace(name) ? "NONE" : name.Trim().ToUpperInvariant();

    public bool Equals(TribeClass other) => string.Equals(Name, other.Name, StringComparison.Ordinal);
    public override bool Equals(object? obj) => obj is TribeClass other && Equals(other);
    public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => Name;

    public static bool operator ==(TribeClass left, TribeClass right) => left.Equals(right);
    public static bool operator !=(TribeClass left, TribeClass right) => !left.Equals(right);
}
