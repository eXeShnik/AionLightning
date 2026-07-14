using AionLightning.Commons.Network;
using AionLightning.Game.Model.Alliance;
using AionLightning.Game.Model.League;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Group/alliance/league management commands (leave, kick, promote, ready-check) + LFG status.
/// Opcode 0x122. Wire format: commandCode(C) + playerObjId(D) + allianceGroupId(D) + secondObjectId(D).
/// Command codes mirror Java's <c>TeamCommand</c> enum.
/// </summary>
public sealed class CM_PLAYER_STATUS_INFO : AionClientPacket
{
    private const byte CMD_GROUP_BAN_MEMBER   = 2;
    private const byte CMD_GROUP_SET_LEADER   = 3;
    private const byte CMD_GROUP_REMOVE       = 6;
    private const byte CMD_GROUP_SET_LFG      = 9;

    private const byte CMD_ALLIANCE_LEAVE                = 14;
    private const byte CMD_ALLIANCE_BAN_MEMBER           = 16;
    private const byte CMD_ALLIANCE_SET_CAPTAIN          = 17;
    private const byte CMD_ALLIANCE_CHECKREADY_CANCEL    = 20;
    private const byte CMD_ALLIANCE_CHECKREADY_START     = 21;
    private const byte CMD_ALLIANCE_CHECKREADY_AUTOCANCEL = 22;
    private const byte CMD_ALLIANCE_CHECKREADY_READY     = 23;
    private const byte CMD_ALLIANCE_CHECKREADY_NOTREADY  = 24;
    private const byte CMD_ALLIANCE_SET_VICECAPTAIN      = 25;
    private const byte CMD_ALLIANCE_UNSET_VICECAPTAIN    = 26;
    private const byte CMD_ALLIANCE_CHANGE_GROUP         = 27;
    private const byte CMD_LEAGUE_LEAVE                  = 29;
    private const byte CMD_LEAGUE_EXPEL                  = 30;

    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly GroupService             _groupService;
    private readonly AllianceService          _allianceService;
    private readonly LeagueService            _leagueService;

    private byte _commandCode;
    private int  _playerObjId;
    private int  _allianceGroupId;
    private int  _secondObjectId;

    public CM_PLAYER_STATUS_INFO(GsClientConnection conn, PlayerConnectionRegistry connRegistry,
        GroupService groupService, AllianceService allianceService, LeagueService leagueService)
    {
        _conn             = conn;
        _connRegistry     = connRegistry;
        _groupService     = groupService;
        _allianceService  = allianceService;
        _leagueService    = leagueService;
    }

    public override void Read(ref PacketReader r)
    {
        _commandCode     = r.ReadC();
        _playerObjId     = r.ReadD();
        _allianceGroupId = r.ReadD();
        _secondObjectId  = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        switch (_commandCode)
        {
            case CMD_GROUP_REMOVE:
                await HandleRemoveMemberAsync(player, _playerObjId, ct);
                break;

            case CMD_GROUP_BAN_MEMBER:
                // Kick: leader forces target out — same notification path as voluntary leave
                if (player.Group is not null && player.Group.IsLeader(player.ObjectId))
                    await HandleRemoveMemberAsync(player, _playerObjId, ct);
                break;

            case CMD_GROUP_SET_LEADER:
                await HandleChangeLeaderAsync(player, _playerObjId, ct);
                break;

            case CMD_ALLIANCE_LEAVE:
                await HandleAllianceLeaveAsync(player, ct);
                break;

            case CMD_ALLIANCE_BAN_MEMBER:
                await HandleAllianceBanAsync(player, _playerObjId, ct);
                break;

            case CMD_ALLIANCE_SET_CAPTAIN:
                await HandleAllianceSetCaptainAsync(player, _playerObjId, ct);
                break;

            case CMD_ALLIANCE_CHECKREADY_CANCEL:
            case CMD_ALLIANCE_CHECKREADY_START:
            case CMD_ALLIANCE_CHECKREADY_AUTOCANCEL:
            case CMD_ALLIANCE_CHECKREADY_READY:
            case CMD_ALLIANCE_CHECKREADY_NOTREADY:
                await HandleAllianceCheckReadyAsync(player, _commandCode, ct);
                break;

            case CMD_ALLIANCE_SET_VICECAPTAIN:
                await HandleAllianceViceCaptainAsync(player, _playerObjId, promote: true, ct);
                break;

            case CMD_ALLIANCE_UNSET_VICECAPTAIN:
                await HandleAllianceViceCaptainAsync(player, _playerObjId, promote: false, ct);
                break;

            case CMD_ALLIANCE_CHANGE_GROUP:
                await HandleAllianceChangeGroupAsync(player, _playerObjId, _secondObjectId, _allianceGroupId, ct);
                break;

            case CMD_LEAGUE_LEAVE:
                await HandleLeagueLeaveAsync(player, ct);
                break;

            case CMD_LEAGUE_EXPEL:
                await HandleLeagueExpelAsync(player, _playerObjId, ct);
                break;
        }
    }

    private async ValueTask HandleRemoveMemberAsync(Model.Player player, int targetObjId, CancellationToken ct)
    {
        // playerObjId == 0 means self-leave
        int subjectId = targetObjId == 0 ? player.ObjectId : targetObjId;

        var group = player.Group;
        if (group is null) return;

        // Only leader can remove others; anyone can remove themselves
        if (subjectId != player.ObjectId && !group.IsLeader(player.ObjectId)) return;

        // Resolve the Player object from the group's member list (works even if offline)
        var subject = group.Members.FirstOrDefault(m => m.ObjectId == subjectId);
        if (subject is null) return;

        var leftGroup = _groupService.LeaveGroup(subject);

        // Notify the leaving player (only if they have an active connection)
        var subjectConn = _connRegistry.Get(subjectId);
        if (subjectConn is not null)
            try { await subjectConn.SendAsync(new SM_LEAVE_GROUP_MEMBER(), ct); } catch { }

        if (leftGroup is null) return;

        // Notify remaining members about the departure
        var leaveNotify = new SM_GROUP_MEMBER_INFO(leftGroup.GroupId, subject, SM_GROUP_MEMBER_INFO.GroupEvent.Leave);
        foreach (var member in leftGroup.Members)
        {
            var mc = _connRegistry.Get(member.ObjectId);
            if (mc is not null)
                try { await mc.SendAsync(leaveNotify, ct); } catch { }
        }

        // If group disbanded (fewer than 2 members remain), notify solo survivor
        if (leftGroup.Members.Count < 2 && leftGroup.Members.Count > 0)
        {
            var survivor     = leftGroup.Members[0];
            var survivorConn = _connRegistry.Get(survivor.ObjectId);
            if (survivorConn is not null)
            {
                try { await survivorConn.SendAsync(new SM_LEAVE_GROUP_MEMBER(), ct); } catch { }
                try { await survivorConn.SendAsync(new SM_GROUP_INFO(), ct); } catch { }
            }
        }
    }

    private async ValueTask HandleChangeLeaderAsync(Model.Player player, int newLeaderObjId, CancellationToken ct)
    {
        var group = player.Group;
        if (group is null || !group.IsLeader(player.ObjectId)) return;
        if (!group.HasMember(newLeaderObjId)) return;

        group.SetLeader(newLeaderObjId);

        var packet = new SM_GROUP_INFO(group);
        foreach (var member in group.Members)
        {
            var mc = _connRegistry.Get(member.ObjectId);
            if (mc is not null)
                try { await mc.SendAsync(packet, ct); } catch { }
        }
    }

    private async ValueTask HandleAllianceLeaveAsync(Model.Player player, CancellationToken ct)
    {
        if (player.Alliance is null) return;
        await RemoveFromAllianceAsync(player, ct);
    }

    private async ValueTask HandleAllianceBanAsync(Model.Player player, int targetObjId, CancellationToken ct)
    {
        var alliance = player.Alliance;
        if (alliance is null || !alliance.IsSomeCaptain(player.ObjectId)) return;

        var target = alliance.GetMember(targetObjId)?.Player;
        if (target is null) return;

        await RemoveFromAllianceAsync(target, ct);
    }

    /// <summary>Java <c>PlayerAllianceLeavedEvent</c>: removal (leave or ban) + broadcast, mirroring the party HandleRemoveMemberAsync flow.</summary>
    private async ValueTask RemoveFromAllianceAsync(Model.Player subject, CancellationToken ct)
    {
        var alliance = subject.Alliance;
        if (alliance is null) return;

        var member = alliance.GetMember(subject.ObjectId);
        if (member is null) return;

        var leftAlliance = _allianceService.RemoveMember(subject);
        if (leftAlliance is null) return;

        var subjectConn = _connRegistry.Get(subject.ObjectId);
        if (subjectConn is not null)
            try { await subjectConn.SendAsync(new SM_LEAVE_GROUP_MEMBER(), ct); } catch { }

        var leaveNotify = new SM_ALLIANCE_MEMBER_INFO(member, SM_ALLIANCE_MEMBER_INFO.AllianceEvent.Leave);
        var infoNotify  = new SM_ALLIANCE_INFO(leftAlliance);
        foreach (var m in leftAlliance.Members)
        {
            var mc = _connRegistry.Get(m.ObjectId);
            if (mc is null) continue;
            try { await mc.SendAsync(leaveNotify, ct); } catch { }
            try { await mc.SendAsync(infoNotify, ct); } catch { }
        }

        // If the alliance auto-disbanded (fewer than 2 members remain), notify the solo survivor.
        if (leftAlliance.MemberCount < 2 && leftAlliance.MemberCount > 0)
        {
            var survivor     = leftAlliance.Members.First();
            var survivorConn = _connRegistry.Get(survivor.ObjectId);
            if (survivorConn is not null)
                try { await survivorConn.SendAsync(new SM_LEAVE_GROUP_MEMBER(), ct); } catch { }
        }
    }

    private async ValueTask HandleAllianceSetCaptainAsync(Model.Player player, int newCaptainObjId, CancellationToken ct)
    {
        var alliance = player.Alliance;
        if (alliance is null || !alliance.IsLeader(player.ObjectId)) return;
        if (!_allianceService.ChangeLeader(alliance, newCaptainObjId)) return;

        await BroadcastAllianceInfoAsync(alliance, ct);
    }

    private async ValueTask HandleAllianceViceCaptainAsync(Model.Player player, int targetObjId, bool promote, CancellationToken ct)
    {
        var alliance = player.Alliance;
        if (alliance is null || !alliance.IsLeader(player.ObjectId)) return;
        if (!alliance.HasMember(targetObjId)) return;
        if (!_allianceService.SetViceCaptain(alliance, targetObjId, promote)) return;

        await BroadcastAllianceInfoAsync(alliance, ct);
    }

    private async ValueTask HandleAllianceChangeGroupAsync(Model.Player player, int firstObjId, int secondObjId, int allianceGroupId, CancellationToken ct)
    {
        var alliance = player.Alliance;
        if (alliance is null || !alliance.IsSomeCaptain(player.ObjectId)) return;

        var first  = alliance.GetMember(firstObjId);
        var second = secondObjId != 0 ? alliance.GetMember(secondObjId) : null;
        if (first is null || (secondObjId != 0 && second is null)) return;

        if (!_allianceService.MoveMember(alliance, firstObjId, secondObjId, allianceGroupId)) return;

        foreach (var member in alliance.Members)
        {
            var mc = _connRegistry.Get(member.ObjectId);
            if (mc is null) continue;
            try { await mc.SendAsync(new SM_ALLIANCE_MEMBER_INFO(first, SM_ALLIANCE_MEMBER_INFO.AllianceEvent.MemberGroupChange), ct); } catch { }
            if (second is not null)
                try { await mc.SendAsync(new SM_ALLIANCE_MEMBER_INFO(second, SM_ALLIANCE_MEMBER_INFO.AllianceEvent.MemberGroupChange), ct); } catch { }
        }
    }

    /// <summary>Java <c>CheckAllianceReadyEvent</c> — statuses: 0=cancelled, 1=start, 2=autocancel, 3=all-ready, 4=not-ready, 5=echo.</summary>
    private async ValueTask HandleAllianceCheckReadyAsync(Model.Player player, byte command, CancellationToken ct)
    {
        var alliance = player.Alliance;
        if (alliance is null) return;

        switch (command)
        {
            case CMD_ALLIANCE_CHECKREADY_CANCEL:
                alliance.ReadyStatus = 0;
                await BroadcastReadyCheckAsync(alliance, player.ObjectId, 0, ct);
                break;

            case CMD_ALLIANCE_CHECKREADY_START:
                alliance.ReadyStatus = alliance.MemberCount - 1;
                await BroadcastReadyCheckAsync(alliance, player.ObjectId, 5, ct);
                await BroadcastReadyCheckAsync(alliance, player.ObjectId, 1, ct);
                break;

            case CMD_ALLIANCE_CHECKREADY_AUTOCANCEL:
                alliance.ReadyStatus = 0;
                await BroadcastReadyCheckAsync(alliance, player.ObjectId, 2, ct);
                break;

            case CMD_ALLIANCE_CHECKREADY_READY:
                alliance.ReadyStatus -= 1;
                await BroadcastReadyCheckAsync(alliance, player.ObjectId, 5, ct);
                if (alliance.ReadyStatus == 0)
                    await BroadcastReadyCheckAsync(alliance, 0, 3, ct);
                break;

            case CMD_ALLIANCE_CHECKREADY_NOTREADY:
                alliance.ReadyStatus -= 1;
                await BroadcastReadyCheckAsync(alliance, player.ObjectId, 4, ct);
                if (alliance.ReadyStatus == 0)
                    await BroadcastReadyCheckAsync(alliance, 0, 3, ct);
                break;
        }
    }

    private async ValueTask BroadcastReadyCheckAsync(PlayerAlliance alliance, int playerObjId, int statusCode, CancellationToken ct)
    {
        var packet = new SM_ALLIANCE_READY_CHECK(playerObjId, statusCode);
        foreach (var member in alliance.Members)
        {
            var mc = _connRegistry.Get(member.ObjectId);
            if (mc is not null)
                try { await mc.SendAsync(packet, ct); } catch { }
        }
    }

    private async ValueTask BroadcastAllianceInfoAsync(PlayerAlliance alliance, CancellationToken ct)
    {
        var packet = new SM_ALLIANCE_INFO(alliance);
        foreach (var member in alliance.Members)
        {
            var mc = _connRegistry.Get(member.ObjectId);
            if (mc is not null)
                try { await mc.SendAsync(packet, ct); } catch { }
        }
    }

    private async ValueTask HandleLeagueLeaveAsync(Model.Player player, CancellationToken ct)
    {
        var alliance = player.Alliance;
        if (alliance is null || alliance.League is null || !alliance.IsLeader(player.ObjectId)) return;

        await RemoveAllianceFromLeagueAsync(alliance, expelledBy: null, ct);
    }

    private async ValueTask HandleLeagueExpelAsync(Model.Player player, int targetAllianceId, CancellationToken ct)
    {
        var callerAlliance = player.Alliance;
        if (callerAlliance is null || callerAlliance.League is null) return;
        if (!callerAlliance.IsLeader(player.ObjectId)) return;
        if (!callerAlliance.League.IsLeader(callerAlliance.AllianceId)) return;

        var targetAlliance = _allianceService.GetById(targetAllianceId);
        if (targetAlliance is null || targetAlliance.League != callerAlliance.League) return;

        await RemoveAllianceFromLeagueAsync(targetAlliance, expelledBy: callerAlliance, ct);
    }

    /// <summary>Java <c>LeagueLeftEvent</c>: removes the alliance and notifies both it and the rest of the league.</summary>
    private async ValueTask RemoveAllianceFromLeagueAsync(PlayerAlliance alliance, PlayerAlliance? expelledBy, CancellationToken ct)
    {
        var league = alliance.League;
        if (league is null) return;

        var otherAllianceIds = league.Members.Select(m => m.AllianceId).Where(id => id != alliance.AllianceId).ToList();

        var leftLeague = _leagueService.RemoveAlliance(alliance);
        if (leftLeague is null) return;

        int messageId = expelledBy is null
            ? SM_ALLIANCE_INFO_LEAGUE_LEFT
            : SM_ALLIANCE_INFO_LEAGUE_EXPELLED;
        var leaderName = expelledBy?.GetMember(expelledBy.CaptainObjectId)?.Name
            ?? alliance.GetMember(alliance.CaptainObjectId)?.Name
            ?? string.Empty;

        var selfPacket = new SM_ALLIANCE_INFO(alliance, messageId, leaderName);
        foreach (var m in alliance.Members)
        {
            var mc = _connRegistry.Get(m.ObjectId);
            if (mc is not null)
                try { await mc.SendAsync(selfPacket, ct); } catch { }
        }

        int otherMessageId = expelledBy is null ? SM_ALLIANCE_INFO_LEAGUE_LEFT : SM_ALLIANCE_INFO_LEAGUE_EXPEL;
        foreach (var otherAllianceId in otherAllianceIds)
        {
            var otherAlliance = _allianceService.GetById(otherAllianceId);
            if (otherAlliance is null) continue;

            var otherPacket = new SM_ALLIANCE_INFO(otherAlliance, otherMessageId, leaderName);
            foreach (var m in otherAlliance.Members)
            {
                var mc = _connRegistry.Get(m.ObjectId);
                if (mc is not null)
                    try { await mc.SendAsync(otherPacket, ct); } catch { }
            }
        }
    }

    // Java SM_ALLIANCE_INFO string-message constants (kept local — no broader SM_SYSTEM_MESSAGE catalog entry ported for these).
    private const int SM_ALLIANCE_INFO_LEAGUE_LEFT     = 1400572;
    private const int SM_ALLIANCE_INFO_LEAGUE_EXPEL    = 1400574;
    private const int SM_ALLIANCE_INFO_LEAGUE_EXPELLED = 1400576;
}
