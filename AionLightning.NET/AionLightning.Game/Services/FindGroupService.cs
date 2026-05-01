using System.Collections.Concurrent;
using AionLightning.Game.Model;

namespace AionLightning.Game.Services;

/// <summary>
/// In-memory LFG board. Two boards per race: recruiters (groups seeking members)
/// and applicants (players seeking groups). Keyed by ObjectId.
/// </summary>
public sealed class FindGroupService
{
    private readonly ConcurrentDictionary<int, FindGroupEntry> _elyosRecruiters   = new();
    private readonly ConcurrentDictionary<int, FindGroupEntry> _elyosApplicants   = new();
    private readonly ConcurrentDictionary<int, FindGroupEntry> _asmodRecruiters   = new();
    private readonly ConcurrentDictionary<int, FindGroupEntry> _asmodApplicants   = new();

    private ConcurrentDictionary<int, FindGroupEntry> GetBoard(Race race, bool recruiter) =>
        race == Race.ELYOS
            ? (recruiter ? _elyosRecruiters : _elyosApplicants)
            : (recruiter ? _asmodRecruiters : _asmodApplicants);

    public void AddRecruiter(Race race, FindGroupEntry entry) =>
        GetBoard(race, recruiter: true)[entry.ObjectId] = entry;

    public void AddApplicant(Race race, FindGroupEntry entry) =>
        GetBoard(race, recruiter: false)[entry.ObjectId] = entry;

    public void RemoveRecruiter(Race race, int objectId) =>
        GetBoard(race, recruiter: true).TryRemove(objectId, out _);

    public void RemoveApplicant(Race race, int objectId) =>
        GetBoard(race, recruiter: false).TryRemove(objectId, out _);

    public FindGroupEntry? GetRecruiter(Race race, int objectId) =>
        GetBoard(race, recruiter: true).TryGetValue(objectId, out var e) ? e : null;

    public FindGroupEntry? GetApplicant(Race race, int objectId) =>
        GetBoard(race, recruiter: false).TryGetValue(objectId, out var e) ? e : null;

    public IReadOnlyList<FindGroupEntry> GetRecruiters(Race race) =>
        GetBoard(race, recruiter: true).Values.ToList();

    public IReadOnlyList<FindGroupEntry> GetApplicants(Race race) =>
        GetBoard(race, recruiter: false).Values.ToList();

    public void RemoveAll(Race race, int objectId)
    {
        RemoveRecruiter(race, objectId);
        RemoveApplicant(race, objectId);
    }
}
