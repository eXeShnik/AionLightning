namespace AionLightning.Game.World.Geo.Collision;

/// <summary>Port of the Java geoEngine <c>UnsupportedCollisionException</c>.</summary>
internal sealed class UnsupportedCollisionException : NotSupportedException
{
    public UnsupportedCollisionException()
    {
    }

    public UnsupportedCollisionException(string message) : base(message)
    {
    }
}
