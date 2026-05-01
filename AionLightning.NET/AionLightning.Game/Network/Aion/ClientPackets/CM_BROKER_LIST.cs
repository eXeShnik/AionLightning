using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client browses the broker by category mask. Opcode 0x159.</summary>
public sealed class CM_BROKER_LIST : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly BrokerService      _broker;

    private int _sortType;
    private int _page;
    private int _listMask;

    public CM_BROKER_LIST(GsClientConnection conn, BrokerService broker)
    {
        _conn   = conn;
        _broker = broker;
    }

    public override void Read(ref PacketReader r)
    {
        r.ReadD();           // brokerId (npc objectId — ignored)
        _sortType = r.ReadC();
        _page     = r.ReadH();
        _listMask = r.ReadH();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var items = _broker.GetPage(player.Race, _page, null);
        int total = _broker.GetTotalCount(player.Race, null);
        await _conn.SendAsync(SM_BROKER_SERVICE.SearchedItems(items, total, _page), ct);
    }
}
