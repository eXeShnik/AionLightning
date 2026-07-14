using System.Collections.Concurrent;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Alliance;

namespace AionLightning.Game.Services;

/// <summary>Manages in-memory raid alliances — the same shape as <see cref="GroupService"/> one tier up.</summary>
public sealed class AllianceService
{
    private readonly ConcurrentDictionary<int, PlayerAlliance> _byAllianceId = new();
    private readonly ConcurrentDictionary<int, PlayerAlliance> _byMemberId   = new();
    private int _nextAllianceId;

    public PlayerAlliance? GetByMember(int objectId) => _byMemberId.TryGetValue(objectId, out var a) ? a : null;
    public PlayerAlliance? GetById(int allianceId)    => _byAllianceId.TryGetValue(allianceId, out var a) ? a : null;

    /// <summary>
    /// Creates a new alliance captained by <paramref name="captain"/>, then adds every other supplied
    /// player (Java <c>createAlliance</c> followed by the invite handler's <c>addPlayer</c> loop, which
    /// folds in the inviter's and invited's whole groups when either is grouped).
    /// </summary>
    public PlayerAlliance CreateAlliance(Player captain, IEnumerable<Player> membersToAdd)
    {
        int id = Interlocked.Increment(ref _nextAllianceId);
        var alliance = new PlayerAlliance(id, captain);
        _byAllianceId[id] = alliance;
        AddMember(alliance, captain);
        foreach (var p in membersToAdd)
            if (p.ObjectId != captain.ObjectId)
                AddMember(alliance, p);
        return alliance;
    }

    /// <summary>Adds a single player to the alliance's first open subgroup.</summary>
    public bool AddMember(PlayerAlliance alliance, Player player)
    {
        if (alliance.IsFull || alliance.HasMember(player.ObjectId)) return false;
        var member = new PlayerAllianceMember(player);
        if (!alliance.AddMember(member)) return false;
        _byMemberId[player.ObjectId] = alliance;
        player.Alliance = alliance;
        return true;
    }

    /// <summary>
    /// Removes a player from their alliance. Mirrors <see cref="GroupService.LeaveGroup"/>: disbands
    /// (drops from the registries, clears every remaining member's <see cref="Player.Alliance"/>) once
    /// fewer than 2 members remain, but leaves the subgroup lists themselves untouched so the caller can
    /// still read the final survivor for notification purposes.
    /// </summary>
    public PlayerAlliance? RemoveMember(Player player)
    {
        if (!_byMemberId.TryRemove(player.ObjectId, out var alliance)) return null;

        player.Alliance = null;
        alliance.RemoveMember(player.ObjectId);

        if (alliance.MemberCount < 2)
        {
            _byAllianceId.TryRemove(alliance.AllianceId, out _);
            foreach (var remaining in alliance.Members)
            {
                _byMemberId.TryRemove(remaining.ObjectId, out _);
                remaining.Player.Alliance = null;
            }
        }

        return alliance;
    }

    /// <summary>Force-disbands an alliance regardless of member count (used by the DISBAND command).</summary>
    public void Disband(PlayerAlliance alliance)
    {
        _byAllianceId.TryRemove(alliance.AllianceId, out _);
        foreach (var member in alliance.Members.ToList())
        {
            _byMemberId.TryRemove(member.ObjectId, out _);
            member.Player.Alliance = null;
        }
    }

    /// <summary>
    /// Moves a member to a different subgroup, or swaps two members' subgroups when
    /// <paramref name="secondObjId"/> is nonzero (Java <c>ChangeMemberGroupEvent</c>).
    /// </summary>
    public bool MoveMember(PlayerAlliance alliance, int firstObjId, int secondObjId, int targetGroupId)
    {
        var first = alliance.GetMember(firstObjId);
        if (first is null) return false;

        if (secondObjId != 0)
        {
            var second = alliance.GetMember(secondObjId);
            if (second is null) return false;

            var firstGroup  = alliance.GetGroup(first.AllianceGroupId);
            var secondGroup = alliance.GetGroup(second.AllianceGroupId);
            firstGroup?.RemoveMember(first.ObjectId);
            secondGroup?.RemoveMember(second.ObjectId);
            secondGroup?.AddMember(first);
            firstGroup?.AddMember(second);
            return true;
        }

        var targetGroup = alliance.GetGroup(targetGroupId);
        if (targetGroup is null || targetGroup.IsFull) return false;

        alliance.GetGroup(first.AllianceGroupId)?.RemoveMember(first.ObjectId);
        targetGroup.AddMember(first);
        return true;
    }

    /// <summary>Promotes/demotes a vice-captain; returns false if promoting would exceed the 4-seat cap.</summary>
    public bool SetViceCaptain(PlayerAlliance alliance, int objectId, bool promote)
    {
        if (promote)
        {
            if (alliance.ViceCaptainIds.Count >= PlayerAlliance.MaxViceCaptains) return false;
            if (!alliance.ViceCaptainIds.Contains(objectId))
                alliance.ViceCaptainIds.Add(objectId);
        }
        else
        {
            alliance.ViceCaptainIds.Remove(objectId);
        }

        return true;
    }

    /// <summary>
    /// Explicit captain handover (ALLIANCE_SET_CAPTAIN). The outgoing captain becomes a vice-captain
    /// when a seat is free (Java's <c>DEMOTE_CAPTAIN_TO_VICECAPTAIN</c> path).
    /// </summary>
    public bool ChangeLeader(PlayerAlliance alliance, int newCaptainObjId)
    {
        if (!alliance.HasMember(newCaptainObjId)) return false;

        int oldCaptain = alliance.CaptainObjectId;
        alliance.SetLeader(newCaptainObjId);

        if (oldCaptain != newCaptainObjId && alliance.ViceCaptainIds.Count < PlayerAlliance.MaxViceCaptains)
            alliance.ViceCaptainIds.Add(oldCaptain);

        return true;
    }
}
