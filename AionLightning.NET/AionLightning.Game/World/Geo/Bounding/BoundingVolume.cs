using AionLightning.Game.World.Geo.Collision;
using AionLightning.Game.World.Geo.Math;

namespace AionLightning.Game.World.Geo.Bounding;

/// <summary>
/// Port of the Java geoEngine <c>BoundingVolume</c> abstract base. The Java hierarchy also had
/// <c>BoundingSphere</c> and a commented-out <c>OrientedBoundingBox</c>; neither is ever
/// instantiated anywhere in the mesh/geometry pipeline (a <see cref="Scene.Mesh"/>'s bound
/// always defaults to and stays a <see cref="BoundingBox"/>), so this port only implements the
/// AABB path — see <see cref="BoundingBox"/>. The abstract base is kept (rather than collapsing
/// to a single concrete class) so future bound kinds can slot back in without touching callers.
/// </summary>
internal abstract class BoundingVolume(Vector3f? center = null) : ICollidable
{
    public Vector3f Center { get; set; } = center ?? new Vector3f();

    /// <summary>Recomputes this volume from a flat x,y,z,x,y,z... point array.</summary>
    public abstract void ComputeFromPoints(float[] points);

    public abstract BoundingVolume Transform(Matrix4f trans, BoundingVolume? store);

    public abstract Plane.Side WhichSide(Plane plane);

    public abstract BoundingVolume? Merge(BoundingVolume volume);

    public abstract BoundingVolume? MergeLocal(BoundingVolume volume);

    public abstract BoundingVolume Clone(BoundingVolume? store);

    public Vector3f GetCenter(Vector3f store)
    {
        store.Set(Center);
        return store;
    }

    public float DistanceTo(Vector3f point) => Center.Distance(point);

    public abstract bool Contains(Vector3f point);

    public abstract bool Intersects(Vector3f point);

    public abstract bool Intersects(BoundingVolume other);

    public abstract bool Intersects(Ray ray);

    public abstract bool IntersectsBoundingBox(BoundingBox box);

    public abstract int CollideWith(ICollidable other, CollisionResults results);
}
