using System.Collections.Concurrent;
using AionLightning.Game.Model.Alliance;
using AionLightning.Game.Model.League;

namespace AionLightning.Game.Services;

/// <summary>Manages in-memory leagues (an alliance-of-alliances, up to 8 members) — mirrors <see cref="AllianceService"/> one tier up.</summary>
public sealed class LeagueService
{
    private readonly ConcurrentDictionary<int, League> _byId = new();
    private int _nextId;

    public League? GetById(int leagueId) => _byId.TryGetValue(leagueId, out var l) ? l : null;

    public League CreateLeague(PlayerAlliance leaderAlliance)
    {
        int id = Interlocked.Increment(ref _nextId);
        var league = new League(id, leaderAlliance);
        _byId[id] = league;
        leaderAlliance.League = league;
        return league;
    }

    public bool AddAlliance(League league, PlayerAlliance alliance)
    {
        if (alliance.IsInLeague || !league.AddAlliance(alliance)) return false;
        alliance.League = league;
        return true;
    }

    /// <summary>Removes an alliance (leave or expel). Disbands the league once at most one alliance remains.</summary>
    public League? RemoveAlliance(PlayerAlliance alliance)
    {
        var league = alliance.League;
        if (league is null) return null;

        league.RemoveAlliance(alliance.AllianceId);
        alliance.League = null;

        if (league.Members.Count < 2)
            DisbandInternal(league);

        return league;
    }

    public void Disband(League league) => DisbandInternal(league);

    private void DisbandInternal(League league)
    {
        _byId.TryRemove(league.LeagueId, out _);
        foreach (var member in league.Members.ToList())
            member.Alliance.League = null;
    }
}
