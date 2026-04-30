using System.Collections.Concurrent;

namespace AionLightning.Game.Services;

/// <summary>Tracks which player is currently gathering which gatherable.</summary>
public sealed class GatherService
{
    // playerObjectId → gatherableObjectId
    private readonly ConcurrentDictionary<int, int> _activeSessions = new();

    public bool StartGathering(int playerObjectId, int gatherableObjectId)
        => _activeSessions.TryAdd(playerObjectId, gatherableObjectId);

    public int? GetActiveTarget(int playerObjectId)
        => _activeSessions.TryGetValue(playerObjectId, out var id) ? id : null;

    public void StopGathering(int playerObjectId)
        => _activeSessions.TryRemove(playerObjectId, out _);
}
