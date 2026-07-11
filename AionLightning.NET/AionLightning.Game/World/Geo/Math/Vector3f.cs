namespace AionLightning.Game.World.Geo.Math;

/// <summary>
/// Port of the Java geoEngine <c>Vector3f</c> (JME-derived). Mutable by design — the BIH/ray
/// code relies on in-place "Local" mutation to avoid allocating per query, matching the Java
/// original. The Java object-factory/recycle pool (<c>GEO_OBJECT_FACTORY_ENABLE</c>) is not
/// ported — the .NET GC handles these short-lived allocations without a manual pool.
/// </summary>
internal sealed class Vector3f
{
    public static readonly Vector3f Zero = new(0, 0, 0);
    public static readonly Vector3f UnitX = new(1, 0, 0);
    public static readonly Vector3f UnitY = new(0, 1, 0);
    public static readonly Vector3f UnitZ = new(0, 0, 1);

    public float X;
    public float Y;
    public float Z;

    public Vector3f()
    {
    }

    public Vector3f(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3f(Vector3f copy) : this(copy.X, copy.Y, copy.Z)
    {
    }

    public Vector3f Set(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
        return this;
    }

    public Vector3f Set(Vector3f other) => Set(other.X, other.Y, other.Z);

    public Vector3f Add(Vector3f other) => new(X + other.X, Y + other.Y, Z + other.Z);

    public Vector3f Add(float addX, float addY, float addZ) => new(X + addX, Y + addY, Z + addZ);

    public Vector3f Add(Vector3f other, Vector3f result)
    {
        result.X = X + other.X;
        result.Y = Y + other.Y;
        result.Z = Z + other.Z;
        return result;
    }

    public Vector3f AddLocal(Vector3f other)
    {
        X += other.X;
        Y += other.Y;
        Z += other.Z;
        return this;
    }

    public Vector3f AddLocal(float addX, float addY, float addZ)
    {
        X += addX;
        Y += addY;
        Z += addZ;
        return this;
    }

    public Vector3f Subtract(Vector3f other) => new(X - other.X, Y - other.Y, Z - other.Z);

    public Vector3f Subtract(Vector3f other, Vector3f result)
    {
        result.X = X - other.X;
        result.Y = Y - other.Y;
        result.Z = Z - other.Z;
        return result;
    }

    public Vector3f Subtract(float subX, float subY, float subZ) => new(X - subX, Y - subY, Z - subZ);

    public Vector3f SubtractLocal(Vector3f other)
    {
        X -= other.X;
        Y -= other.Y;
        Z -= other.Z;
        return this;
    }

    public Vector3f SubtractLocal(float subX, float subY, float subZ)
    {
        X -= subX;
        Y -= subY;
        Z -= subZ;
        return this;
    }

    public float Dot(Vector3f other) => X * other.X + Y * other.Y + Z * other.Z;

    public Vector3f Cross(Vector3f other, Vector3f? result = null) => Cross(other.X, other.Y, other.Z, result);

    public Vector3f Cross(float otherX, float otherY, float otherZ, Vector3f? result = null)
    {
        result ??= new Vector3f();
        var resX = (Y * otherZ) - (Z * otherY);
        var resY = (Z * otherX) - (X * otherZ);
        var resZ = (X * otherY) - (Y * otherX);
        result.Set(resX, resY, resZ);
        return result;
    }

    public Vector3f CrossLocal(Vector3f other) => CrossLocal(other.X, other.Y, other.Z);

    public Vector3f CrossLocal(float otherX, float otherY, float otherZ)
    {
        var tempX = (Y * otherZ) - (Z * otherY);
        var tempY = (Z * otherX) - (X * otherZ);
        Z = (X * otherY) - (Y * otherX);
        X = tempX;
        Y = tempY;
        return this;
    }

    public float Length() => FastMath.Sqrt(LengthSquared());

    public float LengthSquared() => X * X + Y * Y + Z * Z;

    public float DistanceSquared(Vector3f other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        double dz = Z - other.Z;
        return (float)(dx * dx + dy * dy + dz * dz);
    }

    public float Distance(Vector3f other) => FastMath.Sqrt(DistanceSquared(other));

    public Vector3f Mult(float scalar) => new(X * scalar, Y * scalar, Z * scalar);

    public Vector3f Mult(float scalar, Vector3f? product)
    {
        product ??= new Vector3f();
        product.X = X * scalar;
        product.Y = Y * scalar;
        product.Z = Z * scalar;
        return product;
    }

    public Vector3f MultLocal(float scalar)
    {
        X *= scalar;
        Y *= scalar;
        Z *= scalar;
        return this;
    }

    public Vector3f Divide(float scalar)
    {
        var inv = 1f / scalar;
        return new Vector3f(X * inv, Y * inv, Z * inv);
    }

    public Vector3f DivideLocal(float scalar)
    {
        var inv = 1f / scalar;
        X *= inv;
        Y *= inv;
        Z *= inv;
        return this;
    }

    public Vector3f Negate() => new(-X, -Y, -Z);

    public Vector3f NegateLocal()
    {
        X = -X;
        Y = -Y;
        Z = -Z;
        return this;
    }

    public Vector3f Normalize()
    {
        var lengthSq = X * X + Y * Y + Z * Z;
        if (lengthSq is not 1f and not 0f)
        {
            var inv = 1.0f / FastMath.Sqrt(lengthSq);
            return new Vector3f(X * inv, Y * inv, Z * inv);
        }

        return new Vector3f(this);
    }

    public Vector3f NormalizeLocal()
    {
        var lengthSq = X * X + Y * Y + Z * Z;
        if (lengthSq is not 1f and not 0f)
        {
            var inv = 1.0f / FastMath.Sqrt(lengthSq);
            X *= inv;
            Y *= inv;
            Z *= inv;
        }

        return this;
    }

    public float Get(int index) => index switch
    {
        0 => X,
        1 => Y,
        2 => Z,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    public void Set(int index, float value)
    {
        switch (index)
        {
            case 0: X = value; return;
            case 1: Y = value; return;
            case 2: Z = value; return;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public bool ValueEquals(Vector3f other) => X == other.X && Y == other.Y && Z == other.Z;

    public Vector3f Clone() => new(X, Y, Z);

    public override string ToString() => $"({X}, {Y}, {Z})";
}
