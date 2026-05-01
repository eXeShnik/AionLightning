using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client requests legion emblem metadata (used when the client needs to display
/// a DEFAULT emblem and has no cached copy). Opcode 0xD2.
/// Custom emblem data (CUSTOM type) is not supported; those requests are silently ignored.
/// </summary>
public sealed class CM_LEGION_SEND_EMBLEM_INFO : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly LegionService      _legionService;

    private int _legionId;

    public CM_LEGION_SEND_EMBLEM_INFO(GsClientConnection conn, LegionService legionService)
    {
        _conn          = conn;
        _legionService = legionService;
    }

    public override void Read(ref PacketReader r) => _legionId = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (_conn.ActivePlayer is null) return;

        var legion = _legionService.GetById(_legionId);
        if (legion is null || legion.EmblemType != 0) return;

        await _conn.SendAsync(new SM_LEGION_SEND_EMBLEM(
            legion.LegionId, legion.EmblemId, legion.EmblemType,
            legion.EmblemR, legion.EmblemG, legion.EmblemB,
            legion.Name), ct);
    }
}
