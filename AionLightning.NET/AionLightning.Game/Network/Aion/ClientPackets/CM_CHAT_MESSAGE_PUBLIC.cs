using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client sends a public chat message. Opcode 0xF9.</summary>
public sealed class CM_CHAT_MESSAGE_PUBLIC : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;

    private byte   _channelType;
    private string _message = string.Empty;

    public CM_CHAT_MESSAGE_PUBLIC(GsClientConnection conn, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        _channelType = r.ReadC();
        _message     = r.ReadS();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        // Group chat — deliver only to party members
        if (_channelType == 0x05)
        {
            var group = player.Group;
            if (group is null) return;

            var groupPacket = new SM_MESSAGE(player, _message, SM_MESSAGE.ChatType.Group);
            foreach (var member in group.Members)
            {
                var mc = _connRegistry.Get(member.ObjectId);
                if (mc is not null) try { await mc.SendAsync(groupPacket, ct); } catch { }
            }
            return;
        }

        // Legion chat — no legion system yet; unknown channel types silently dropped
        if (_channelType is not (0x00 or 0x03)) return;

        var chatType = _channelType == 0x03 ? SM_MESSAGE.ChatType.Shout : SM_MESSAGE.ChatType.Normal;

        // Normal and Shout are zone-scoped: only players in the same WorldId receive them
        var packet  = new SM_MESSAGE(player, _message, chatType);
        int worldId = player.Position.WorldId;
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(packet, ct); } catch { }
    }
}
