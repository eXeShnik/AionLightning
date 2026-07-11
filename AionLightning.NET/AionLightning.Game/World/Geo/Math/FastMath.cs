namespace AionLightning.Game.World.Geo.Math;

/// <summary>
/// Port of the Java geoEngine FastMath helper (JME-derived). Only the members actually
/// exercised by the geo engine port are included — trig/interpolation helpers unused by
/// GeoMap/BIHTree/GeoWorldLoader (Catmull-Rom, spherical conversions, half-float, etc.) are
/// intentionally not ported.
/// </summary>
internal static class FastMath
{
    public const float FltEpsilon = 1.1920928955078125E-7f;
    public const float ZeroTolerance = 0.0001f;
    public const float OneThird = 1f / 3f;

    public static float Abs(float value) => value < 0 ? -value : value;

    public static float Sqrt(float value) => System.MathF.Sqrt(value);

    public static float InvSqrt(float value) => 1f / System.MathF.Sqrt(value);

    public static float Acos(float value)
    {
        if (-1.0f < value)
        {
            if (value < 1.0f)
                return System.MathF.Acos(value);
            return 0.0f;
        }

        return System.MathF.PI;
    }

    public static float Sin(float value) => System.MathF.Sin(value);

    public static float Cos(float value) => System.MathF.Cos(value);
}
