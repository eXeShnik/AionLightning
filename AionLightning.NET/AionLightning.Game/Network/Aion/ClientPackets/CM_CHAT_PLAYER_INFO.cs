using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client requests info for a player whose name was clicked in chat. Opcode 0xC5.
/// Sends STR_MSG_ASK_PCINFO_LOGOFF (1300046) when target is offline.
/// </summary>
public sealed class CM_CHAT_PLAYER_INFO : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;

    private string _playerName = string.Empty;

    public CM_CHAT_PLAYER_INFO(GsClientConnection conn, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r) => _playerName = r.ReadS();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var target = _connRegistry.GetByName(_playerName);
        if (target?.ActivePlayer is null)
        {
            // Player is offline — notify requester
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.PlayerOffline(), ct);
        }
        // If online: SM_CHAT_WINDOW would normally be sent here; client handles the absence gracefully
    }
}
