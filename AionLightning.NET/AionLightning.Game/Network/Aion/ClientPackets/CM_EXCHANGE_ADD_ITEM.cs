using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client adds an item to an active trade window. Opcode 0x102.</summary>
public sealed class CM_EXCHANGE_ADD_ITEM : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ExchangeService          _exchangeService;

    private int _itemObjId;
    private int _itemCount;

    public CM_EXCHANGE_ADD_ITEM(GsClientConnection conn, PlayerConnectionRegistry connRegistry,
        ExchangeService exchangeService)
    {
        _conn            = conn;
        _connRegistry    = connRegistry;
        _exchangeService = exchangeService;
    }

    public override void Read(ref PacketReader r)
    {
        _itemObjId = r.ReadD();
        _itemCount = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var session = _exchangeService.GetSession(player.ObjectId);
        if (session is null || session.InitiatorLocked || session.TargetLocked) return;

        var item = player.Inventory.Get(_itemObjId);
        if (item is null || item.IsEquipped) return;

        int count = _itemCount > 0 ? Math.Min(_itemCount, (int)item.Count) : (int)item.Count;

        bool isInitiator = session.IsInitiator(player.ObjectId);
        if (isInitiator)
            session.InitiatorItems.Add((item, count));
        else
            session.TargetItems.Add((item, count));

        var partner       = isInitiator ? session.Target : session.Initiator;
        var partnerConn   = _connRegistry.Get(partner.ObjectId);

        // action 0 = own side, 1 = partner's side
        await _conn.SendAsync(new SM_EXCHANGE_ADD_ITEM(0, item), ct);
        if (partnerConn is not null)
            await partnerConn.SendAsync(new SM_EXCHANGE_ADD_ITEM(1, item), ct);
    }
}
