using System.Text;
using AionLightning.Game.World.Geo.Collision;
using AionLightning.Game.World.Geo.Math;
using AionLightning.Game.World.Geo.Models;
using AionLightning.Game.World.Geo.Scene;

namespace AionLightning.Game.World.Geo.Loader;

/// <summary>
/// Port of the Java geoEngine <c>GeoWorldLoader</c> — binary reader for <c>meshs.geo</c> (shared
/// mesh library) and per-world <c>{worldId}.geo</c> (terrain + instance placements). All
/// integers/floats in these files are little-endian; <see cref="BinaryReader"/> reads primitives
/// little-endian on every .NET-supported architecture, so no explicit byte-swapping is needed
/// (Java's original used an NIO <c>MappedByteBuffer</c> with <c>ByteOrder.LITTLE_ENDIAN</c> for
/// the same reason — this is a straight semantic port, not a literal buffer-mapping port).
/// Door and material-zone handling (Java's <c>DoorGeometry</c> creation and
/// <c>ZoneService.createMaterialZoneTemplate</c> calls) are stubbed out — Phase 3 per the task —
/// matching this repo's (and Java's default) doors/materials-disabled behavior.
/// </summary>
internal static class GeoWorldLoader
{
    public static Dictionary<string, Spatial> LoadMeshes(string filePath)
    {
        var geoms = new Dictionary<string, Spatial>();
        using var stream = File.OpenRead(filePath);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        while (stream.Position < stream.Length)
        {
            var name = ReadPrefixedName(reader);
            var node = new Node(name);
            byte intentions = 0;
            sbyte singleChildMaterialId = -1;
            var modelCount = reader.ReadInt16();

            for (var c = 0; c < modelCount; c++)
            {
                var mesh = new Mesh();

                var vectorCount = reader.ReadInt16() * 3;
                var vertices = new float[vectorCount];
                for (var x = 0; x < vectorCount; x++)
                    vertices[x] = reader.ReadSingle();

                // Count of raw index shorts (== triangleCount * 3), not a triangle count.
                var indexValueCount = reader.ReadInt32();
                var indices = new int[indexValueCount];
                for (var x = 0; x < indexValueCount; x++)
                    indices[x] = reader.ReadUInt16();

                mesh.CollisionFlags = reader.ReadInt16();
                if ((mesh.Intentions & (byte)CollisionIntention.Moveable) != 0)
                {
                    // TODO: moveable collisions (ships, shugo boxes) — not handled yet.
                    continue;
                }

                intentions |= mesh.Intentions;
                mesh.SetPositions(vertices);
                mesh.SetIndices(indices);
                mesh.CreateCollisionData();

                if ((intentions & (byte)CollisionIntention.Door) != 0 && (intentions & (byte)CollisionIntention.Physical) != 0)
                {
                    // Doors not supported by this port (Phase 3 stub) — skip entirely.
                    continue;
                }

                var geomName = modelCount == 1 ? name : $"child{c}_{name}";
                var geom = new Geometry(geomName, mesh);
                if (modelCount == 1)
                    singleChildMaterialId = geom.MaterialId;

                node.AttachChild(geom);
                geoms[geom.Name!] = geom;
            }

            node.CollisionFlags = (short)((intentions << 8) | (singleChildMaterialId & 0xFF));
            if (node.GetChildren().Count > 0)
                geoms[name] = node;
        }

        return geoms;
    }

    public static bool LoadWorld(int worldId, string geoDataPath, Dictionary<string, Spatial> models, GeoMap map)
    {
        var path = Path.Combine(geoDataPath, $"{worldId}.geo");
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        if (reader.ReadByte() == 0)
        {
            map.SetTerrainData([reader.ReadInt16()]);
        }
        else
        {
            var size = reader.ReadInt32();
            var terrainData = new short[size];
            for (var i = 0; i < size; i++)
                terrainData[i] = reader.ReadInt16();
            map.SetTerrainData(terrainData);
        }

        while (stream.Position < stream.Length)
        {
            var name = ReadPrefixedName(reader);
            var loc = new Vector3f(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            var matrixValues = new float[9];
            for (var i = 0; i < 9; i++)
                matrixValues[i] = reader.ReadSingle();
            var scale = reader.ReadSingle();

            var matrix = new Matrix3f().Set(matrixValues);

            if (!models.TryGetValue(name.ToLowerInvariant(), out var node))
                continue;

            try
            {
                AttachInstance(map, node, matrix, loc, scale);

                if (node is Node childHolder)
                {
                    // Multi-model meshes: each "childN_<name>" geometry gets its own instance
                    // placement with the same transform (material-zone registration per child —
                    // Java's createZone — is not ported; see class remarks).
                    var escapedName = name.Replace("\\", "\\\\");
                    foreach (var child in childHolder.DescendantMatches($"child\\d+_{escapedName}"))
                        AttachInstance(map, child, matrix, loc, scale);
                }
            }
            catch (Exception)
            {
                // Java swallows per-instance failures and continues loading the rest of the world.
            }
        }

        map.UpdateModelBound();
        return true;
    }

    private static Spatial AttachInstance(GeoMap map, Spatial node, Matrix3f matrix, Vector3f location, float scale)
    {
        var clone = node.DeepClone();
        clone.SetTransform(matrix, location, scale);
        clone.UpdateModelBound();
        map.AttachChild(clone);
        return clone;
    }

    private static string ReadPrefixedName(BinaryReader reader)
    {
        var length = reader.ReadInt16();
        var bytes = reader.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }
}
