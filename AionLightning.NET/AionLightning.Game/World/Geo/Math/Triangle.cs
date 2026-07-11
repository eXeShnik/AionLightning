namespace AionLightning.Game.World.Geo.Math;

/// <summary>
/// Port of the Java geoEngine <c>Triangle</c> (JME-derived) — a plain 3-point holder used by
/// <see cref="Ray"/> intersection tests and terrain bilinear sampling. The OBBTree-era
/// center/normal caching and object-factory recycling from the Java original are not needed
/// here (nothing in the geo engine port re-centers or re-normals a pooled Triangle instance).
/// </summary>
internal sealed class Triangle
{
    private readonly Vector3f _pointA = new();
    private readonly Vector3f _pointB = new();
    private readonly Vector3f _pointC = new();

    public Triangle()
    {
    }

    public Triangle(Vector3f p1, Vector3f p2, Vector3f p3)
    {
        _pointA.Set(p1);
        _pointB.Set(p2);
        _pointC.Set(p3);
    }

    public Vector3f Get(int index) => index switch
    {
        0 => _pointA,
        1 => _pointB,
        2 => _pointC,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    public static Vector3f ComputeTriangleNormal(Vector3f v1, Vector3f v2, Vector3f v3, Vector3f? store = null)
    {
        store ??= new Vector3f(v2);
        store.Set(v2);
        store.SubtractLocal(v1).CrossLocal(v3.X - v1.X, v3.Y - v1.Y, v3.Z - v1.Z);
        return store.NormalizeLocal();
    }
}
