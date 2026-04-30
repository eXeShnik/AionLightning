using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client invites another player to a group. Opcode 0x123.
/// Auto-accepts the invite (no question-window round-trip).
/// </summary>
public sealed class CM_INVITE_TO_GROUP : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly GroupService             _groupService;

    private string _targetName = string.Empty;

    public CM_INVITE_TO_GROUP(GsClientConnection conn, PlayerConnectionRegistry connRegistry,
        GroupService groupService)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
        _groupService = groupService;
    }

    public override void Read(ref PacketReader r) => _targetName = r.ReadS();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var inviter = _conn.ActivePlayer;
        if (inviter is null) return;
        if (inviter.Group is { IsFull: true }) return;

        var targetConn = _connRegistry.GetByName(_targetName);
        if (targetConn is null) return;

        var target = targetConn.ActivePlayer;
        if (target is null || target.IsAlreadyDead || target.Group is not null) return;

        var group = inviter.Group is null
            ? _groupService.CreateGroup(inviter, target)
            : _groupService.JoinGroup(inviter.Group, target)
                ? inviter.Group
                : null;

        if (group is null) return;

        // Send group metadata to the newly joined player.
        var targetConn2 = _connRegistry.Get(target.ObjectId);
        if (targetConn2 is not null)
        {
            await targetConn2.SendAsync(new SM_GROUP_INFO(group), ct);
            // JOIN event tells the client "this is you joining the group"
            await targetConn2.SendAsync(new SM_GROUP_MEMBER_INFO(group.GroupId, target, SM_GROUP_MEMBER_INFO.GroupEvent.Join), ct);
        }

        // Exchange ENTER events: existing members see the new player; new player sees existing members.
        foreach (var member in group.Members)
        {
            if (member.ObjectId == target.ObjectId) continue;

            // Existing member gets info about the newly joined player
            var memberConn = _connRegistry.Get(member.ObjectId);
            if (memberConn is not null)
                await memberConn.SendAsync(new SM_GROUP_MEMBER_INFO(group.GroupId, target, SM_GROUP_MEMBER_INFO.GroupEvent.Enter), ct);

            // New player gets info about each existing member
            if (targetConn2 is not null)
                await targetConn2.SendAsync(new SM_GROUP_MEMBER_INFO(group.GroupId, member, SM_GROUP_MEMBER_INFO.GroupEvent.Enter), ct);
        }
    }
}
