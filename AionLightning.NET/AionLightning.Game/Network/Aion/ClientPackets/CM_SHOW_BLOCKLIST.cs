using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client opens the block list panel. Opcode 0x17C.</summary>
public sealed class CM_SHOW_BLOCKLIST : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly ISocialDao         _socialDao;

    public CM_SHOW_BLOCKLIST(GsClientConnection conn, ISocialDao socialDao)
    {
        _conn      = conn;
        _socialDao = socialDao;
    }

    public override void Read(ref PacketReader r) { }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var blocks = await _socialDao.GetBlocksAsync(player.ObjectId, ct);
        await _conn.SendAsync(new SM_BLOCK_LIST(blocks), ct);
    }
}
