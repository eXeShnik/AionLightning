using AionLightning.Commons.Network;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client registers an item on the broker. Opcode 0x15D.</summary>
public sealed class CM_REGISTER_BROKER_ITEM : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly BrokerService      _broker;

    private int  _uniqueId;
    private long _price;
    private int  _count;

    public CM_REGISTER_BROKER_ITEM(GsClientConnection conn, BrokerService broker)
    {
        _conn   = conn;
        _broker = broker;
    }

    public override void Read(ref PacketReader r)
    {
        r.ReadD();          // brokerId — ignored
        _uniqueId = r.ReadD();
        _price    = r.ReadQ();
        _count    = r.ReadH();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || player.IsAlreadyDead) return;
        if (_price < 1 || _count < 1) return;

        await _broker.RegisterAsync(player, _uniqueId, _count, _price, ct);
    }
}
