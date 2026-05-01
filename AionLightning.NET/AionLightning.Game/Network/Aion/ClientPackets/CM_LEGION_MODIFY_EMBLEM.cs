using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Brigade General changes the legion emblem. Opcode 0x119.
/// Only DEFAULT-type emblems are supported (no custom image upload).
/// Broadcasts SM_LEGION_UPDATE_EMBLEM to all online players so their UI refreshes.
/// </summary>
public sealed class CM_LEGION_MODIFY_EMBLEM : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly LegionService            _legionService;
    private readonly PlayerConnectionRegistry _connRegistry;

    private int  _legionId;
    private byte _emblemId;
    private byte _emblemType;
    private byte _r;
    private byte _g;
    private byte _b;

    public CM_LEGION_MODIFY_EMBLEM(GsClientConnection conn, LegionService legionService,
        PlayerConnectionRegistry connRegistry)
    {
        _conn          = conn;
        _legionService = legionService;
        _connRegistry  = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        _legionId   = r.ReadD();
        _emblemId   = r.ReadC();
        _emblemType = r.ReadC();
        r.ReadC();              // fixed 0xFF
        _r = r.ReadC();
        _g = r.ReadC();
        _b = r.ReadC();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var legion = player.Legion;
        if (legion is null || legion.LegionId != _legionId) return;

        // Only Brigade General can change the emblem
        var member = legion.Members.GetValueOrDefault(player.ObjectId);
        if (member is null || member.Rank != Model.Legion.LegionRank.BrigadeGeneral) return;

        // Ignore custom emblem type — DEFAULT only
        if (_emblemType != 0) return;

        legion.EmblemId   = _emblemId;
        legion.EmblemType = _emblemType;
        legion.EmblemR    = _r;
        legion.EmblemG    = _g;
        legion.EmblemB    = _b;

        var update = new SM_LEGION_UPDATE_EMBLEM(
            legion.LegionId, legion.EmblemId, legion.EmblemType,
            legion.EmblemR, legion.EmblemG, legion.EmblemB);

        foreach (var conn in _connRegistry.GetAll())
            try { await conn.SendAsync(update, ct); } catch { }
    }
}
