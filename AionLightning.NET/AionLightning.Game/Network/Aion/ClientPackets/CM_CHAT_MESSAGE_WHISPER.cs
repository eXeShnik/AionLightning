using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client sends a whisper to another player. Opcode 0xFE.</summary>
public sealed class CM_CHAT_MESSAGE_WHISPER : AionClientPacket
{
    private readonly GsClientConnection      _conn;
    private readonly PlayerConnectionRegistry _connRegistry;

    private string _targetName = string.Empty;
    private string _message    = string.Empty;

    public CM_CHAT_MESSAGE_WHISPER(GsClientConnection conn, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        _targetName = r.ReadS();
        _message    = r.ReadS();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var sender = _conn.ActivePlayer;
        if (sender is null) return;

        var targetConn = _connRegistry.GetByName(_targetName);
        if (targetConn?.ActivePlayer is null)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.NoSuchUser(_targetName), ct);
            return;
        }

        var packet = new SM_MESSAGE(sender, _message, SM_MESSAGE.ChatType.Whisper);
        await targetConn.SendAsync(packet, ct);
        await _conn.SendAsync(packet, ct);
    }
}
