using AionLightning.Commons.Network;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client buys an item from the broker. Opcode 0x15C.</summary>
public sealed class CM_BUY_BROKER_ITEM : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly BrokerService      _broker;

    private int _brokerItemId;

    public CM_BUY_BROKER_ITEM(GsClientConnection conn, BrokerService broker)
    {
        _conn   = conn;
        _broker = broker;
    }

    public override void Read(ref PacketReader r)
    {
        r.ReadD();              // brokerId — ignored
        _brokerItemId = r.ReadD();
        r.ReadH();              // itemCount — ignored (always buy 1 stack)
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || player.IsAlreadyDead) return;

        await _broker.BuyAsync(player, _brokerItemId, ct);
    }
}
