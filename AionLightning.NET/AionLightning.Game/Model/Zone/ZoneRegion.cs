namespace AionLightning.Game.Model.Zone;

/// <summary>Area shape of a zone region (Java <c>model.templates.zone.AreaType</c>).</summary>
public enum ZoneAreaType
{
    Polygon,
    Cylinder,
    Sphere,
}

/// <summary>
/// A named zone region loaded from <c>data/static_data/zones/zones_*.xml</c> (Java
/// <c>world.zone.ZoneInstance</c> + <c>model.geometry.Area</c> port, collapsed into one flat model
/// since this port has no geo-mesh/handler-class infra — only the area-membership test needed by
/// <see cref="Services.ZoneService"/> and the quest onEnterZone hook).
/// </summary>
public sealed class ZoneRegion
{
    public required string Name { get; init; }
    public required int WorldId { get; init; }
    public required ZoneAreaType AreaType { get; init; }

    /// <summary>Inclusive z bounds (Java <c>AbstractArea.isInsideZ</c>: z &gt;= minZ &amp;&amp; z &lt;= maxZ). Unused for <see cref="ZoneAreaType.Sphere"/>, which tests a true 3D radius instead.</summary>
    public float MinZ { get; init; }
    public float MaxZ { get; init; }

    // Polygon (x/y pairs, same index = one vertex)
    public float[] PolyX { get; init; } = [];
    public float[] PolyY { get; init; } = [];

    // Cylinder / Sphere center + radius
    public float CenterX { get; init; }
    public float CenterY { get; init; }
    public float CenterZ { get; init; }
    public float Radius { get; init; }

    /// <summary>
    /// Java <c>ZoneInstance.isInsideCordinate</c> / <c>Area.isInside3D</c> port. Polygon and
    /// cylinder are z-bounded 2D-membership tests (Java <c>AbstractArea.isInside3D</c>: isInsideZ
    /// &amp;&amp; isInside2D); sphere is a true 3D radius test (Java <c>SphereArea.isInside3D</c>
    /// via <c>MathUtil.isIn3dRange</c>, which does not consult minZ/maxZ at all).
    /// </summary>
    public bool IsInside(float x, float y, float z) => AreaType switch
    {
        ZoneAreaType.Polygon  => z >= MinZ && z <= MaxZ && IsInsidePolygon(x, y),
        ZoneAreaType.Cylinder => z >= MinZ && z <= MaxZ && DistanceSquared2D(x, y) < Radius * Radius,
        ZoneAreaType.Sphere   => DistanceSquared3D(x, y, z) < Radius * Radius,
        _ => false,
    };

    /// <summary>
    /// Ray-casting (even-odd) point-in-polygon test. Java builds a <c>java.awt.geom.GeneralPath</c>
    /// from the same vertex list and calls its <c>contains(x, y)</c>; for the simple (non-self
    /// -intersecting) polygons zones.xml describes, the even-odd and non-zero winding rules agree,
    /// so this crossing-number test is behaviorally equivalent.
    /// </summary>
    private bool IsInsidePolygon(float x, float y)
    {
        bool inside = false;
        int n = PolyX.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            float yi = PolyY[i], yj = PolyY[j];
            if ((yi > y) != (yj > y) &&
                x < (PolyX[j] - PolyX[i]) * (y - yi) / (yj - yi) + PolyX[i])
            {
                inside = !inside;
            }
        }
        return inside;
    }

    private float DistanceSquared2D(float x, float y)
    {
        float dx = x - CenterX, dy = y - CenterY;
        return dx * dx + dy * dy;
    }

    private float DistanceSquared3D(float x, float y, float z)
    {
        float dx = x - CenterX, dy = y - CenterY, dz = z - CenterZ;
        return dx * dx + dy * dy + dz * dz;
    }
}
