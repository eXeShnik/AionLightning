using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client searches broker by item IDs. Opcode 0x15E.</summary>
public sealed class CM_BROKER_SEARCH : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly BrokerService      _broker;

    private int       _sortType;
    private int       _page;
    private List<int> _itemIds = new();

    public CM_BROKER_SEARCH(GsClientConnection conn, BrokerService broker)
    {
        _conn   = conn;
        _broker = broker;
    }

    public override void Read(ref PacketReader r)
    {
        r.ReadD();       // brokerId — ignored
        _sortType = r.ReadC();
        _page     = r.ReadH();
        r.ReadH();       // mask — ignored
        int count = r.ReadH();
        _itemIds  = new List<int>(count);
        for (int i = 0; i < count; i++)
            _itemIds.Add(r.ReadD());
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var filter = _itemIds.Count > 0 ? (IReadOnlyList<int>)_itemIds : null;
        var items  = _broker.GetPage(player.Race, _page, filter);
        int total  = _broker.GetTotalCount(player.Race, filter);
        await _conn.SendAsync(SM_BROKER_SERVICE.SearchedItems(items, total, _page), ct);
    }
}
