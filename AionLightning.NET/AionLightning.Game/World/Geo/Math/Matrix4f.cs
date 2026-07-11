namespace AionLightning.Game.World.Geo.Math;

/// <summary>
/// Port of the Java geoEngine <c>Matrix4f</c> (JME-derived). Only the subset used to compose
/// and apply a mesh instance's world transform (rotation + uniform scale + translation) and to
/// invert it for ray-space BIH queries is ported — no projection/frustum/quaternion machinery.
/// </summary>
internal sealed class Matrix4f
{
    private float _m00, _m01, _m02, _m03;
    private float _m10, _m11, _m12, _m13;
    private float _m20, _m21, _m22, _m23;
    private float _m30, _m31, _m32, _m33;

    public Matrix4f() => LoadIdentity();

    public void LoadIdentity()
    {
        _m01 = _m02 = _m03 = 0f;
        _m10 = _m12 = _m13 = 0f;
        _m20 = _m21 = _m23 = 0f;
        _m30 = _m31 = _m32 = 0f;
        _m00 = _m11 = _m22 = _m33 = 1f;
    }

    public void SetRotationMatrix(Matrix3f mat)
    {
        _m00 = mat.M00;
        _m01 = mat.M01;
        _m02 = mat.M02;
        _m10 = mat.M10;
        _m11 = mat.M11;
        _m12 = mat.M12;
        _m20 = mat.M20;
        _m21 = mat.M21;
        _m22 = mat.M22;
    }

    public void ToRotationMatrix(Matrix3f mat)
    {
        mat.M00 = _m00;
        mat.M01 = _m01;
        mat.M02 = _m02;
        mat.M10 = _m10;
        mat.M11 = _m11;
        mat.M12 = _m12;
        mat.M20 = _m20;
        mat.M21 = _m21;
        mat.M22 = _m22;
    }

    /// <summary>Applies a uniform scale to the rotation block (Java's <c>scale(float)</c>).</summary>
    public void Scale(float scale)
    {
        _m00 *= scale;
        _m10 *= scale;
        _m20 *= scale;
        _m30 *= scale;
        _m01 *= scale;
        _m11 *= scale;
        _m21 *= scale;
        _m31 *= scale;
        _m02 *= scale;
        _m12 *= scale;
        _m22 *= scale;
        _m32 *= scale;
    }

    public void SetTranslation(Vector3f translation)
    {
        _m03 = translation.X;
        _m13 = translation.Y;
        _m23 = translation.Z;
    }

    /// <summary>Rotates+translates <paramref name="vec"/>, returning the W component (Java's <c>multProj</c>).</summary>
    public float MultProj(Vector3f vec, Vector3f store)
    {
        float vx = vec.X, vy = vec.Y, vz = vec.Z;
        store.X = _m00 * vx + _m01 * vy + _m02 * vz + _m03;
        store.Y = _m10 * vx + _m11 * vy + _m12 * vz + _m13;
        store.Z = _m20 * vx + _m21 * vy + _m22 * vz + _m23;
        return _m30 * vx + _m31 * vy + _m32 * vz + _m33;
    }

    public Vector3f Mult(Vector3f vec, Vector3f? store = null)
    {
        store ??= new Vector3f();
        float vx = vec.X, vy = vec.Y, vz = vec.Z;
        store.X = _m00 * vx + _m01 * vy + _m02 * vz + _m03;
        store.Y = _m10 * vx + _m11 * vy + _m12 * vz + _m13;
        store.Z = _m20 * vx + _m21 * vy + _m22 * vz + _m23;
        return store;
    }

    /// <summary>Rotates <paramref name="vec"/> without translating (Java's <c>multNormal</c>).</summary>
    public Vector3f MultNormal(Vector3f vec, Vector3f? store = null)
    {
        store ??= new Vector3f();
        float vx = vec.X, vy = vec.Y, vz = vec.Z;
        store.X = _m00 * vx + _m01 * vy + _m02 * vz;
        store.Y = _m10 * vx + _m11 * vy + _m12 * vz;
        store.Z = _m20 * vx + _m21 * vy + _m22 * vz;
        return store;
    }

    public float Determinant()
    {
        var fA0 = _m00 * _m11 - _m01 * _m10;
        var fA1 = _m00 * _m12 - _m02 * _m10;
        var fA2 = _m00 * _m13 - _m03 * _m10;
        var fA3 = _m01 * _m12 - _m02 * _m11;
        var fA4 = _m01 * _m13 - _m03 * _m11;
        var fA5 = _m02 * _m13 - _m03 * _m12;
        var fB0 = _m20 * _m31 - _m21 * _m30;
        var fB1 = _m20 * _m32 - _m22 * _m30;
        var fB2 = _m20 * _m33 - _m23 * _m30;
        var fB3 = _m21 * _m32 - _m22 * _m31;
        var fB4 = _m21 * _m33 - _m23 * _m31;
        var fB5 = _m22 * _m33 - _m23 * _m32;
        return fA0 * fB5 - fA1 * fB4 + fA2 * fB3 + fA3 * fB2 - fA4 * fB1 + fA5 * fB0;
    }

    public Matrix4f Invert()
    {
        var store = new Matrix4f();

        var fA0 = _m00 * _m11 - _m01 * _m10;
        var fA1 = _m00 * _m12 - _m02 * _m10;
        var fA2 = _m00 * _m13 - _m03 * _m10;
        var fA3 = _m01 * _m12 - _m02 * _m11;
        var fA4 = _m01 * _m13 - _m03 * _m11;
        var fA5 = _m02 * _m13 - _m03 * _m12;
        var fB0 = _m20 * _m31 - _m21 * _m30;
        var fB1 = _m20 * _m32 - _m22 * _m30;
        var fB2 = _m20 * _m33 - _m23 * _m30;
        var fB3 = _m21 * _m32 - _m22 * _m31;
        var fB4 = _m21 * _m33 - _m23 * _m31;
        var fB5 = _m22 * _m33 - _m23 * _m32;
        var fDet = fA0 * fB5 - fA1 * fB4 + fA2 * fB3 + fA3 * fB2 - fA4 * fB1 + fA5 * fB0;

        if (FastMath.Abs(fDet) <= 0f)
            throw new InvalidOperationException("This matrix cannot be inverted");

        store._m00 = +_m11 * fB5 - _m12 * fB4 + _m13 * fB3;
        store._m10 = -_m10 * fB5 + _m12 * fB2 - _m13 * fB1;
        store._m20 = +_m10 * fB4 - _m11 * fB2 + _m13 * fB0;
        store._m30 = -_m10 * fB3 + _m11 * fB1 - _m12 * fB0;
        store._m01 = -_m01 * fB5 + _m02 * fB4 - _m03 * fB3;
        store._m11 = +_m00 * fB5 - _m02 * fB2 + _m03 * fB1;
        store._m21 = -_m00 * fB4 + _m01 * fB2 - _m03 * fB0;
        store._m31 = +_m00 * fB3 - _m01 * fB1 + _m02 * fB0;
        store._m02 = +_m31 * fA5 - _m32 * fA4 + _m33 * fA3;
        store._m12 = -_m30 * fA5 + _m32 * fA2 - _m33 * fA1;
        store._m22 = +_m30 * fA4 - _m31 * fA2 + _m33 * fA0;
        store._m32 = -_m30 * fA3 + _m31 * fA1 - _m32 * fA0;
        store._m03 = -_m21 * fA5 + _m22 * fA4 - _m23 * fA3;
        store._m13 = +_m20 * fA5 - _m22 * fA2 + _m23 * fA1;
        store._m23 = -_m20 * fA4 + _m21 * fA2 - _m23 * fA0;
        store._m33 = +_m20 * fA3 - _m21 * fA1 + _m22 * fA0;

        var invDet = 1.0f / fDet;
        store.MultLocal(invDet);
        return store;
    }

    private void MultLocal(float scalar)
    {
        _m00 *= scalar; _m01 *= scalar; _m02 *= scalar; _m03 *= scalar;
        _m10 *= scalar; _m11 *= scalar; _m12 *= scalar; _m13 *= scalar;
        _m20 *= scalar; _m21 *= scalar; _m22 *= scalar; _m23 *= scalar;
        _m30 *= scalar; _m31 *= scalar; _m32 *= scalar; _m33 *= scalar;
    }
}
