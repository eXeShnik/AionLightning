using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client issues a /loc command — responds with current map coordinates. Opcode 0x12C.</summary>
public sealed class CM_CLIENT_COMMAND_LOC : AionClientPacket
{
    private readonly GsClientConnection _conn;

    public CM_CLIENT_COMMAND_LOC(GsClientConnection conn) => _conn = conn;

    public override void Read(ref PacketReader r) { }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var pos = player.Position;
        await _conn.SendAsync(SM_SYSTEM_MESSAGE.LocationDesc(pos.WorldId, pos.X, pos.Y, pos.Z), ct);
    }
}
