using System.Collections.Concurrent;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;

namespace AionLightning.Game.Services;

public sealed class ExchangeSession
{
    public Player Initiator { get; }
    public Player Target    { get; }

    public List<(Item item, int count)> InitiatorItems { get; } = new();
    public List<(Item item, int count)> TargetItems    { get; } = new();

    public long InitiatorKinah { get; set; }
    public long TargetKinah    { get; set; }

    public bool InitiatorLocked { get; set; }
    public bool TargetLocked    { get; set; }

    public ExchangeSession(Player initiator, Player target)
    {
        Initiator = initiator;
        Target    = target;
    }

    public bool IsInitiator(int playerId) => playerId == Initiator.ObjectId;

    public bool BothLocked => InitiatorLocked && TargetLocked;
}

/// <summary>Manages in-memory player-to-player exchange sessions.</summary>
public sealed class ExchangeService
{
    // keyed by each participant's objectId (each session stored under two keys)
    private readonly ConcurrentDictionary<int, ExchangeSession> _sessions = new();

    public ExchangeSession? GetSession(int playerId)
        => _sessions.GetValueOrDefault(playerId);

    public ExchangeSession Start(Player initiator, Player target)
    {
        var session = new ExchangeSession(initiator, target);
        _sessions[initiator.ObjectId] = session;
        _sessions[target.ObjectId]    = session;
        return session;
    }

    public void Cancel(int playerId)
    {
        if (!_sessions.TryRemove(playerId, out var session)) return;
        _sessions.TryRemove(session.IsInitiator(playerId)
            ? session.Target.ObjectId
            : session.Initiator.ObjectId, out _);
    }

    public void Complete(ExchangeSession session)
    {
        _sessions.TryRemove(session.Initiator.ObjectId, out _);
        _sessions.TryRemove(session.Target.ObjectId, out _);
    }
}
