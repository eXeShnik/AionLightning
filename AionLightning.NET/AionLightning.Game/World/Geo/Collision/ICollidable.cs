namespace AionLightning.Game.World.Geo.Collision;

/// <summary>Port of the Java geoEngine <c>Collidable</c> marker interface.</summary>
internal interface ICollidable
{
    /// <summary>Checks collision with another collidable, returning how many collisions were found.</summary>
    int CollideWith(ICollidable other, CollisionResults results);
}
