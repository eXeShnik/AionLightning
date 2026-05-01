using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client views their own broker listings. Opcode 0x15F.</summary>
public sealed class CM_BROKER_REGISTERED : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly BrokerService      _broker;

    public CM_BROKER_REGISTERED(GsClientConnection conn, BrokerService broker)
    {
        _conn   = conn;
        _broker = broker;
    }

    public override void Read(ref PacketReader r) => r.ReadD(); // npcId — ignored

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var items = _broker.GetListings(player.ObjectId, player.Race);
        await _conn.SendAsync(SM_BROKER_SERVICE.RegisteredItems(items), ct);
    }
}
