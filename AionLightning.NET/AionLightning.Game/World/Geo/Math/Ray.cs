using AionLightning.Game.World.Geo.Collision;

namespace AionLightning.Game.World.Geo.Math;

/// <summary>
/// Port of the Java geoEngine <c>Ray</c> (JME-derived): R(t) = origin + t*direction, t in [0, limit].
/// Only the two Möller–Trumbore-style triangle intersection overloads the geo engine actually
/// calls are ported (the planar/quad variants and the generic <see cref="ICollidable"/> dispatch
/// used elsewhere in jME are not exercised by GeoMap/BIHTree).
/// </summary>
internal sealed class Ray : ICollidable
{
    public Vector3f Origin;
    public Vector3f Direction;
    public float Limit = float.PositiveInfinity;

    public Ray()
    {
        Origin = new Vector3f();
        Direction = new Vector3f();
    }

    public Ray(Vector3f origin, Vector3f direction)
    {
        Origin = origin;
        Direction = direction;
    }

    public void SetOrigin(Vector3f origin) => Origin = origin;

    public void SetDirection(Vector3f direction) => Direction = direction;

    /// <summary>
    /// Intersects the ray against a triangle, writing the world-space hit point into
    /// <paramref name="loc"/> when it collides. Ported verbatim from Java's
    /// <c>intersectWhere(Triangle, Vector3f)</c> → <c>intersects(v0,v1,v2,store,doPlanar=false)</c>.
    /// </summary>
    public bool IntersectWhere(Triangle triangle, Vector3f loc) =>
        IntersectsWithStore(triangle.Get(0), triangle.Get(1), triangle.Get(2), loc);

    private bool IntersectsWithStore(Vector3f v0, Vector3f v1, Vector3f v2, Vector3f store)
    {
        var diff = Origin.Subtract(v0);
        var edge1 = v1.Subtract(v0);
        var edge2 = v2.Subtract(v0);
        var norm = edge1.Cross(edge2);

        var dirDotNorm = Direction.Dot(norm);
        float sign;
        if (dirDotNorm > FastMath.FltEpsilon)
        {
            sign = 1;
        }
        else if (dirDotNorm < -FastMath.FltEpsilon)
        {
            sign = -1f;
            dirDotNorm = -dirDotNorm;
        }
        else
        {
            // ray and triangle are parallel
            return false;
        }

        var dirDotDiffxEdge2 = sign * Direction.Dot(diff.Cross(edge2, edge2));
        if (dirDotDiffxEdge2 < 0.0f)
            return false;

        var dirDotEdge1xDiff = sign * Direction.Dot(edge1.CrossLocal(diff));
        if (dirDotEdge1xDiff < 0.0f)
            return false;

        if (dirDotDiffxEdge2 + dirDotEdge1xDiff > dirDotNorm)
            return false;

        var diffDotNorm = -sign * diff.Dot(norm);
        if (diffDotNorm < 0.0f)
            return false;

        var inv = 1f / dirDotNorm;
        var t = diffDotNorm * inv;
        store.Set(Origin).AddLocal(Direction.X * t, Direction.Y * t, Direction.Z * t);
        return true;
    }

    /// <summary>
    /// Non-allocating distance-only intersection used by the BIH traversal hot path. Returns
    /// the hit distance along the ray, or <see cref="float.PositiveInfinity"/> when it misses.
    /// Ported verbatim from Java's field-math <c>intersects(Vector3f, Vector3f, Vector3f)</c>.
    /// </summary>
    public float Intersects(Vector3f v0, Vector3f v1, Vector3f v2)
    {
        var edge1X = v1.X - v0.X;
        var edge1Y = v1.Y - v0.Y;
        var edge1Z = v1.Z - v0.Z;

        var edge2X = v2.X - v0.X;
        var edge2Y = v2.Y - v0.Y;
        var edge2Z = v2.Z - v0.Z;

        var normX = (edge1Y * edge2Z) - (edge1Z * edge2Y);
        var normY = (edge1Z * edge2X) - (edge1X * edge2Z);
        var normZ = (edge1X * edge2Y) - (edge1Y * edge2X);

        var dirDotNorm = Direction.X * normX + Direction.Y * normY + Direction.Z * normZ;

        var diffX = Origin.X - v0.X;
        var diffY = Origin.Y - v0.Y;
        var diffZ = Origin.Z - v0.Z;

        float sign;
        if (dirDotNorm > FastMath.FltEpsilon)
        {
            sign = 1;
        }
        else if (dirDotNorm < -FastMath.FltEpsilon)
        {
            sign = -1f;
            dirDotNorm = -dirDotNorm;
        }
        else
        {
            return float.PositiveInfinity;
        }

        var diffEdge2X = (diffY * edge2Z) - (diffZ * edge2Y);
        var diffEdge2Y = (diffZ * edge2X) - (diffX * edge2Z);
        var diffEdge2Z = (diffX * edge2Y) - (diffY * edge2X);

        var dirDotDiffxEdge2 = sign * (Direction.X * diffEdge2X + Direction.Y * diffEdge2Y + Direction.Z * diffEdge2Z);
        if (dirDotDiffxEdge2 < 0.0f)
            return float.PositiveInfinity;

        var edge1DiffX = (edge1Y * diffZ) - (edge1Z * diffY);
        var edge1DiffY = (edge1Z * diffX) - (edge1X * diffZ);
        var edge1DiffZ = (edge1X * diffY) - (edge1Y * diffX);

        var dirDotEdge1xDiff = sign * (Direction.X * edge1DiffX + Direction.Y * edge1DiffY + Direction.Z * edge1DiffZ);
        if (dirDotEdge1xDiff < 0.0f)
            return float.PositiveInfinity;

        if (dirDotDiffxEdge2 + dirDotEdge1xDiff > dirDotNorm)
            return float.PositiveInfinity;

        var diffDotNorm = -sign * (diffX * normX + diffY * normY + diffZ * normZ);
        if (diffDotNorm < 0.0f)
            return float.PositiveInfinity;

        var inv = 1f / dirDotNorm;
        return diffDotNorm * inv;
    }

    /// <summary>
    /// Unreachable in the geo engine's own call graph — every query enters via
    /// <c>Node.CollideWith(Ray, ...)</c>, never <c>ray.CollideWith(...)</c>. Kept only so
    /// <see cref="Ray"/> satisfies <see cref="ICollidable"/> where the type is used generically.
    /// </summary>
    public int CollideWith(ICollidable other, CollisionResults results) =>
        throw new UnsupportedCollisionException("Ray-as-subject collision is not used by the geo engine port.");
}
