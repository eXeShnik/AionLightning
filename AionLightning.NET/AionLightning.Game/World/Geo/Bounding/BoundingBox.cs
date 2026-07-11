using AionLightning.Game.World.Geo.Collision;
using AionLightning.Game.World.Geo.Math;

namespace AionLightning.Game.World.Geo.Bounding;

/// <summary>
/// Port of the Java geoEngine <c>BoundingBox</c> (JME-derived) — an axis-aligned box, the only
/// bounding-volume kind actually used by the mesh/geometry pipeline (see
/// <see cref="BoundingVolume"/> remarks). <c>FloatBuffer</c> point lists become flat
/// <c>float[]</c> triplets since there is no NIO/GL layer to share buffers with.
/// </summary>
internal sealed class BoundingBox : BoundingVolume
{
    public float XExtent;
    public float YExtent;
    public float ZExtent;

    public BoundingBox()
    {
    }

    public BoundingBox(Vector3f center, float x, float y, float z) : base(center)
    {
        XExtent = x;
        YExtent = y;
        ZExtent = z;
    }

    public BoundingBox(BoundingBox source) : base(new Vector3f(source.Center))
    {
        XExtent = source.XExtent;
        YExtent = source.YExtent;
        ZExtent = source.ZExtent;
    }

    public BoundingBox(Vector3f min, Vector3f max)
    {
        SetMinMax(min, max);
    }

    /// <summary>Computes a min-volume AABB from a flat x,y,z,x,y,z... triplet array.</summary>
    public override void ComputeFromPoints(float[] points)
    {
        if (points.Length < 3)
            return;

        float minX = points[0], minY = points[1], minZ = points[2];
        float maxX = minX, maxY = minY, maxZ = minZ;

        for (var i = 3; i + 2 < points.Length; i += 3)
        {
            var x = points[i];
            var y = points[i + 1];
            var z = points[i + 2];
            if (x < minX) minX = x; else if (x > maxX) maxX = x;
            if (y < minY) minY = y; else if (y > maxY) maxY = y;
            if (z < minZ) minZ = z; else if (z > maxZ) maxZ = z;
        }

        Center.Set(minX + maxX, minY + maxY, minZ + maxZ).MultLocal(0.5f);
        XExtent = maxX - Center.X;
        YExtent = maxY - Center.Y;
        ZExtent = maxZ - Center.Z;
    }

    public static void CheckMinMax(Vector3f min, Vector3f max, Vector3f point)
    {
        if (point.X < min.X) min.X = point.X;
        if (point.X > max.X) max.X = point.X;
        if (point.Y < min.Y) min.Y = point.Y;
        if (point.Y > max.Y) max.Y = point.Y;
        if (point.Z < min.Z) min.Z = point.Z;
        if (point.Z > max.Z) max.Z = point.Z;
    }

    public override BoundingVolume Transform(Matrix4f trans, BoundingVolume? store)
    {
        var box = store as BoundingBox ?? new BoundingBox();

        var w = trans.MultProj(Center, box.Center);
        box.Center.DivideLocal(w);

        var transMatrix = new Matrix3f();
        trans.ToRotationMatrix(transMatrix);
        transMatrix.AbsoluteLocal();

        var extent = new Vector3f(XExtent, YExtent, ZExtent);
        transMatrix.Mult(extent, extent);

        box.XExtent = FastMath.Abs(extent.X);
        box.YExtent = FastMath.Abs(extent.Y);
        box.ZExtent = FastMath.Abs(extent.Z);
        return box;
    }

    public override Plane.Side WhichSide(Plane plane)
    {
        var radius = FastMath.Abs(XExtent * plane.Normal.X) + FastMath.Abs(YExtent * plane.Normal.Y) + FastMath.Abs(ZExtent * plane.Normal.Z);
        var distance = plane.PseudoDistance(Center);

        if (distance < -radius) return Plane.Side.Negative;
        if (distance > radius) return Plane.Side.Positive;
        return Plane.Side.None;
    }

    public override BoundingVolume? Merge(BoundingVolume volume)
    {
        if (volume is not BoundingBox vBox)
            return null;

        // Faithful port note: Java's public merge() computes the merged bounds into THIS box
        // (self-mutation) but returns an unrelated freshly-allocated empty box — an apparent bug
        // in the original. It is never actually called (Node.UpdateModelBound only ever calls
        // MergeLocal), so the quirk is preserved rather than "fixed" to avoid changing behavior
        // on a path nothing exercises.
        return MergeInto(vBox.Center, vBox.XExtent, vBox.YExtent, vBox.ZExtent, new BoundingBox(new Vector3f(0, 0, 0), 0, 0, 0));
    }

    public override BoundingVolume? MergeLocal(BoundingVolume volume)
    {
        if (volume is not BoundingBox vBox)
            return null;

        return MergeInto(vBox.Center, vBox.XExtent, vBox.YExtent, vBox.ZExtent, this);
    }

    private BoundingBox MergeInto(Vector3f boxCenter, float boxX, float boxY, float boxZ, BoundingBox rVal)
    {
        var min = new Vector3f
        {
            X = Center.X - XExtent > boxCenter.X - boxX ? boxCenter.X - boxX : Center.X - XExtent,
            Y = Center.Y - YExtent > boxCenter.Y - boxY ? boxCenter.Y - boxY : Center.Y - YExtent,
            Z = Center.Z - ZExtent > boxCenter.Z - boxZ ? boxCenter.Z - boxZ : Center.Z - ZExtent
        };
        var max = new Vector3f
        {
            X = Center.X + XExtent < boxCenter.X + boxX ? boxCenter.X + boxX : Center.X + XExtent,
            Y = Center.Y + YExtent < boxCenter.Y + boxY ? boxCenter.Y + boxY : Center.Y + YExtent,
            Z = Center.Z + ZExtent < boxCenter.Z + boxZ ? boxCenter.Z + boxZ : Center.Z + ZExtent
        };

        Center.Set(max).AddLocal(min).MultLocal(0.5f);
        XExtent = max.X - Center.X;
        YExtent = max.Y - Center.Y;
        ZExtent = max.Z - Center.Z;
        return rVal;
    }

    public override BoundingVolume Clone(BoundingVolume? store)
    {
        if (store is BoundingBox rVal)
        {
            rVal.Center.Set(Center);
            rVal.XExtent = XExtent;
            rVal.YExtent = YExtent;
            rVal.ZExtent = ZExtent;
            return rVal;
        }

        return new BoundingBox(Center.Clone(), XExtent, YExtent, ZExtent);
    }

    public override bool Intersects(BoundingVolume other) => other.IntersectsBoundingBox(this);

    public override bool IntersectsBoundingBox(BoundingBox bb)
    {
        if (Center.X + XExtent < bb.Center.X - bb.XExtent || Center.X - XExtent > bb.Center.X + bb.XExtent) return false;
        if (Center.Y + YExtent < bb.Center.Y - bb.YExtent || Center.Y - YExtent > bb.Center.Y + bb.YExtent) return false;
        if (Center.Z + ZExtent < bb.Center.Z - bb.ZExtent || Center.Z - ZExtent > bb.Center.Z + bb.ZExtent) return false;
        return true;
    }

    public override bool Contains(Vector3f point) =>
        FastMath.Abs(Center.X - point.X) < XExtent && FastMath.Abs(Center.Y - point.Y) < YExtent && FastMath.Abs(Center.Z - point.Z) < ZExtent;

    public override bool Intersects(Vector3f point) =>
        FastMath.Abs(Center.X - point.X) <= XExtent && FastMath.Abs(Center.Y - point.Y) <= YExtent && FastMath.Abs(Center.Z - point.Z) <= ZExtent;

    /// <summary>Triangle-vs-box separating-axis test — see <see cref="Intersection"/>.</summary>
    public bool Intersects(Vector3f v1, Vector3f v2, Vector3f v3) => Intersection.Intersect(this, v1, v2, v3);

    public override bool Intersects(Ray ray)
    {
        var diff = ray.Origin.Subtract(GetCenter(new Vector3f()));

        var wdUx = ray.Direction.Dot(Vector3f.UnitX);
        var aWdUx = FastMath.Abs(wdUx);
        var dDUx = diff.Dot(Vector3f.UnitX);
        if (FastMath.Abs(dDUx) > XExtent && dDUx * wdUx >= 0.0) return false;

        var wdUy = ray.Direction.Dot(Vector3f.UnitY);
        var aWdUy = FastMath.Abs(wdUy);
        var dDUy = diff.Dot(Vector3f.UnitY);
        if (FastMath.Abs(dDUy) > YExtent && dDUy * wdUy >= 0.0) return false;

        var wdUz = ray.Direction.Dot(Vector3f.UnitZ);
        var aWdUz = FastMath.Abs(wdUz);
        var dDUz = diff.Dot(Vector3f.UnitZ);
        if (FastMath.Abs(dDUz) > ZExtent && dDUz * wdUz >= 0.0) return false;

        var wCrossD = ray.Direction.Cross(diff);

        var aWxDdUx = FastMath.Abs(wCrossD.Dot(Vector3f.UnitX));
        if (aWxDdUx > YExtent * aWdUz + ZExtent * aWdUy) return false;

        var aWxDdUy = FastMath.Abs(wCrossD.Dot(Vector3f.UnitY));
        if (aWxDdUy > XExtent * aWdUz + ZExtent * aWdUx) return false;

        var aWxDdUz = FastMath.Abs(wCrossD.Dot(Vector3f.UnitZ));
        if (aWxDdUz > XExtent * aWdUy + YExtent * aWdUx) return false;

        return true;
    }

    private int CollideWithRay(Ray ray, CollisionResults results)
    {
        var diff = ray.Origin.Subtract(Center);
        var direction = new Vector3f(ray.Direction);

        var t = new[] { 0f, float.PositiveInfinity };
        var saveT0 = t[0];
        var saveT1 = t[1];

        var notEntirelyClipped =
            Clip(+direction.X, -diff.X - XExtent, t) && Clip(-direction.X, +diff.X - XExtent, t) &&
            Clip(+direction.Y, -diff.Y - YExtent, t) && Clip(-direction.Y, +diff.Y - YExtent, t) &&
            Clip(+direction.Z, -diff.Z - ZExtent, t) && Clip(-direction.Z, +diff.Z - ZExtent, t);

        if (notEntirelyClipped && (t[0] != saveT0 || t[1] != saveT1))
        {
            if (t[1] > t[0])
            {
                var p0 = new Vector3f(ray.Direction).MultLocal(t[0]).AddLocal(ray.Origin);
                var p1 = new Vector3f(ray.Direction).MultLocal(t[1]).AddLocal(ray.Origin);
                results.AddCollision(new CollisionResult(p0, t[0]));
                results.AddCollision(new CollisionResult(p1, t[1]));
                return 2;
            }

            var point = new Vector3f(ray.Direction).MultLocal(t[0]).AddLocal(ray.Origin);
            results.AddCollision(new CollisionResult(point, t[0]));
            return 1;
        }

        return 0;
    }

    private static bool Clip(float denom, float numer, float[] t)
    {
        if (denom > 0.0f)
        {
            if (numer > denom * t[1]) return false;
            if (numer > denom * t[0]) t[0] = numer / denom;
            return true;
        }

        if (denom < 0.0f)
        {
            if (numer > denom * t[0]) return false;
            if (numer > denom * t[1]) t[1] = numer / denom;
            return true;
        }

        return numer <= 0.0;
    }

    public override int CollideWith(ICollidable other, CollisionResults results) => other switch
    {
        Ray ray => CollideWithRay(ray, results),
        _ => throw new UnsupportedCollisionException($"With: {other.GetType().Name}")
    };

    public Vector3f GetExtent(Vector3f? store = null)
    {
        store ??= new Vector3f();
        store.Set(XExtent, YExtent, ZExtent);
        return store;
    }

    public Vector3f GetMin(Vector3f? store = null)
    {
        store ??= new Vector3f();
        store.Set(Center).SubtractLocal(XExtent, YExtent, ZExtent);
        return store;
    }

    public Vector3f GetMax(Vector3f? store = null)
    {
        store ??= new Vector3f();
        store.Set(Center).AddLocal(XExtent, YExtent, ZExtent);
        return store;
    }

    public void SetMinMax(Vector3f min, Vector3f max)
    {
        Center.Set(max).AddLocal(min).MultLocal(0.5f);
        XExtent = FastMath.Abs(max.X - Center.X);
        YExtent = FastMath.Abs(max.Y - Center.Y);
        ZExtent = FastMath.Abs(max.Z - Center.Z);
    }
}
