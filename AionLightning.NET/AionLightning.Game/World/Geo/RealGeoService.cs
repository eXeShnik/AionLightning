using AionLightning.Game.Configs.Options;
using AionLightning.Game.World.Geo.Collision;
using AionLightning.Game.World.Geo.Loader;
using AionLightning.Game.World.Geo.Math;
using AionLightning.Game.World.Geo.Models;
using AionLightning.Game.World.Geo.Scene;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.World.Geo;

/// <summary>
/// Port of the Java geoEngine's <c>RealGeoData</c> — loads <c>meshs.geo</c> once, then every
/// <c>{worldId}.geo</c> file found under <see cref="GeoDataOptions.DataPath"/>, keeping one
/// <see cref="GeoMap"/> per world. A per-world load failure reverts that single world to the
/// dummy fallback (Java's <c>DummyGeoData.DUMMY_MAP</c> equivalent) rather than failing the
/// whole server — and since no <c>.geo</c> data exists in this repository yet, a missing
/// <c>meshs.geo</c> is logged and this service quietly behaves exactly like
/// <see cref="DummyGeoService"/> for every world (never throws).
/// </summary>
public sealed class RealGeoService(ILogger<RealGeoService> logger, IOptions<GeoDataOptions> options) : IGeoService
{
    // Java's GeoService.getWorldSize() is itself hardcoded to 3072 for every world (not read
    // from WorldMapTemplate) — this port matches that constant rather than wiring in the
    // per-map template size, which is out of scope for this phase (see migration_plan.md).
    private const int DefaultWorldSize = 3072;
    private const int DefaultInstanceId = 1;
    private const byte SolidIntentions = (byte)(CollisionIntention.Physical | CollisionIntention.Walk | CollisionIntention.Door);

    private static readonly GeoMap DummyMap = new("dummy", 0);

    private IReadOnlyDictionary<int, GeoMap> _geoMaps = new Dictionary<int, GeoMap>();

    /// <summary>Loads every available geo map. Never throws — logs and falls back to dummy behavior on failure.</summary>
    internal async Task LoadAsync(CancellationToken cancellationToken)
    {
        var dataPath = options.Value.DataPath;
        var meshsPath = Path.Combine(dataPath, "meshs.geo");

        if (!File.Exists(meshsPath))
        {
            logger.LogWarning(
                "Geodata: {MeshsPath} not found — the real geo engine has nothing to load and will " +
                "behave exactly like the dummy engine (GetZ returns input z, CanSee always true, " +
                "GetClosestCollision returns the raw target).", meshsPath);
            return;
        }

        Dictionary<string, Spatial> models;
        try
        {
            models = await Task.Run(() => GeoWorldLoader.LoadMeshes(meshsPath), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Geodata: failed to load {MeshsPath} — falling back to dummy behavior for every world.", meshsPath);
            return;
        }

        var loaded = new Dictionary<int, GeoMap>();
        var mapsWithErrors = new List<int>();
        var worldFiles = Directory.Exists(dataPath) ? Directory.EnumerateFiles(dataPath, "*.geo") : [];

        foreach (var file in worldFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (!int.TryParse(fileName, out var worldId))
                continue;

            var map = new GeoMap(fileName, DefaultWorldSize);
            try
            {
                await Task.Run(() => GeoWorldLoader.LoadWorld(worldId, dataPath, models, map), cancellationToken).ConfigureAwait(false);
                loaded[worldId] = map;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Geodata: world {WorldId} failed to load — reverted to the dummy map.", worldId);
                mapsWithErrors.Add(worldId);
            }
        }

        _geoMaps = loaded;
        logger.LogInformation(
            "Geodata: {Count} geo map(s) loaded{Errors}.", loaded.Count,
            mapsWithErrors.Count > 0 ? $" ({mapsWithErrors.Count} reverted to dummy: {string.Join(",", mapsWithErrors)})" : string.Empty);
    }

    private GeoMap GetMap(int worldId) => _geoMaps.TryGetValue(worldId, out var map) ? map : DummyMap;

    public float GetZ(int worldId, float x, float y, float z) => GetMap(worldId).GetZ(x, y, z, DefaultInstanceId);

    public bool CanSee(int worldId, float x1, float y1, float z1, float x2, float y2, float z2)
    {
        var limit = new Vector3f(x1, y1, z1).Distance(new Vector3f(x2, y2, z2));
        return GetMap(worldId).CanSee(x1, y1, z1, x2, y2, z2, limit, DefaultInstanceId);
    }

    public (float X, float Y, float Z) GetClosestCollision(int worldId, float x, float y, float z, float targetX, float targetY, float targetZ)
    {
        var result = GetMap(worldId).GetClosestCollision(x, y, z, targetX, targetY, targetZ, true, false, DefaultInstanceId, SolidIntentions);
        return (result.X, result.Y, result.Z);
    }

    public bool IsInBounds(float x, float y, float z) =>
        x is >= 0f and <= 3072f && y is >= 0f and <= 3072f && z is >= 0f and <= 4000f;
}
