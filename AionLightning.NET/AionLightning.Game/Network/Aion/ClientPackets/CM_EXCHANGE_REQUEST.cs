using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests a trade with another player. Opcode 0x11D.</summary>
public sealed class CM_EXCHANGE_REQUEST : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly GameWorld                _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ExchangeService          _exchangeService;

    private int _targetObjectId;

    public CM_EXCHANGE_REQUEST(GsClientConnection conn, GameWorld world,
        PlayerConnectionRegistry connRegistry, ExchangeService exchangeService)
    {
        _conn            = conn;
        _world           = world;
        _connRegistry    = connRegistry;
        _exchangeService = exchangeService;
    }

    public override void Read(ref PacketReader r) => _targetObjectId = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var initiator = _conn.ActivePlayer;
        if (initiator is null) return;

        // Cannot trade with self or if already in a trade
        if (_targetObjectId == initiator.ObjectId) return;
        if (_exchangeService.GetSession(initiator.ObjectId) is not null) return;

        var targetConn = _connRegistry.Get(_targetObjectId);
        var target = targetConn?.ActivePlayer;
        if (target is null) return;
        if (_exchangeService.GetSession(target.ObjectId) is not null) return;

        _exchangeService.Start(initiator, target);

        // Send exchange window open packet to both sides
        await _conn.SendAsync(new SM_EXCHANGE_REQUEST(target.Name), ct);
        await targetConn!.SendAsync(new SM_EXCHANGE_REQUEST(initiator.Name), ct);
    }
}
