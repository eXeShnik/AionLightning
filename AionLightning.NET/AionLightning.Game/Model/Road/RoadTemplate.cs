namespace AionLightning.Game.Model.Road;

/// <summary>Java model.templates.road.RoadPoint — a bare 3D point, used for both the plane-defining
/// center/p1/p2 triple and (flattened, see <see cref="RoadTemplate"/>) the exit destination.</summary>
public readonly record struct RoadPoint(float X, float Y, float Z);

/// <summary>
/// Java model.templates.road.RoadTemplate (+ RoadExit) — one static "road": an invisible plane-crossing
/// shortcut between two adjacent open-world zones (e.g. Verteron&lt;-&gt;Eltnen, Morheim&lt;-&gt;Altgard&lt;-&gt;
/// Beluslan, and the contested Sarpan/Tiamaranta/Katalam/Danaria border crossings), flattened out of the
/// nested data/static_data/roads/roads.xml structure (road/center|p1|p2|roadexit) by
/// <see cref="AionLightning.Game.DataHolders.RoadData"/>, the same way
/// <see cref="AionLightning.Game.DataHolders.SiegeSpawnData"/> flattens siege spawn XML. Unlike Java's
/// model.road.Road (a spawned VisibleObject carrying a Plane3D and a knownlist), this port has no
/// geo-plane-intersection/knownlist machinery to spawn it against (see
/// <see cref="AionLightning.Game.Services.RoadService"/>'s own doc comment), so the template is kept as
/// plain data plus a stable <see cref="Id"/> for <see cref="AionLightning.Game.Services.RoadService"/> to
/// look up by.
/// </summary>
public sealed class RoadTemplate
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required int WorldId { get; init; }
    public required float Radius { get; init; }
    public required RoadPoint Center { get; init; }
    public required RoadPoint P1 { get; init; }
    public required RoadPoint P2 { get; init; }
    public required int ExitWorldId { get; init; }
    public required RoadPoint ExitPoint { get; init; }
}
