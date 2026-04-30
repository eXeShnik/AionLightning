using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client cancels an active trade. Opcode 0x2E7.</summary>
public sealed class CM_EXCHANGE_CANCEL : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ExchangeService          _exchangeService;

    public CM_EXCHANGE_CANCEL(GsClientConnection conn, PlayerConnectionRegistry connRegistry,
        ExchangeService exchangeService)
    {
        _conn            = conn;
        _connRegistry    = connRegistry;
        _exchangeService = exchangeService;
    }

    public override void Read(ref PacketReader r) { }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var session = _exchangeService.GetSession(player.ObjectId);
        if (session is null) return;

        var partner     = session.IsInitiator(player.ObjectId) ? session.Target : session.Initiator;
        var partnerConn = _connRegistry.Get(partner.ObjectId);

        _exchangeService.Cancel(player.ObjectId);

        var cancel = new SM_EXCHANGE_CONFIRMATION(SM_EXCHANGE_CONFIRMATION.Action.Cancelled);
        await _conn.SendAsync(cancel, ct);
        if (partnerConn is not null)
            await partnerConn.SendAsync(cancel, ct);
    }
}
