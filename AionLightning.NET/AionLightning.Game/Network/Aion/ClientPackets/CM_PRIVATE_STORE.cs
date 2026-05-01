using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Seller submits store item list, or cancels with 0 items to close the shop. Opcode 0x155.</summary>
public sealed class CM_PRIVATE_STORE : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;

    private readonly List<PrivateStoreItem> _items = new();

    public CM_PRIVATE_STORE(GsClientConnection conn, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
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
            await CloseStoreAsync(player, ct);
            return;
        }

        // Validate every listed item is actually in the player's bag at the claimed quantity
        var validated = new List<PrivateStoreItem>(_items.Count);
        foreach (var si in _items)
        {
            var inv = player.Inventory.Get(si.UniqueId);
            if (inv is null || inv.ItemId != si.ItemId || inv.Count < si.Count || si.Price <= 0)
            {
                await _conn.SendAsync(SM_SYSTEM_MESSAGE.InventoryFull(), ct); // generic error
                return;
            }
            validated.Add(si);
        }

        player.StoreItems = validated;
        player.State     |= CreatureState.PrivateShop;
    }

    private async ValueTask CloseStoreAsync(Player player, CancellationToken ct)
    {
        player.StoreItems = null;
        player.StoreName  = string.Empty;
        player.State     &= ~CreatureState.PrivateShop;

        int worldId      = player.Position.WorldId;
        var closeEmotion = new SM_EMOTION(player, EmotionType.CLOSE_PRIVATESHOP);

        foreach (var conn in _connRegistry.GetAll())
        {
            if (conn.ActivePlayer?.Position.WorldId != worldId) continue;
            try { await conn.SendAsync(closeEmotion, ct); } catch { }
        }
    }
}
