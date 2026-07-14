namespace AionLightning.Game.Dao;

/// <summary>Port of Java <c>dao.HouseObjectCooldownsDAO</c> — per-player reuse timers for placed house
/// objects (Java Player.houseObjectCooldownList), keyed by the object's registry objectId.</summary>
public interface IHouseObjectCooldownsDao
{
    /// <summary>Returns objectId -&gt; reuse-time (unix milliseconds) pairs for the player.</summary>
    Task<Dictionary<int, long>> LoadAsync(int playerId, CancellationToken ct = default);

    Task UpsertAsync(int playerId, int objectId, long reuseTimeMs, CancellationToken ct = default);

    Task DeleteAsync(int playerId, int objectId, CancellationToken ct = default);
}
