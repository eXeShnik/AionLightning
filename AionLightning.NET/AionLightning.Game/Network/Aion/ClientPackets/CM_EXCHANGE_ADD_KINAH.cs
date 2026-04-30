using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client offers kinah in an active trade. Opcode 0x100.</summary>
public sealed class CM_EXCHANGE_ADD_KINAH : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ExchangeService          _exchangeService;

    private int _amount;

    public CM_EXCHANGE_ADD_KINAH(GsClientConnection conn, PlayerConnectionRegistry connRegistry,
        ExchangeService exchangeService)
    {
        _conn            = conn;
        _connRegistry    = connRegistry;
        _exchangeService = exchangeService;
    }

    public override void Read(ref PacketReader r)
    {
        _amount = r.ReadD();
        r.ReadD(); // unk
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var session = _exchangeService.GetSession(player.ObjectId);
        if (session is null || session.InitiatorLocked || session.TargetLocked) return;

        const int KinahId = 182400001;
        var kinah = player.Inventory.FindByItemId(KinahId);
        if (kinah is null || kinah.Count < _amount || _amount <= 0) return;

        bool isInitiator = session.IsInitiator(player.ObjectId);
        if (isInitiator)
            session.InitiatorKinah = _amount;
        else
            session.TargetKinah = _amount;

        var partner     = isInitiator ? session.Target : session.Initiator;
        var partnerConn = _connRegistry.Get(partner.ObjectId);

        await _conn.SendAsync(new SM_EXCHANGE_ADD_KINAH(0, _amount), ct);
        if (partnerConn is not null)
            await partnerConn.SendAsync(new SM_EXCHANGE_ADD_KINAH(1, _amount), ct);
    }
}
