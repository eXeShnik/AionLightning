using AionLightning.Commons.Network;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client cancels their own broker listing and receives the item back. Opcode 0x142.</summary>
public sealed class CM_BROKER_CANCEL_REGISTERED : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly BrokerService      _broker;

    private int _brokerItemId;

    public CM_BROKER_CANCEL_REGISTERED(GsClientConnection conn, BrokerService broker)
    {
        _conn   = conn;
        _broker = broker;
    }

    public override void Read(ref PacketReader r)
    {
        r.ReadD();               // npcId — ignored
        _brokerItemId = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        await _broker.CancelAsync(player, _brokerItemId, ct);
    }
}
