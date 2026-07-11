using AionLightning.Game.World.Geo.Bounding;
using AionLightning.Game.World.Geo.Collision;
using AionLightning.Game.World.Geo.Math;
using AionLightning.Game.World.Geo.Scene;

namespace AionLightning.Game.World.Geo.Models;

/// <summary>
/// Port of the Java geoEngine <c>GeoMap</c> — one per world, holding a 256x256-unit grid of
/// sub-<see cref="Node"/>s (for cheap spatial partitioning of instance meshes) plus the
/// terrain heightmap. Door support (Java's <c>doors</c> map / <c>DoorGeometry</c>) is not
/// ported — Phase 3 per the task: <see cref="SetDoorState"/> is a no-op, matching this repo's
/// (and Java's default) <c>GEO_DOORS_ENABLE=false</c> behavior.
/// </summary>
internal sealed class GeoMap : Node
{
    private short[] _terrainData = [];
    private readonly List<BoundingBox> _tmpBox = [];

    public GeoMap(string name, int worldSize) : base(name)
    {
        CollisionFlags = (short)((byte)CollisionIntention.All << 8);
        for (var x = 0; x < worldSize; x += 256)
        {
            for (var y = 0; y < worldSize; y += 256)
            {
                var geoNode = new Node(string.Empty) { CollisionFlags = (short)((byte)CollisionIntention.All << 8) };
                _tmpBox.Add(new BoundingBox(new Vector3f(x, y, 0), new Vector3f(x + 256, y + 256, 4000)));
                base.AttachChild(geoNode);
            }
        }
    }

    public void SetTerrainData(short[] terrainData) => _terrainData = terrainData;

    /// <summary>Doors are not supported by this port (Phase 3 stub) — always a no-op.</summary>
    public void SetDoorState(int instanceId, string name, bool isOpened)
    {
    }

    /// <summary>Routes an instance's geometry into every 256-unit region it overlaps.</summary>
    public override int AttachChild(Spatial child)
    {
        var i = 0;
        foreach (var spatial in GetChildren())
        {
            if (child.WorldBound != null && _tmpBox[i].Intersects(child.WorldBound))
                ((Node)spatial).AttachChild(child);
            i++;
        }

        return 0;
    }

    /// <summary>Downward ray from z=4000 to find the ground/mesh height under (x, y, z).</summary>
    public float GetZ(float x, float y, float z, int instanceId)
    {
        var results = new CollisionResults((byte)CollisionIntention.Physical, false, instanceId);
        var pos = new Vector3f(x, y, z + 2);
        var dir = new Vector3f(x, y, z - 100);
        var limit = pos.Distance(dir);
        dir.SubtractLocal(pos).NormalizeLocal();
        var r = new Ray(pos, dir) { Limit = limit };
        CollideWith(r, results);

        Vector3f? terrain = null;
        if (_terrainData.Length == 1)
        {
            if (_terrainData[0] != 0)
                terrain = new Vector3f(x, y, _terrainData[0] / 32f);
        }
        else
        {
            terrain = TerrainCollision(x, y, r);
        }

        if (terrain != null && terrain.Z > 0 && terrain.Z < z + 2)
            results.AddCollision(new CollisionResult(terrain, System.Math.Abs(z - terrain.Z + 2)));

        if (results.Size == 0)
            return z;

        return results.GetClosestCollision()!.ContactPoint.Z;
    }

    public Vector3f GetClosestCollision(float x, float y, float z, float targetX, float targetY, float targetZ,
        bool changeDirection, bool fly, int instanceId, byte intentions)
    {
        float zChecked1 = 0;
        float zChecked2 = 0;
        if (!fly && changeDirection)
        {
            zChecked1 = z;
            z = GetZ(x, y, z + 2, instanceId);
        }

        z += 1f;
        targetZ += 1f;
        var start = new Vector3f(x, y, z);
        var end = new Vector3f(targetX, targetY, targetZ);
        var pos = new Vector3f(x, y, z);
        var dir = new Vector3f(targetX, targetY, targetZ);

        var results = new CollisionResults(intentions, false, instanceId);

        var limit = pos.Distance(dir);
        dir.SubtractLocal(pos).NormalizeLocal();
        var r = new Ray(pos, dir) { Limit = limit };
        var terrain = CalculateTerrainCollision(start.X, start.Y, start.Z, end.X, end.Y, end.Z, r);
        if (terrain != null)
            results.AddCollision(new CollisionResult(terrain, terrain.Distance(pos)));

        CollideWith(r, results);

        float geoZ = 0;
        if (results.Size == 0)
        {
            if (fly)
                return end;

            if (zChecked1 > 0 && targetX == x && targetY == y && targetZ - 1f == zChecked1)
            {
                geoZ = z - 1f;
            }
            else
            {
                zChecked2 = targetZ;
                geoZ = GetZ(targetX, targetY, targetZ + 2, instanceId);
            }

            if (System.Math.Abs(geoZ - targetZ) < start.Distance(end))
            {
                end.Z = geoZ;
                return end;
            }

            return start;
        }

        var closest = results.GetClosestCollision()!;
        var contactPoint = closest.ContactPoint;
        var distance = closest.Distance;
        if (distance < 1)
            return start;

        // -1m
        contactPoint = contactPoint.Subtract(dir);
        if (!fly && changeDirection)
        {
            if (zChecked1 > 0 && contactPoint.X == x && contactPoint.Y == y && contactPoint.Z == zChecked1)
                contactPoint.Z = z - 1f;
            else if (zChecked2 > 0 && contactPoint.X == targetX && contactPoint.Y == targetY && contactPoint.Z == zChecked2)
                contactPoint.Z = geoZ;
            else
                contactPoint.Z = GetZ(contactPoint.X, contactPoint.Y, contactPoint.Z + 2, instanceId);
        }

        if (!fly && System.Math.Abs(start.Z - contactPoint.Z) > distance)
            return start;

        return contactPoint;
    }

    public bool CanSee(float x, float y, float z, float targetX, float targetY, float targetZ, float limit, int instanceId)
    {
        targetZ += 1;
        z += 1;

        var x2 = x - targetX;
        var y2 = y - targetY;
        var distance = FastMath.Sqrt(x2 * x2 + y2 * y2);
        if (distance > 80f)
            return false;

        var intD = (int)System.Math.Abs(distance);

        var pos = new Vector3f(x, y, z);
        var dir = new Vector3f(targetX, targetY, targetZ);
        dir.SubtractLocal(pos).NormalizeLocal();
        var r = new Ray(pos, dir) { Limit = limit };

        for (float s = 2; s < intD; s += 2)
        {
            var tempX = targetX + (x2 * s / distance);
            var tempY = targetY + (y2 * s / distance);
            var result = TerrainCollision(tempX, tempY, r);
            if (result != null)
                return false;
        }

        var results = new CollisionResults((byte)(CollisionIntention.Physical | CollisionIntention.Door), false, instanceId);
        var collisions = CollideWith(r, results);
        return results.Size == 0 && collisions == 0;
    }

    private Vector3f? CalculateTerrainCollision(float x, float y, float z, float targetX, float targetY, float targetZ, Ray ray)
    {
        var x2 = targetX - x;
        var y2 = targetY - y;
        var intD = (int)System.Math.Abs(ray.Limit);

        for (float s = 0; s < intD; s += 2)
        {
            var tempX = x + (x2 * s / ray.Limit);
            var tempY = y + (y2 * s / ray.Limit);
            var result = TerrainCollision(tempX, tempY, ray);
            if (result != null)
                return result;
        }

        return null;
    }

    /// <summary>Bilinear (2-triangle) terrain height sample at 2m tile resolution.</summary>
    private Vector3f? TerrainCollision(float x, float y, Ray ray)
    {
        y /= 2f;
        x /= 2f;
        var xInt = (int)x;
        var yInt = (int)y;

        // p1-----p2
        // ||     ||
        // ||     ||
        // p3-----p4
        float p1, p2, p3, p4;
        if (_terrainData.Length == 1)
        {
            p1 = p2 = p3 = p4 = _terrainData[0] / 32f;
        }
        else
        {
            var size = (int)System.Math.Sqrt(_terrainData.Length);
            var i1 = yInt + xInt * size;
            var i2 = (yInt + 1) + xInt * size;
            var i3 = yInt + (xInt + 1) * size;
            var i4 = (yInt + 1) + (xInt + 1) * size;
            if (i1 < 0 || i2 < 0 || i3 < 0 || i4 < 0 ||
                i1 >= _terrainData.Length || i2 >= _terrainData.Length || i3 >= _terrainData.Length || i4 >= _terrainData.Length)
                return null;

            p1 = _terrainData[i1] / 32f;
            p2 = _terrainData[i2] / 32f;
            p3 = _terrainData[i3] / 32f;
            p4 = _terrainData[i4] / 32f;
        }

        var result = new Vector3f();
        if (p1 >= 0 && p2 >= 0 && p3 >= 0)
        {
            var triangle1 = new Triangle(
                new Vector3f(xInt * 2, yInt * 2, p1),
                new Vector3f(xInt * 2, (yInt + 1) * 2, p2),
                new Vector3f((xInt + 1) * 2, yInt * 2, p3));
            if (ray.IntersectWhere(triangle1, result))
                return result;
        }

        if (p4 >= 0 && p2 >= 0 && p3 >= 0)
        {
            var triangle2 = new Triangle(
                new Vector3f((xInt + 1) * 2, (yInt + 1) * 2, p4),
                new Vector3f(xInt * 2, (yInt + 1) * 2, p2),
                new Vector3f((xInt + 1) * 2, yInt * 2, p3));
            if (ray.IntersectWhere(triangle2, result))
                return result;
        }

        return null;
    }

    public override void UpdateModelBound()
    {
        Children.RemoveAll(s => s is Node n && n.GetChildren().Count == 0);
        base.UpdateModelBound();
    }
}
