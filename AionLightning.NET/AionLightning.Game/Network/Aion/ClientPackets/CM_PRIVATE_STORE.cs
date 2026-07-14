using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Seller submits store item list, or cancels with 0 items to close the shop. Opcode 0x155
/// (4.5-era packet table). TODO: verify opcode against a live 4.6 client capture before enabling.</summary>
public sealed class CM_PRIVATE_STORE : AionClientPacket
{
    private readonly GsClientConnection    _conn;
    private readonly PrivateStoreService   _privateStoreService;

    private readonly List<PrivateStoreItem> _items = new();

    public CM_PRIVATE_STORE(GsClientConnection conn, PrivateStoreService privateStoreService)
    {
        _conn                = conn;
        _privateStoreService = privateStoreService;
    }

    public override void Read(ref PacketReader r)
    {
        int count = r.ReadH();
        for (int i = 0; i < count; i++)
        {
            int uniqueId = r.ReadD();
            int itemId   = r.ReadD();
            int qty      = r.ReadH();
            int price    = r.ReadD();
            _items.Add(new PrivateStoreItem(uniqueId, itemId, qty, price));
        }
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || player.IsAlreadyDead) return;

        if (_items.Count == 0)
        {
            await _privateStoreService.CloseStoreAsync(player, ct);
            return;
        }

        var result = _privateStoreService.SetItems(player, _items);
        if (result != PrivateStoreService.SetItemsResult.Success)
        {
            // note: Java reports distinct messages here (generic "Invalid item." chat line, or
            // msg code 1300344 with the item's DescriptionId for non-tradeable items). Neither is
            // modeled 1:1 — InventoryFull is reused as the closest existing generic error signal.
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.InventoryFull(), ct);
        }
    }
}
