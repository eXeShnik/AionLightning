using AionLightning.Commons.Network;
using AionLightning.Game.Model.Alliance;
using AionLightning.Game.Model.League;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client invites another player to a group/alliance/force/league. Opcode 0x123.
/// Sends an SM_QUESTION_WINDOW to the target; the team is formed/expanded only on acceptance.
/// </summary>
public sealed class CM_INVITE_TO_GROUP : AionClientPacket
{
    private const byte InviteGroup    = 0;
    private const byte InviteAlliance = 12;
    private const byte InviteLeague   = 28;

    /// <summary>Java SM_QUESTION_WINDOW.STR_PARTY_ALLIANCE_DO_YOU_ACCEPT_HIS_INVITATION.</summary>
    private const int AllianceInviteQuestionCode = 70000;
    /// <summary>Java SM_QUESTION_WINDOW.STR_MSGBOX_UNION_INVITE_ME.</summary>
    private const int LeagueInviteQuestionCode = 902249;

    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly GroupService              _groupService;
    private readonly AllianceService            _allianceService;
    private readonly LeagueService              _leagueService;
    private readonly PlayerResponseRegistry    _responseRegistry;

    private byte   _inviteType;
    private string _targetName = string.Empty;

    public CM_INVITE_TO_GROUP(GsClientConnection conn, PlayerConnectionRegistry connRegistry,
        GroupService groupService, PlayerResponseRegistry responseRegistry,
        AllianceService allianceService, LeagueService leagueService)
    {
        _conn             = conn;
        _connRegistry     = connRegistry;
        _groupService     = groupService;
        _responseRegistry = responseRegistry;
        _allianceService  = allianceService;
        _leagueService    = leagueService;
    }

    public override void Read(ref PacketReader r)
    {
        _inviteType = r.ReadC(); // 0=group, 12=alliance, 28=league
        _targetName = r.ReadS();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var inviter = _conn.ActivePlayer;
        if (inviter is null) return;

        switch (_inviteType)
        {
            case InviteGroup:
                await RunGroupInviteAsync(inviter, ct);
                break;
            case InviteAlliance:
                await RunAllianceInviteAsync(inviter, ct);
                break;
            case InviteLeague:
                await RunLeagueInviteAsync(inviter, ct);
                break;
        }
    }

    private async ValueTask RunGroupInviteAsync(Model.Player inviter, CancellationToken ct)
    {
        if (inviter.Group is { IsFull: true }) return;

        var targetConn = _connRegistry.GetByName(_targetName);
        if (targetConn is null) return;

        var target = targetConn.ActivePlayer;
        if (target is null || target.IsAlreadyDead || target.Group is not null) return;

        var tcs = _responseRegistry.RegisterPending(target.ObjectId);
        try
        {
            await targetConn.SendAsync(new SM_QUESTION_WINDOW(60000, 0, 0, inviter.Name), ct);
        }
        catch
        {
            _responseRegistry.CancelPending(target.ObjectId);
            return;
        }

        bool accepted;
        try
        {
            accepted = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
        }
        catch
        {
            _responseRegistry.CancelPending(target.ObjectId);
            return;
        }

        if (!accepted) return;

        target = targetConn.ActivePlayer;
        if (target is null || target.IsAlreadyDead || target.Group is not null) return;
        if (inviter.Group is { IsFull: true }) return;

        var group = inviter.Group is null
            ? _groupService.CreateGroup(inviter, target)
            : _groupService.JoinGroup(inviter.Group, target)
                ? inviter.Group
                : null;

        if (group is null) return;

        await targetConn.SendAsync(new SM_GROUP_INFO(group), ct);
        await targetConn.SendAsync(new SM_GROUP_MEMBER_INFO(group.GroupId, target, SM_GROUP_MEMBER_INFO.GroupEvent.Join), ct);

        foreach (var member in group.Members)
        {
            if (member.ObjectId == target.ObjectId) continue;

            var memberConn = _connRegistry.Get(member.ObjectId);
            if (memberConn is not null)
                try { await memberConn.SendAsync(new SM_GROUP_MEMBER_INFO(group.GroupId, target, SM_GROUP_MEMBER_INFO.GroupEvent.Enter), ct); } catch { }

            try { await targetConn.SendAsync(new SM_GROUP_MEMBER_INFO(group.GroupId, member, SM_GROUP_MEMBER_INFO.GroupEvent.Enter), ct); } catch { }
        }
    }

    /// <summary>
    /// Java <c>PlayerAllianceInvite.acceptRequest</c>: when either side is grouped, that whole party
    /// folds into the alliance (the parties are dissolved via <see cref="GroupService.LeaveGroup"/>).
    /// </summary>
    private async ValueTask RunAllianceInviteAsync(Model.Player inviter, CancellationToken ct)
    {
        if (inviter.IsAlreadyDead) return;

        var targetConn = _connRegistry.GetByName(_targetName);
        var target = targetConn?.ActivePlayer;
        if (target is null || target.IsAlreadyDead || target.Alliance is not null) return;

        var existingAlliance = inviter.Alliance;
        if (existingAlliance is not null)
        {
            int incomingSize = target.Group?.Members.Count ?? 1;
            if (existingAlliance.MemberCount + incomingSize > PlayerAlliance.MaxMembers) return;
        }

        var tcs = _responseRegistry.RegisterPending(target.ObjectId);
        try
        {
            await targetConn!.SendAsync(new SM_QUESTION_WINDOW(AllianceInviteQuestionCode, 0, 0, inviter.Name), ct);
        }
        catch
        {
            _responseRegistry.CancelPending(target.ObjectId);
            return;
        }

        bool accepted;
        try
        {
            accepted = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
        }
        catch
        {
            _responseRegistry.CancelPending(target.ObjectId);
            return;
        }

        if (!accepted) return;

        target = targetConn!.ActivePlayer;
        if (target is null || target.IsAlreadyDead || target.Alliance is not null) return;

        var playersToAdd = new List<Model.Player>();

        if (inviter.Group is not null && existingAlliance is null)
        {
            var groupMembers = inviter.Group.Members.ToList();
            playersToAdd.AddRange(groupMembers.Where(m => m.ObjectId != inviter.ObjectId));
            foreach (var m in groupMembers)
                _groupService.LeaveGroup(m);
        }

        if (target.Group is not null)
        {
            var groupMembers = target.Group.Members.ToList();
            playersToAdd.AddRange(groupMembers);
            foreach (var m in groupMembers)
                _groupService.LeaveGroup(m);
        }
        else
        {
            playersToAdd.Add(target);
        }

        var newMembers = new List<Model.Player>();
        PlayerAlliance alliance;
        if (existingAlliance is null)
        {
            alliance = _allianceService.CreateAlliance(inviter, playersToAdd);
            newMembers.Add(inviter);
            newMembers.AddRange(playersToAdd.Where(p => alliance.HasMember(p.ObjectId)));
        }
        else
        {
            alliance = existingAlliance;
            foreach (var p in playersToAdd)
                if (_allianceService.AddMember(alliance, p))
                    newMembers.Add(p);
        }

        await BroadcastAllianceJoinAsync(alliance, newMembers, ct);
    }

    private async ValueTask BroadcastAllianceJoinAsync(PlayerAlliance alliance, IReadOnlyList<Model.Player> newMembers, CancellationToken ct)
    {
        var newIds = newMembers.Select(p => p.ObjectId).ToHashSet();
        var info = new SM_ALLIANCE_INFO(alliance);

        foreach (var member in alliance.Members)
        {
            var conn = _connRegistry.Get(member.ObjectId);
            if (conn is not null)
                try { await conn.SendAsync(info, ct); } catch { }
        }

        foreach (var newPlayer in newMembers)
        {
            var newMember = alliance.GetMember(newPlayer.ObjectId);
            if (newMember is null) continue;

            var newConn = _connRegistry.Get(newPlayer.ObjectId);
            if (newConn is not null)
                try { await newConn.SendAsync(new SM_ALLIANCE_MEMBER_INFO(newMember, SM_ALLIANCE_MEMBER_INFO.AllianceEvent.Join), ct); } catch { }

            foreach (var other in alliance.Members)
            {
                if (other.ObjectId == newPlayer.ObjectId) continue;

                var otherConn = _connRegistry.Get(other.ObjectId);
                if (otherConn is not null)
                    try { await otherConn.SendAsync(new SM_ALLIANCE_MEMBER_INFO(newMember, SM_ALLIANCE_MEMBER_INFO.AllianceEvent.Enter), ct); } catch { }

                if (!newIds.Contains(other.ObjectId) && newConn is not null)
                    try { await newConn.SendAsync(new SM_ALLIANCE_MEMBER_INFO(other, SM_ALLIANCE_MEMBER_INFO.AllianceEvent.Enter), ct); } catch { }
            }
        }
    }

    /// <summary>Java <c>LeagueInvite.acceptRequest</c>: whole alliances join a league, not individual players.</summary>
    private async ValueTask RunLeagueInviteAsync(Model.Player inviter, CancellationToken ct)
    {
        var inviterAlliance = inviter.Alliance;
        if (inviterAlliance is null || !inviterAlliance.IsLeader(inviter.ObjectId)) return;

        var existingLeague = inviterAlliance.League;
        if (existingLeague is { IsFull: true }) return;

        var targetConn = _connRegistry.GetByName(_targetName);
        var target = targetConn?.ActivePlayer;
        if (target is null) return;

        var targetAlliance = target.Alliance;
        if (targetAlliance is null || targetAlliance.IsInLeague) return;

        var tcs = _responseRegistry.RegisterPending(target.ObjectId);
        try
        {
            await targetConn!.SendAsync(new SM_QUESTION_WINDOW(LeagueInviteQuestionCode, 0, 0, inviter.Name), ct);
        }
        catch
        {
            _responseRegistry.CancelPending(target.ObjectId);
            return;
        }

        bool accepted;
        try
        {
            accepted = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
        }
        catch
        {
            _responseRegistry.CancelPending(target.ObjectId);
            return;
        }

        if (!accepted) return;

        target = targetConn!.ActivePlayer;
        targetAlliance = target?.Alliance;
        if (target is null || targetAlliance is null || targetAlliance.IsInLeague) return;

        var league = inviterAlliance.League ?? _leagueService.CreateLeague(inviterAlliance);
        if (!_leagueService.AddAlliance(league, targetAlliance)) return;

        foreach (var leagueMember in league.Members)
        {
            var info = new SM_ALLIANCE_INFO(leagueMember.Alliance);
            foreach (var p in leagueMember.Alliance.Members)
            {
                var conn = _connRegistry.Get(p.ObjectId);
                if (conn is not null)
                    try { await conn.SendAsync(info, ct); } catch { }
            }
        }
    }
}
