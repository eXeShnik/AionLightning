namespace AionLightning.Game.World.Geo.Math;

/// <summary>
/// Port of the Java geoEngine <c>Plane</c> (JME-derived): Normal . (x,y,z) = Constant.
/// Only the triangle-vs-box separating-axis test (<see cref="Bounding.Intersection"/>) needs it.
/// </summary>
internal sealed class Plane
{
    public enum Side
    {
        None,
        Positive,
        Negative
    }

    public Vector3f Normal = new();
    public float Constant;

    public void SetPlanePoints(Vector3f v1, Vector3f v2, Vector3f v3)
    {
        Normal.Set(v2).SubtractLocal(v1);
        Normal.CrossLocal(v3.X - v1.X, v3.Y - v1.Y, v3.Z - v1.Z).NormalizeLocal();
        Constant = Normal.Dot(v1);
    }

    public float PseudoDistance(Vector3f point) => Normal.Dot(point) - Constant;
}
