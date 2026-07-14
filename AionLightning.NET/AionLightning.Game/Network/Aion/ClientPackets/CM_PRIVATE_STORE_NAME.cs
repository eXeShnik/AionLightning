using AionLightning.Commons.Network;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Seller sets shop name and opens the private store. Opcode 0x15A (4.5-era packet table).
/// TODO: verify opcode against a live 4.6 client capture before enabling.</summary>
public sealed class CM_PRIVATE_STORE_NAME : AionClientPacket
{
    private readonly GsClientConnection  _conn;
    private readonly PrivateStoreService _privateStoreService;

    private string _name = string.Empty;

    public CM_PRIVATE_STORE_NAME(GsClientConnection conn, PrivateStoreService privateStoreService)
    {
        _conn                = conn;
        _privateStoreService = privateStoreService;
    }

    public override void Read(ref PacketReader r) => _name = r.ReadS();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || player.IsAlreadyDead) return;

        await _privateStoreService.OpenStoreAsync(player, _name, ct);
    }
}
