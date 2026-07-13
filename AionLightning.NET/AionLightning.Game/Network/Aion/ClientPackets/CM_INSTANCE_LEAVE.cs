using AionLightning.Commons.Network;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client leaves an instance. Teleports player to the race-specific exit location. Opcode 0xCC.</summary>
public sealed class CM_INSTANCE_LEAVE : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly TeleportService    _teleport;

    public CM_INSTANCE_LEAVE(GsClientConnection conn, TeleportService teleport)
    {
        _conn     = conn;
        _teleport = teleport;
    }

    public override void Read(ref PacketReader r) { }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;
        await _teleport.MoveToInstanceExitAsync(player, ct);
    }
}
