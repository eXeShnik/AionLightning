using AionLightning.Game.World.Geo.Math;

namespace AionLightning.Game.World.Geo.Bounding;

/// <summary>
/// Port of the Java geoEngine <c>Intersection</c> — triangle-vs-AABB separating-axis test
/// (Akenine-Möller). Not currently reached by any GeoMap query path (GeoMap only ever collides
/// rays, never raw triangles against a box), but kept as a faithful, isolated port per the
/// bounding-volume port scope; a future material-zone/shape consumer can call
/// <see cref="BoundingBox.Intersects(Vector3f, Vector3f, Vector3f)"/> directly.
/// </summary>
internal static class Intersection
{
    public static bool Intersect(BoundingBox bbox, Vector3f v1, Vector3f v2, Vector3f v3)
    {
        var center = bbox.Center;
        var extent = bbox.GetExtent();

        var tmp0 = v1.Subtract(center);
        var tmp1 = v2.Subtract(center);
        var tmp2 = v3.Subtract(center);

        var e0 = tmp1.Subtract(tmp0);
        var e1 = tmp2.Subtract(tmp1);
        var e2 = tmp0.Subtract(tmp2);

        float min, max, p0, p1, p2, rad;
        var fex = FastMath.Abs(e0.X);
        var fey = FastMath.Abs(e0.Y);
        var fez = FastMath.Abs(e0.Z);

        p0 = e0.Z * tmp0.Y - e0.Y * tmp0.Z;
        p2 = e0.Z * tmp2.Y - e0.Y * tmp2.Z;
        min = System.Math.Min(p0, p2);
        max = System.Math.Max(p0, p2);
        rad = fez * extent.Y + fey * extent.Z;
        if (min > rad || max < -rad) return false;

        p0 = -e0.Z * tmp0.X + e0.X * tmp0.Z;
        p2 = -e0.Z * tmp2.X + e0.X * tmp2.Z;
        min = System.Math.Min(p0, p2);
        max = System.Math.Max(p0, p2);
        rad = fez * extent.X + fex * extent.Z;
        if (min > rad || max < -rad) return false;

        p1 = e0.Y * tmp1.X - e0.X * tmp1.Y;
        p2 = e0.Y * tmp2.X - e0.X * tmp2.Y;
        min = System.Math.Min(p1, p2);
        max = System.Math.Max(p1, p2);
        rad = fey * extent.X + fex * extent.Y;
        if (min > rad || max < -rad) return false;

        fex = FastMath.Abs(e1.X);
        fey = FastMath.Abs(e1.Y);
        fez = FastMath.Abs(e1.Z);

        p0 = e1.Z * tmp0.Y - e1.Y * tmp0.Z;
        p2 = e1.Z * tmp2.Y - e1.Y * tmp2.Z;
        min = System.Math.Min(p0, p2);
        max = System.Math.Max(p0, p2);
        rad = fez * extent.Y + fey * extent.Z;
        if (min > rad || max < -rad) return false;

        p0 = -e1.Z * tmp0.X + e1.X * tmp0.Z;
        p2 = -e1.Z * tmp2.X + e1.X * tmp2.Z;
        min = System.Math.Min(p0, p2);
        max = System.Math.Max(p0, p2);
        rad = fez * extent.X + fex * extent.Z;
        if (min > rad || max < -rad) return false;

        p0 = e1.Y * tmp0.X - e1.X * tmp0.Y;
        p1 = e1.Y * tmp1.X - e1.X * tmp1.Y;
        min = System.Math.Min(p0, p1);
        max = System.Math.Max(p0, p1);
        rad = fey * extent.X + fex * extent.Y;
        if (min > rad || max < -rad) return false;

        fex = FastMath.Abs(e2.X);
        fey = FastMath.Abs(e2.Y);
        fez = FastMath.Abs(e2.Z);

        p0 = e2.Z * tmp0.Y - e2.Y * tmp0.Z;
        p1 = e2.Z * tmp1.Y - e2.Y * tmp1.Z;
        min = System.Math.Min(p0, p1);
        max = System.Math.Max(p0, p1);
        rad = fez * extent.Y + fey * extent.Z;
        if (min > rad || max < -rad) return false;

        p0 = -e2.Z * tmp0.X + e2.X * tmp0.Z;
        p1 = -e2.Z * tmp1.X + e2.X * tmp1.Z;
        min = System.Math.Min(p0, p1);
        max = System.Math.Max(p0, p1);
        rad = fez * extent.X + fex * extent.Y;
        if (min > rad || max < -rad) return false;

        p1 = e2.Y * tmp1.X - e2.X * tmp1.Y;
        p2 = e2.Y * tmp2.X - e2.X * tmp2.Y;
        min = System.Math.Min(p1, p2);
        max = System.Math.Max(p1, p2);
        rad = fey * extent.X + fex * extent.Y;
        if (min > rad || max < -rad) return false;

        // Bullet 1: {x,y,z}-direction min/max overlap (AABB-of-triangle vs box)
        if (FindMin(tmp0.X, tmp1.X, tmp2.X) > extent.X || FindMax(tmp0.X, tmp1.X, tmp2.X) < -extent.X) return false;
        if (FindMin(tmp0.Y, tmp1.Y, tmp2.Y) > extent.Y || FindMax(tmp0.Y, tmp1.Y, tmp2.Y) < -extent.Y) return false;
        if (FindMin(tmp0.Z, tmp1.Z, tmp2.Z) > extent.Z || FindMax(tmp0.Z, tmp1.Z, tmp2.Z) < -extent.Z) return false;

        // Bullet 2: box intersects the triangle's plane
        var p = new Plane();
        p.SetPlanePoints(v1, v2, v3);
        if (bbox.WhichSide(p) == Plane.Side.Negative) return false;

        return true;
    }

    private static float FindMin(float a, float b, float c) => System.Math.Min(a, System.Math.Min(b, c));

    private static float FindMax(float a, float b, float c) => System.Math.Max(a, System.Math.Max(b, c));
}
