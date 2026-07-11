using AionLightning.Game.World.Geo.Math;

namespace AionLightning.Game.World.Geo.Collision.Bih;

/// <summary>
/// Port of the Java geoEngine <c>BIHNode</c> (Bounding Interval Hierarchy — Wächter/Keller).
/// Only <see cref="IntersectWhere"/> (the ray-vs-tree traversal GeoMap actually drives) is
/// ported. Java also carried an <c>intersectBrute</c> method and a <c>Collidable</c>-vs-box
/// overload of <c>intersectWhere</c>; both are dead in the original (the brute-force path is
/// commented out at the only call site, and the box overload always returns 0 collisions since
/// its collision-adding line is commented out there too) and are not ported here.
/// </summary>
internal sealed class BIHNode
{
    private readonly int _leftIndex;
    private readonly int _rightIndex;
    private BIHNode? _left;
    private BIHNode? _right;
    private float _leftPlane;
    private float _rightPlane;
    private readonly int _axis;

    /// <summary>Leaf node spanning triangle indices [l, r].</summary>
    public BIHNode(int l, int r)
    {
        _leftIndex = l;
        _rightIndex = r;
        _axis = 3; // indicates leaf
    }

    /// <summary>Internal (split) node along the given axis (0=x, 1=y, 2=z).</summary>
    public BIHNode(int axis) => _axis = axis;

    public BIHNode LeftChild
    {
        get => _left!;
        set => _left = value;
    }

    public BIHNode RightChild
    {
        get => _right!;
        set => _right = value;
    }

    public float LeftPlane
    {
        get => _leftPlane;
        set => _leftPlane = value;
    }

    public float RightPlane
    {
        get => _rightPlane;
        set => _rightPlane = value;
    }

    /// <summary>
    /// Ray-space BIH traversal. Ported verbatim from Java's <c>intersectWhere(Ray, Matrix4f,
    /// BIHTree, float, float, CollisionResults)</c>: transforms the ray into mesh-local space
    /// (mutating the caller's <paramref name="r"/> temporarily — restored before returning,
    /// matching the Java original's aliasing), walks the interval hierarchy, and ray-triangle
    /// tests each leaf's triangles, converting hits back to world space.
    /// </summary>
    public int IntersectWhere(Ray r, Matrix4f worldMatrix, BIHTree tree, float sceneMin, float sceneMax, CollisionResults results)
    {
        var stack = new List<(BIHNode Node, float Min, float Max)>();

        var o = r.Origin.Clone();
        var d = r.Direction.Clone();

        var inv = worldMatrix.Invert();
        inv.Mult(r.Origin, r.Origin);
        // Fixes rotation collision bug — matches Java's use of MultNormal (not MultNormalAcross).
        inv.MultNormal(r.Direction, r.Direction);

        var origins = new[] { r.Origin.X, r.Origin.Y, r.Origin.Z };
        var invDirections = new[] { 1f / r.Direction.X, 1f / r.Direction.Y, 1f / r.Direction.Z };

        r.Direction.NormalizeLocal();

        Vector3f v1 = new(), v2 = new(), v3 = new();
        var cols = 0;

        stack.Add((this, sceneMin, sceneMax));

        while (stack.Count > 0)
        {
            var (node, tMin, tMax) = stack[^1];
            stack.RemoveAt(stack.Count - 1);

            if (tMax < tMin)
                continue;

            var clippedOut = false;
            while (node._axis != 3)
            {
                var a = node._axis;
                var origin = origins[a];
                var invDirection = invDirections[a];

                var tNearSplit = (node._leftPlane - origin) * invDirection;
                var tFarSplit = (node._rightPlane - origin) * invDirection;
                var nearNode = node._left!;
                var farNode = node._right!;

                if (invDirection < 0)
                {
                    (tNearSplit, tFarSplit) = (tFarSplit, tNearSplit);
                    (nearNode, farNode) = (farNode, nearNode);
                }

                if (tMin > tNearSplit && tMax < tFarSplit)
                {
                    clippedOut = true;
                    break;
                }

                if (tMin > tNearSplit)
                {
                    tMin = System.Math.Max(tMin, tFarSplit);
                    node = farNode;
                }
                else if (tMax < tFarSplit)
                {
                    tMax = System.Math.Min(tMax, tNearSplit);
                    node = nearNode;
                }
                else
                {
                    stack.Add((farNode, System.Math.Max(tMin, tFarSplit), tMax));
                    tMax = System.Math.Min(tMax, tNearSplit);
                    node = nearNode;
                }
            }

            if (clippedOut)
                continue;

            // a leaf
            for (var i = node._leftIndex; i <= node._rightIndex; i++)
            {
                tree.GetTriangle(i, v1, v2, v3);

                var t = r.Intersects(v1, v2, v3);
                if (float.IsInfinity(t))
                    continue;

                worldMatrix.Mult(v1, v1);
                worldMatrix.Mult(v2, v2);
                worldMatrix.Mult(v3, v3);
                t = new Ray(o, d).Intersects(v1, v2, v3);

                var contactNormal = Triangle.ComputeTriangleNormal(v1, v2, v3, null);
                var contactPoint = new Vector3f(d).MultLocal(t).AddLocal(o);
                var worldSpaceDist = o.Distance(contactPoint);
                // fix invisible walls
                if (worldSpaceDist > r.Limit)
                    continue;

                var cr = new CollisionResult(contactPoint, worldSpaceDist) { ContactNormal = contactNormal };
                results.AddCollision(cr);
                if (results.IsOnlyFirst)
                {
                    r.SetOrigin(o);
                    r.SetDirection(d);
                    return 1;
                }

                cols++;
            }
        }

        r.SetOrigin(o);
        r.SetDirection(d);
        return cols;
    }
}
