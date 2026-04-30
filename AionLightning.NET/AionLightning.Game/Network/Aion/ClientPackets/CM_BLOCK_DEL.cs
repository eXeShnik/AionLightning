using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client unblocks a player by object ID. Opcode 0x145.</summary>
public sealed class CM_BLOCK_DEL : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly ISocialDao         _socialDao;

    private int _blockedObjectId;

    public CM_BLOCK_DEL(GsClientConnection conn, ISocialDao socialDao)
    {
        _conn      = conn;
        _socialDao = socialDao;
    }

    public override void Read(ref PacketReader r) => _blockedObjectId = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        await _socialDao.RemoveBlockAsync(player.ObjectId, _blockedObjectId, ct);

        var blocks = await _socialDao.GetBlocksAsync(player.ObjectId, ct);
        await _conn.SendAsync(new SM_BLOCK_LIST(blocks), ct);
    }
}
