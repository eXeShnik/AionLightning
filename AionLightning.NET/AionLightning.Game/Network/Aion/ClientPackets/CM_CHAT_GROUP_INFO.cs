using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client opens whisper/group-chat window for a target player by name. Opcode 0x11F.</summary>
public sealed class CM_CHAT_GROUP_INFO : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;

    private string _playerName = string.Empty;

    public CM_CHAT_GROUP_INFO(GsClientConnection conn, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        _playerName = r.ReadS();
        r.ReadD();              // unk
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var target = _connRegistry.GetAll()
            .Select(c => c.ActivePlayer)
            .FirstOrDefault(p => p is not null && p.Name == _playerName);

        if (target is null)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.PlayerOffline(), ct);
            return;
        }

        await _conn.SendAsync(new SM_CHAT_WINDOW(target, isGroup: true), ct);
    }
}
