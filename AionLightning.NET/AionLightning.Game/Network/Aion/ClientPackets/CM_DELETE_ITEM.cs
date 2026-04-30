using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_DELETE_ITEM : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IItemDao _itemDao;

    private long _uniqueId;

    public CM_DELETE_ITEM(GsClientConnection conn, IItemDao itemDao)
    {
        _conn    = conn;
        _itemDao = itemDao;
    }

    public override void Read(ref PacketReader r) => _uniqueId = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var item = player.Inventory.Get(_uniqueId);
        if (item is null || item.IsEquipped) return;

        player.Inventory.Remove(_uniqueId);
        await _itemDao.DeleteAsync(_uniqueId, ct);
        await _conn.SendAsync(new SM_DELETE_ITEM(_uniqueId), ct);
    }
}
