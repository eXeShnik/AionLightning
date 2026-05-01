using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client invites another player to a group. Opcode 0x123.
/// Sends an SM_QUESTION_WINDOW to the target; group is formed only on acceptance.
/// </summary>
public sealed class CM_INVITE_TO_GROUP : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly GroupService             _groupService;
    private readonly PlayerResponseRegistry   _responseRegistry;

    private byte   _inviteType;
    private string _targetName = string.Empty;

    public CM_INVITE_TO_GROUP(GsClientConnection conn, PlayerConnectionRegistry connRegistry,
        GroupService groupService, PlayerResponseRegistry responseRegistry)
    {
        _conn             = conn;
        _connRegistry     = connRegistry;
        _groupService     = groupService;
        _responseRegistry = responseRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        _inviteType = r.ReadC(); // 0=group, 12=alliance, 28=league — only 0 handled
        _targetName = r.ReadS();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (_inviteType != 0) return;

        var inviter = _conn.ActivePlayer;
        if (inviter is null) return;
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
}
