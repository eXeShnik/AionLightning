using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Group management commands (leave, kick, change leader) + LFG status. Opcode 0x122.
/// Wire format: commandCode(C) + playerObjId(D) + allianceGroupId(D) + secondObjectId(D)
/// </summary>
public sealed class CM_PLAYER_STATUS_INFO : AionClientPacket
{
    private const byte CMD_GROUP_BAN_MEMBER   = 2;
    private const byte CMD_GROUP_SET_LEADER   = 3;
    private const byte CMD_GROUP_REMOVE       = 6;
    private const byte CMD_GROUP_SET_LFG      = 9;

    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly GroupService             _groupService;

    private byte _commandCode;
    private int  _playerObjId;

    public CM_PLAYER_STATUS_INFO(GsClientConnection conn, PlayerConnectionRegistry connRegistry,
        GroupService groupService)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
        _groupService = groupService;
    }

    public override void Read(ref PacketReader r)
    {
        _commandCode = r.ReadC();
        _playerObjId = r.ReadD();
        r.ReadD(); // allianceGroupId — unused
        r.ReadD(); // secondObjectId  — unused
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
}
