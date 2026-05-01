using AionLightning.Commons.Network;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Received when a player uses /ReportAutoHunting. Opcode 0x19D.</summary>
public sealed class CM_REPORT_PLAYER : AionClientPacket
{
    private readonly GsClientConnection     _conn;
    private readonly ILogger<CM_REPORT_PLAYER> _log;

    private string _reported = string.Empty;

    public CM_REPORT_PLAYER(GsClientConnection conn, ILogger<CM_REPORT_PLAYER> log)
    {
        _conn = conn;
        _log  = log;
    }

    public override void Read(ref PacketReader r)
    {
        r.ReadB(1);        // unknown byte
        _reported = r.ReadS();
    }

    public override ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is not null)
            _log.LogInformation("Player {Reporter} reported {Reported} for auto-hunting",
                player.Name, _reported);
        return ValueTask.CompletedTask;
    }
}
