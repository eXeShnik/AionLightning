using AionLightning.Game.World.Geo.Bounding;
using AionLightning.Game.World.Geo.Collision;
using AionLightning.Game.World.Geo.Math;

namespace AionLightning.Game.World.Geo.Scene;

/// <summary>Port of the Java geoEngine <c>Geometry</c> — a leaf scene node backed by a <see cref="Mesh"/>.</summary>
internal sealed class Geometry(string? name, Mesh mesh) : Spatial(name)
{
    public Mesh Mesh { get; private set; } = mesh;

    /// <summary>Rotation + uniform scale + translation composed for this instance placement.</summary>
    public Matrix4f WorldMatrix { get; } = new();

    public override short CollisionFlags
    {
        get => Mesh.CollisionFlags;
        set => Mesh.CollisionFlags = value;
    }

    public BoundingVolume GetModelBound() => Mesh.GetBound();

    public override void UpdateModelBound()
    {
        Mesh.UpdateBound();
        WorldBound = GetModelBound().Transform(WorldMatrix, WorldBound);
    }

    public override void SetModelBound(BoundingVolume? modelBound) => Mesh.SetBound(modelBound);

    public override int CollideWith(ICollidable other, CollisionResults results)
    {
        if (other is Ray ray && WorldBound != null && !WorldBound.Intersects(ray))
            return 0;

        var prevSize = results.Size;
        var added = Mesh.CollideWith(other, WorldMatrix, WorldBound!, results);
        for (var i = prevSize; i < results.Size; i++)
            results.GetCollisionDirect(i).Geometry = this;
        return added;
    }

    public override void SetTransform(Matrix3f rotation, Vector3f loc, float scale)
    {
        WorldMatrix.LoadIdentity();
        WorldMatrix.SetRotationMatrix(rotation);
        WorldMatrix.Scale(scale);
        WorldMatrix.SetTranslation(loc);
    }

    /// <summary>
    /// Shares the same <see cref="Mesh"/> (and its lazily-built BIH tree) across every instance
    /// placement — only the transform differs per clone. Matches Java's Node.clone() special
    /// case for Geometry children (<c>new Geometry(name, existingMesh)</c>), just expressed as
    /// this type's own override instead of being special-cased inside Node.
    /// </summary>
    public override Spatial DeepClone() => new Geometry(Name, Mesh);
}
