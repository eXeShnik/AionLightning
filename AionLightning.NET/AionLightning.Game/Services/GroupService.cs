using System.Collections.Concurrent;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Group;

namespace AionLightning.Game.Services;

/// <summary>Manages in-memory party groups.</summary>
public sealed class GroupService
{
    private readonly ConcurrentDictionary<int, PlayerGroup> _byGroupId  = new();
    private readonly ConcurrentDictionary<int, PlayerGroup> _byMemberId = new();
    private int _nextGroupId;

    public PlayerGroup? GetGroupByMember(int objectId)
        => _byMemberId.TryGetValue(objectId, out var g) ? g : null;

    /// <summary>Creates a new 2-player group, sets Group on both players, returns the group.</summary>
    public PlayerGroup CreateGroup(Player leader, Player invited)
    {
        int gid   = Interlocked.Increment(ref _nextGroupId);
        var group = new PlayerGroup(gid, leader);
        group.AddMember(invited);
        _byGroupId[gid] = group;
        _byMemberId[leader.ObjectId]  = group;
        _byMemberId[invited.ObjectId] = group;
        leader.Group  = group;
        invited.Group = group;
        return group;
    }

    /// <summary>Adds a player to an existing group.</summary>
    public bool JoinGroup(PlayerGroup group, Player player)
    {
        if (!group.AddMember(player)) return false;
        _byMemberId[player.ObjectId] = group;
        player.Group = group;
        return true;
    }

    /// <summary>
    /// Removes a player from their group.
    /// Returns the group they left (null if not in a group).
    /// Disbands the group (removes from dictionaries) when fewer than 2 members remain.
    /// </summary>
    public PlayerGroup? LeaveGroup(Player player)
    {
        if (!_byMemberId.TryRemove(player.ObjectId, out var group)) return null;

        player.Group = null;
        group.RemoveMember(player.ObjectId);

        if (group.Members.Count < 2)
        {
            _byGroupId.TryRemove(group.GroupId, out _);
            foreach (var remaining in group.Members)
            {
                _byMemberId.TryRemove(remaining.ObjectId, out _);
                remaining.Group = null;
            }
        }

        return group;
    }
}
