using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client updates social display/deny settings. Opcode 0xAE.</summary>
public sealed class CM_CUSTOM_SETTINGS : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IPlayerDao _playerDao;

    private int _display;
    private int _deny;

    public CM_CUSTOM_SETTINGS(GsClientConnection conn, PlayerConnectionRegistry connRegistry, IPlayerDao playerDao)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
        _playerDao    = playerDao;
    }

    public override void Read(ref PacketReader r)
    {
        _display = r.ReadH();
        _deny    = r.ReadH();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        player.DisplaySettings = _display;
        player.DenySettings    = _deny;

        await _playerDao.UpdateDisplaySettingsAsync(player.ObjectId, _display, _deny, ct);

        var packet  = new SM_CUSTOM_SETTINGS(player.ObjectId, _display, _deny);
        int worldId = player.Position.WorldId;
        await _conn.SendAsync(packet, ct);
        foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
            if (other.ActivePlayer?.Position.WorldId == worldId)
                try { await other.SendAsync(packet, ct); } catch { }
    }
}
