using System.Collections.Concurrent;
using AionLightning.Game.Model.Templates.Gatherable;

namespace AionLightning.Game.Services;

/// <summary>Tracks which player is currently gathering which gatherable.</summary>
public sealed class GatherService
{
    public sealed record GatherSession(int GatherableObjectId, GatherableMaterial Material);

    // playerObjectId → active session (target + pre-rolled material)
    private readonly ConcurrentDictionary<int, GatherSession> _activeSessions = new();

    /// <summary>
    /// Starts a gathering session and stores the pre-rolled material so that
    /// HandleFinish delivers the same item the player was shown in HandleStart.
    /// Returns false if the player already has an active session.
    /// </summary>
    public bool StartGathering(int playerObjectId, int gatherableObjectId, GatherableMaterial material)
    {
        var session = new GatherSession(gatherableObjectId, material);
        return _activeSessions.TryAdd(playerObjectId, session);
    }

    /// <summary>Returns the active session, or null if none.</summary>
    public GatherSession? GetSession(int playerObjectId)
        => _activeSessions.TryGetValue(playerObjectId, out var s) ? s : null;

    public void StopGathering(int playerObjectId)
        => _activeSessions.TryRemove(playerObjectId, out _);
}
