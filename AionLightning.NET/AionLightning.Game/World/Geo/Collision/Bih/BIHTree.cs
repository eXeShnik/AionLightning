using AionLightning.Game.World.Geo.Bounding;
using AionLightning.Game.World.Geo.Math;
using AionLightning.Game.World.Geo.Scene;

namespace AionLightning.Game.World.Geo.Collision.Bih;

/// <summary>
/// Port of the Java geoEngine <c>BIHTree</c> — same build algorithm (median-ish split on the
/// axis with the least "wasted" exterior extent, degenerate-partition retry at the same depth).
/// Vertex/index NIO buffers become plain <c>float[]</c>/<c>int[]</c> (see <see cref="Mesh"/>
/// remarks — the byte/short/int index-buffer split from Java's mesh layer is collapsed since
/// there is no GPU upload path to preserve compact storage for).
/// </summary>
internal sealed class BIHTree : ICollisionData
{
    public const int MaxTreeDepth = 100;
    public const int MaxTrisPerNode = 21;

    private readonly int _maxTrisPerNode;
    private int _numTris;
    private float[] _pointData = [];
    private int[] _triIndices = [];
    private readonly float[] _swapTmp = new float[9];
    private BIHNode? _root;

    public BIHTree(Mesh mesh, int maxTrisPerNode = MaxTrisPerNode)
    {
        if (maxTrisPerNode < 1)
            throw new ArgumentOutOfRangeException(nameof(maxTrisPerNode));

        _maxTrisPerNode = maxTrisPerNode;

        var positions = mesh.Positions;
        var indices = mesh.Indices;
        _numTris = indices.Length / 3;
        InitTriList(positions, indices);
    }

    private void InitTriList(float[] positions, int[] indices)
    {
        _pointData = new float[_numTris * 3 * 3];
        var p = 0;
        for (var i = 0; i < _numTris * 3; i += 3)
        {
            var vert = indices[i] * 3;
            _pointData[p++] = positions[vert++];
            _pointData[p++] = positions[vert++];
            _pointData[p++] = positions[vert];

            vert = indices[i + 1] * 3;
            _pointData[p++] = positions[vert++];
            _pointData[p++] = positions[vert++];
            _pointData[p++] = positions[vert];

            vert = indices[i + 2] * 3;
            _pointData[p++] = positions[vert++];
            _pointData[p++] = positions[vert++];
            _pointData[p++] = positions[vert];
        }

        _triIndices = new int[_numTris];
        for (var i = 0; i < _numTris; i++)
            _triIndices[i] = i;
    }

    public void Construct()
    {
        var sceneBbox = CreateBox(0, _numTris - 1);
        _root = CreateNode(0, _numTris - 1, sceneBbox, 0);
    }

    private BoundingBox CreateBox(int l, int r)
    {
        var min = new Vector3f(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector3f(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        Vector3f v1 = new(), v2 = new(), v3 = new();
        for (var i = l; i <= r; i++)
        {
            GetTriangle(i, v1, v2, v3);
            BoundingBox.CheckMinMax(min, max, v1);
            BoundingBox.CheckMinMax(min, max, v2);
            BoundingBox.CheckMinMax(min, max, v3);
        }

        return new BoundingBox(min, max);
    }

    private int SortTriangles(int l, int r, float split, int axis)
    {
        var pivot = l;
        var j = r;

        Vector3f v1 = new(), v2 = new(), v3 = new();
        while (pivot <= j)
        {
            GetTriangle(pivot, v1, v2, v3);
            v1.AddLocal(v2).AddLocal(v3).MultLocal(FastMath.OneThird);
            if (v1.Get(axis) > split)
            {
                SwapTriangles(pivot, j);
                --j;
            }
            else
            {
                ++pivot;
            }
        }

        return pivot == l && j < pivot ? j : pivot;
    }

    private static void SetMinMax(BoundingBox bbox, bool doMin, int axis, float value)
    {
        var min = bbox.GetMin();
        var max = bbox.GetMax();

        if (doMin) min.Set(axis, value);
        else max.Set(axis, value);

        bbox.SetMinMax(min, max);
    }

    private static float GetMinMax(BoundingBox bbox, bool doMin, int axis) =>
        doMin ? bbox.GetMin().Get(axis) : bbox.GetMax().Get(axis);

    private BIHNode CreateNode(int l, int r, BoundingBox nodeBbox, int depth)
    {
        if (r - l < _maxTrisPerNode || depth > MaxTreeDepth)
            return new BIHNode(l, r);

        var currentBox = CreateBox(l, r);

        var exteriorExt = nodeBbox.GetExtent();
        var interiorExt = currentBox.GetExtent();
        exteriorExt.SubtractLocal(interiorExt);

        int axis;
        if (exteriorExt.X > exteriorExt.Y)
            axis = exteriorExt.X > exteriorExt.Z ? 0 : 2;
        else
            axis = exteriorExt.Y > exteriorExt.Z ? 1 : 2;

        if (exteriorExt.ValueEquals(Vector3f.Zero))
            axis = 0;

        var split = currentBox.Center.Get(axis);
        var pivot = SortTriangles(l, r, split, axis);
        if (pivot == l || pivot == r)
            pivot = (r + l) / 2;

        if (pivot < l)
        {
            var rbbox = new BoundingBox(currentBox);
            SetMinMax(rbbox, true, axis, split);
            return CreateNode(l, r, rbbox, depth + 1);
        }

        if (pivot > r)
        {
            var lbbox = new BoundingBox(currentBox);
            SetMinMax(lbbox, false, axis, split);
            return CreateNode(l, r, lbbox, depth + 1);
        }

        var node = new BIHNode(axis);

        var leftBbox = new BoundingBox(currentBox);
        SetMinMax(leftBbox, false, axis, split);
        node.LeftPlane = GetMinMax(CreateBox(l, System.Math.Max(l, pivot - 1)), false, axis);
        node.LeftChild = CreateNode(l, System.Math.Max(l, pivot - 1), leftBbox, depth + 1);

        var rightBbox = new BoundingBox(currentBox);
        SetMinMax(rightBbox, true, axis, split);
        node.RightPlane = GetMinMax(CreateBox(pivot, r), true, axis);
        node.RightChild = CreateNode(pivot, r, rightBbox, depth + 1);

        return node;
    }

    public void GetTriangle(int index, Vector3f v1, Vector3f v2, Vector3f v3)
    {
        var pointIndex = index * 9;

        v1.X = _pointData[pointIndex++];
        v1.Y = _pointData[pointIndex++];
        v1.Z = _pointData[pointIndex++];

        v2.X = _pointData[pointIndex++];
        v2.Y = _pointData[pointIndex++];
        v2.Z = _pointData[pointIndex++];

        v3.X = _pointData[pointIndex++];
        v3.Y = _pointData[pointIndex++];
        v3.Z = _pointData[pointIndex];
    }

    private void SwapTriangles(int index1, int index2)
    {
        var p1 = index1 * 9;
        var p2 = index2 * 9;

        Array.Copy(_pointData, p1, _swapTmp, 0, 9);
        Array.Copy(_pointData, p2, _pointData, p1, 9);
        Array.Copy(_swapTmp, 0, _pointData, p2, 9);

        (_triIndices[index1], _triIndices[index2]) = (_triIndices[index2], _triIndices[index1]);
    }

    private int CollideWithRay(Ray ray, Matrix4f worldMatrix, BoundingVolume worldBound, CollisionResults results)
    {
        var boundResults = new CollisionResults(results.Intentions, results.IsOnlyFirst, results.InstanceId);
        worldBound.CollideWith(ray, boundResults);
        if (boundResults.Size <= 0)
            return 0;

        var tMin = boundResults.GetClosestCollision()!.Distance;
        var tMax = boundResults.GetFarthestCollision()!.Distance;

        if (tMax <= 0) tMax = float.PositiveInfinity;
        else if (tMin == tMax) tMin = 0;

        if (tMin <= 0) tMin = 0;

        if (ray.Limit < float.PositiveInfinity)
            tMax = System.Math.Min(tMax, ray.Limit);

        return _root!.IntersectWhere(ray, worldMatrix, this, tMin, tMax, results);
    }

    public int CollideWith(ICollidable other, Matrix4f worldMatrix, BoundingVolume worldBound, CollisionResults results) => other switch
    {
        Ray ray => CollideWithRay(ray, worldMatrix, worldBound, results),
        _ => throw new UnsupportedCollisionException($"With: {other.GetType().Name}")
    };
}
