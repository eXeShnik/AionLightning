using AionLightning.Game.World.Geo.Math;
using AionLightning.Game.World.Geo.Scene;

namespace AionLightning.Game.World.Geo.Collision;

/// <summary>Port of the Java geoEngine <c>CollisionResult</c>.</summary>
internal sealed class CollisionResult(Vector3f contactPoint, float distance) : IComparable<CollisionResult>
{
    public Vector3f ContactPoint { get; set; } = contactPoint;
    public Vector3f? ContactNormal { get; set; }
    public float Distance { get; set; } = distance;
    public Geometry? Geometry { get; set; }

    public int CompareTo(CollisionResult? other)
    {
        if (other is null) return 1;
        if (Distance < other.Distance) return -1;
        if (Distance > other.Distance) return 1;
        return 0;
    }
}
