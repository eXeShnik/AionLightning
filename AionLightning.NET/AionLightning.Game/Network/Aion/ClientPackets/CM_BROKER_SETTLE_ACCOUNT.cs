using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client collects kinah from sold broker items. Opcode 0x140.</summary>
public sealed class CM_BROKER_SETTLE_ACCOUNT : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly BrokerService      _broker;
    private readonly IItemDao           _itemDao;

    public CM_BROKER_SETTLE_ACCOUNT(GsClientConnection conn, BrokerService broker, IItemDao itemDao)
    {
        _conn     = conn;
        _broker   = broker;
        _itemDao  = itemDao;
    }

    public override void Read(ref PacketReader r) => r.ReadD(); // npcId — ignored

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        await _broker.SettleAsync(player, ct);

        // Persist kinah update
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        var kinah = player.Inventory.FindByItemId(182400001);
        if (kinah is not null)
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinah]), ct);
    }
}
