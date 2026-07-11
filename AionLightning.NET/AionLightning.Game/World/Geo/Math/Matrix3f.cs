namespace AionLightning.Game.World.Geo.Math;

/// <summary>
/// Port of the Java geoEngine <c>Matrix3f</c> (JME-derived). Only the members the geo engine
/// actually exercises are ported: row-major bulk set (instance transform loading from
/// {worldId}.geo), absolute-value + vector multiply (bounding-box transform), and the raw
/// m00..m22 fields (read directly by <see cref="Matrix4f"/> when composing a world matrix).
/// </summary>
internal sealed class Matrix3f
{
    public float M00, M01, M02;
    public float M10, M11, M12;
    public float M20, M21, M22;

    public Matrix3f() => LoadIdentity();

    public void LoadIdentity()
    {
        M01 = M02 = M10 = M12 = M20 = M21 = 0;
        M00 = M11 = M22 = 1;
    }

    /// <summary>Sets the matrix from a 9-float row-major array (m00,m01,m02,m10,...).</summary>
    public Matrix3f Set(float[] matrix)
    {
        if (matrix.Length != 9)
            throw new ArgumentException("Array must be of size 9.", nameof(matrix));

        M00 = matrix[0];
        M01 = matrix[1];
        M02 = matrix[2];
        M10 = matrix[3];
        M11 = matrix[4];
        M12 = matrix[5];
        M20 = matrix[6];
        M21 = matrix[7];
        M22 = matrix[8];
        return this;
    }

    public void AbsoluteLocal()
    {
        M00 = FastMath.Abs(M00);
        M01 = FastMath.Abs(M01);
        M02 = FastMath.Abs(M02);
        M10 = FastMath.Abs(M10);
        M11 = FastMath.Abs(M11);
        M12 = FastMath.Abs(M12);
        M20 = FastMath.Abs(M20);
        M21 = FastMath.Abs(M21);
        M22 = FastMath.Abs(M22);
    }

    public Vector3f Mult(Vector3f vec, Vector3f? product = null)
    {
        product ??= new Vector3f();
        var x = vec.X;
        var y = vec.Y;
        var z = vec.Z;
        product.X = M00 * x + M01 * y + M02 * z;
        product.Y = M10 * x + M11 * y + M12 * z;
        product.Z = M20 * x + M21 * y + M22 * z;
        return product;
    }
}
