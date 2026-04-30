using System.Collections.Concurrent;

namespace AionLightning.Game.Services;

/// <summary>Tracks active player-vs-player duels and exposes lifecycle helpers.</summary>
public sealed class DuelService
{
    // Each participant maps to their opponent's objectId while the duel is active.
    private readonly ConcurrentDictionary<int, int> _duels = new();

    public bool IsDueling(int objectId) => _duels.ContainsKey(objectId);

    public int? GetOpponent(int objectId)
        => _duels.TryGetValue(objectId, out var opponentId) ? opponentId : null;

    public void StartDuel(int playerA, int playerB)
    {
        _duels[playerA] = playerB;
        _duels[playerB] = playerA;
    }

    public void EndDuel(int playerA, int playerB)
    {
        _duels.TryRemove(playerA, out _);
        _duels.TryRemove(playerB, out _);
    }

    public void RemovePlayer(int objectId)
    {
        if (_duels.TryRemove(objectId, out var opponentId))
            _duels.TryRemove(opponentId, out _);
    }
}
