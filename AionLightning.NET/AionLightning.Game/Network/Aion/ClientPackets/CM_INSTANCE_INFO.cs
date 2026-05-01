using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client requests instance cooldown info (e.g. from the instance entry window). Opcode 0x182.
/// Responds with SM_INSTANCE_INFO showing no active cooldowns (portal cooldown tracking not yet implemented).
/// </summary>
public sealed class CM_INSTANCE_INFO : AionClientPacket
{
    private readonly GsClientConnection _conn;

    public CM_INSTANCE_INFO(GsClientConnection conn) => _conn = conn;

    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // unk1
        r.ReadC(); // unk2 (team flag)
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        await _conn.SendAsync(new SM_INSTANCE_INFO(player, isAnswer: true), ct);
    }
}
