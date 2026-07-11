namespace AionLightning.Game.World.Geo.Collision;

/// <summary>
/// Port of the Java geoEngine <c>CollisionIntention</c> bit flags. Values match the Java
/// original exactly since they are read back from the on-disk mesh collision-flags byte.
/// </summary>
[Flags]
internal enum CollisionIntention : byte
{
    None = 0,
    Physical = 1 << 0,
    Material = 1 << 1,
    Skill = 1 << 2,
    Walk = 1 << 3,
    Door = 1 << 4,
    Event = 1 << 5,
    Moveable = 1 << 6,
    All = Physical | Material | Skill | Walk | Door | Event | Moveable
}
