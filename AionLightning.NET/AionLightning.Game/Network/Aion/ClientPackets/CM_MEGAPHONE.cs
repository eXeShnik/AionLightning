using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client uses a megaphone item to broadcast a server-wide or faction-wide message. Opcode 0x1B4.</summary>
public sealed class CM_MEGAPHONE : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IItemDao                 _itemDao;

    private string _message      = string.Empty;
    private long   _itemUniqueId;

    // Item IDs 188930000–188930008 are all-faction megaphones; others are faction-scoped.
    private static bool IsAllMegaphone(int itemId) => itemId is >= 188930000 and <= 188930008;

    public CM_MEGAPHONE(GsClientConnection conn, PlayerConnectionRegistry connRegistry, IItemDao itemDao)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
        _itemDao      = itemDao;
    }

    public override void Read(ref PacketReader r)
    {
        _message      = r.ReadS();
        _itemUniqueId = r.ReadD();   // Java reads int (D) for objectId
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var item = player.Inventory.All.FirstOrDefault(i => i.UniqueId == _itemUniqueId);
        if (item is null) return;

        bool isAll = IsAllMegaphone(item.ItemId);
        int  itemId = item.ItemId;

        // Consume one megaphone
        item.Count--;
        if (item.Count <= 0)
            player.Inventory.Remove(item.UniqueId);
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);

        var packet = new SM_MEGAPHONE(player.Name, _message, itemId, isAll, player.Race);

        foreach (var conn in _connRegistry.GetAll())
        {
            var target = conn.ActivePlayer;
            if (target is null) continue;
            // Cross-faction megaphones reach everyone; faction megaphones only reach same race
            if (isAll || target.Race == player.Race)
                try { await conn.SendAsync(packet, ct); } catch { }
        }
    }
}
