using AionLightning.Game.World.Geo.Bounding;
using AionLightning.Game.World.Geo.Collision;
using AionLightning.Game.World.Geo.Math;

namespace AionLightning.Game.World.Geo.Scene;

/// <summary>
/// Port of the Java geoEngine <c>Spatial</c> — base of the geo scene graph (<see cref="Node"/>,
/// <see cref="Geometry"/>). Rendering-only members (CullHint, GL bookkeeping) are not ported;
/// this is a headless collision/height scene graph only.
/// </summary>
internal abstract class Spatial(string? name) : ICollidable
{
    public BoundingVolume? WorldBound { get; protected set; }

    public string? Name { get; set; } = name;

    public Node? Parent { get; internal set; }

    /// <summary>
    /// Signed to match Java's <c>byte getMaterialId()</c> exactly — collision-filtering code
    /// compares it against <c>&lt;= 0</c> to mean "no material", which only works correctly if
    /// the high bit is treated as a sign bit rather than as an unsigned 128..255 value.
    /// </summary>
    public sbyte MaterialId => (sbyte)(CollisionFlags & 0xFF);

    public byte Intentions => (byte)(CollisionFlags >> 8);

    public abstract short CollisionFlags { get; set; }

    public abstract void UpdateModelBound();

    public abstract void SetModelBound(BoundingVolume? modelBound);

    public abstract void SetTransform(Matrix3f rotation, Vector3f loc, float scale);

    public abstract Spatial DeepClone();

    public abstract int CollideWith(ICollidable other, CollisionResults results);
}
