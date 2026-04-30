using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Final trade confirmation (second lock click). Opcode 0x2E6.
/// Delegates to the same logic as CM_EXCHANGE_LOCK.
/// </summary>
public sealed class CM_EXCHANGE_OK : AionClientPacket
{
    private readonly CM_EXCHANGE_LOCK _inner;

    public CM_EXCHANGE_OK(GsClientConnection conn, PlayerConnectionRegistry connRegistry,
        ExchangeService exchangeService, IItemDao itemDao)
        => _inner = new CM_EXCHANGE_LOCK(conn, connRegistry, exchangeService, itemDao);

    public override void Read(ref PacketReader r) { }

    public override ValueTask RunAsync(CancellationToken ct) => _inner.RunAsync(ct);
}
