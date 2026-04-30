using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client locks (confirms) the trade window. Opcode 0x101.</summary>
public sealed class CM_EXCHANGE_LOCK : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ExchangeService          _exchangeService;
    private readonly IItemDao                 _itemDao;

    public CM_EXCHANGE_LOCK(GsClientConnection conn, PlayerConnectionRegistry connRegistry,
        ExchangeService exchangeService, IItemDao itemDao)
    {
        _conn            = conn;
        _connRegistry    = connRegistry;
        _exchangeService = exchangeService;
        _itemDao         = itemDao;
    }

    public override void Read(ref PacketReader r) { }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var session = _exchangeService.GetSession(player.ObjectId);
        if (session is null) return;

        bool isInitiator = session.IsInitiator(player.ObjectId);
        if (isInitiator)
            session.InitiatorLocked = true;
        else
            session.TargetLocked = true;

        var partner     = isInitiator ? session.Target : session.Initiator;
        var partnerConn = _connRegistry.Get(partner.ObjectId);

        if (!session.BothLocked)
        {
            // Notify partner that this side locked
            if (partnerConn is not null)
                await partnerConn.SendAsync(new SM_EXCHANGE_CONFIRMATION(SM_EXCHANGE_CONFIRMATION.Action.PartnerLocked), ct);
            return;
        }

        // Both locked — execute the trade
        await ExecuteTradeAsync(session, partnerConn, ct);
    }

    private async ValueTask ExecuteTradeAsync(ExchangeSession session,
        GsClientConnection? partnerConn, CancellationToken ct)
    {
        const int KinahId = 182400001;

        var initiatorConn = _connRegistry.Get(session.Initiator.ObjectId);
        var ini = session.Initiator;
        var tgt = session.Target;

        // Validate kinah availability
        var iniKinah = ini.Inventory.FindByItemId(KinahId);
        var tgtKinah = tgt.Inventory.FindByItemId(KinahId);
        if (session.InitiatorKinah > 0 && (iniKinah is null || iniKinah.Count < session.InitiatorKinah))
        {
            await CancelAsync(session, initiatorConn, partnerConn, ct);
            return;
        }
        if (session.TargetKinah > 0 && (tgtKinah is null || tgtKinah.Count < session.TargetKinah))
        {
            await CancelAsync(session, initiatorConn, partnerConn, ct);
            return;
        }

        // Transfer kinah
        if (session.InitiatorKinah > 0)
        {
            iniKinah!.Count -= session.InitiatorKinah;
            if (tgtKinah is not null)
                tgtKinah.Count += session.InitiatorKinah;
            else
            {
                long uid = await _itemDao.NextUniqueIdAsync(ct);
                tgt.Inventory.Add(new Item { UniqueId = uid, ItemId = KinahId, Count = session.InitiatorKinah, Slot = -1 });
            }
        }
        if (session.TargetKinah > 0)
        {
            tgtKinah!.Count -= session.TargetKinah;
            if (iniKinah is not null)
                iniKinah.Count += session.TargetKinah;
            else
            {
                long uid = await _itemDao.NextUniqueIdAsync(ct);
                ini.Inventory.Add(new Item { UniqueId = uid, ItemId = KinahId, Count = session.TargetKinah, Slot = -1 });
            }
        }

        // Validate all items are still present before touching either inventory
        foreach (var (item, _) in session.InitiatorItems)
            if (ini.Inventory.Get(item.UniqueId) is null) { await CancelAsync(session, initiatorConn, partnerConn, ct); return; }
        foreach (var (item, _) in session.TargetItems)
            if (tgt.Inventory.Get(item.UniqueId) is null) { await CancelAsync(session, initiatorConn, partnerConn, ct); return; }

        // Transfer items: move from initiator → target
        foreach (var (item, _) in session.InitiatorItems)
        {
            ini.Inventory.Remove(item.UniqueId);
            tgt.Inventory.Add(item);
        }
        // Transfer items: target → initiator
        foreach (var (item, _) in session.TargetItems)
        {
            tgt.Inventory.Remove(item.UniqueId);
            ini.Inventory.Add(item);
        }

        // Persist inventories
        await _itemDao.SaveAllAsync(ini.ObjectId, ini.Inventory.All, ct);
        await _itemDao.SaveAllAsync(tgt.ObjectId, tgt.Inventory.All, ct);

        _exchangeService.Complete(session);

        var success = new SM_EXCHANGE_CONFIRMATION(SM_EXCHANGE_CONFIRMATION.Action.Success);
        if (initiatorConn is not null) await initiatorConn.SendAsync(success, ct);
        if (partnerConn is not null)   await partnerConn.SendAsync(success, ct);
    }

    private async ValueTask CancelAsync(ExchangeSession session,
        GsClientConnection? connA, GsClientConnection? connB, CancellationToken ct)
    {
        _exchangeService.Complete(session);
        var cancel = new SM_EXCHANGE_CONFIRMATION(SM_EXCHANGE_CONFIRMATION.Action.Cancelled);
        if (connA is not null) await connA.SendAsync(cancel, ct);
        if (connB is not null) await connB.SendAsync(cancel, ct);
    }
}
