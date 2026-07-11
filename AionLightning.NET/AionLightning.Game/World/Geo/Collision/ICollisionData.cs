using AionLightning.Game.World.Geo.Bounding;
using AionLightning.Game.World.Geo.Math;

namespace AionLightning.Game.World.Geo.Collision;

/// <summary>Port of the Java geoEngine <c>CollisionData</c> — triangle-accurate ray/mesh collision.</summary>
internal interface ICollisionData
{
    int CollideWith(ICollidable other, Matrix4f worldMatrix, BoundingVolume worldBound, CollisionResults results);
}
