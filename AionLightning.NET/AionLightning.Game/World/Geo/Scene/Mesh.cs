using AionLightning.Game.World.Geo.Bounding;
using AionLightning.Game.World.Geo.Collision;
using AionLightning.Game.World.Geo.Collision.Bih;
using AionLightning.Game.World.Geo.Math;

namespace AionLightning.Game.World.Geo.Scene;

/// <summary>
/// Port of the Java geoEngine <c>Mesh</c>, collapsed to what the geo engine actually needs: flat
/// vertex-position and triangle-index arrays plus a lazily-built <see cref="BIHTree"/>. Java's
/// generic <c>VertexBuffer</c>/<c>IndexBuffer</c> (byte/short/int variants) and <c>GLObject</c>
/// machinery existed to share code with the renderer's GPU upload path; there is no renderer
/// here, so triangle data is stored directly as <c>float[]</c> positions and a single
/// <c>int[]</c> index buffer (Java's Byte/Short/IntIndexBuffer split collapsed into one, per the
/// port plan).
/// </summary>
internal sealed class Mesh
{
    private ICollisionData? _collisionTree;

    public float[] Positions { get; private set; } = [];

    public int[] Indices { get; private set; } = [];

    public BoundingVolume MeshBound { get; private set; } = new BoundingBox();

    public short CollisionFlags { get; set; } = -1;

    /// <summary>Signed to match Java's <c>byte getMaterialId()</c> — see <see cref="Spatial.MaterialId"/>.</summary>
    public sbyte MaterialId => (sbyte)(CollisionFlags & 0xFF);

    public byte Intentions => (byte)(CollisionFlags >> 8);

    public void SetPositions(float[] positions) => Positions = positions;

    public void SetIndices(int[] indices) => Indices = indices;

    public void GetTriangle(int index, Vector3f v1, Vector3f v2, Vector3f v3)
    {
        var vertIndex = index * 3;
        var i1 = Indices[vertIndex] * 3;
        var i2 = Indices[vertIndex + 1] * 3;
        var i3 = Indices[vertIndex + 2] * 3;

        v1.Set(Positions[i1], Positions[i1 + 1], Positions[i1 + 2]);
        v2.Set(Positions[i2], Positions[i2 + 1], Positions[i2 + 2]);
        v3.Set(Positions[i3], Positions[i3 + 1], Positions[i3 + 2]);
    }

    public void CreateCollisionData()
    {
        if (_collisionTree != null)
            return;

        var tree = new BIHTree(this);
        tree.Construct();
        _collisionTree = tree;
    }

    public int CollideWith(ICollidable other, Matrix4f worldMatrix, BoundingVolume worldBound, CollisionResults results)
    {
        _collisionTree ??= CreateAndReturn();
        return _collisionTree.CollideWith(other, worldMatrix, worldBound, results);

        ICollisionData CreateAndReturn()
        {
            CreateCollisionData();
            return _collisionTree!;
        }
    }

    public void UpdateBound() => MeshBound.ComputeFromPoints(Positions);

    public BoundingVolume GetBound() => MeshBound;

    public void SetBound(BoundingVolume? modelBound) => MeshBound = modelBound ?? new BoundingBox();
}
