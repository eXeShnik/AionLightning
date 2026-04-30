using System.Collections.Concurrent;
using AionLightning.Game.Model.Legion;

namespace AionLightning.Game.Services;

public sealed class LegionService
{
    private readonly ConcurrentDictionary<int, Legion> _byId     = new();
    private readonly ConcurrentDictionary<int, Legion> _byPlayer = new();

    public void LoadLegion(Legion legion)
    {
        _byId[legion.LegionId] = legion;
        foreach (var m in legion.Members.Values)
            _byPlayer[m.ObjectId] = legion;
    }

    public Legion? GetByPlayerId(int playerId) => _byPlayer.TryGetValue(playerId, out var l) ? l : null;
    public Legion? GetById(int legionId)        => _byId.TryGetValue(legionId, out var l) ? l : null;
    public Legion? GetByName(string name)        => _byId.Values.FirstOrDefault(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    public bool    IsInLegion(int playerId)      => _byPlayer.ContainsKey(playerId);

    public void AddLegion(Legion legion) => LoadLegion(legion);

    public void AddMember(Legion legion, LegionMember member)
    {
        legion.Members[member.ObjectId] = member;
        _byPlayer[member.ObjectId] = legion;
    }

    public void RemoveMember(Legion legion, int playerId)
    {
        legion.Members.Remove(playerId);
        _byPlayer.TryRemove(playerId, out _);
    }

    public void RemoveLegion(Legion legion)
    {
        _byId.TryRemove(legion.LegionId, out _);
        foreach (var m in legion.Members.Values)
            _byPlayer.TryRemove(m.ObjectId, out _);
    }
}
