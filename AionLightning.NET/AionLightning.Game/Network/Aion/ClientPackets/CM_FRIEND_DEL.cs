using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client removes a friend by object ID. Opcode 0x132.</summary>
public sealed class CM_FRIEND_DEL : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly ISocialDao               _socialDao;
    private readonly PlayerConnectionRegistry _connRegistry;

    private int _friendObjectId;

    public CM_FRIEND_DEL(GsClientConnection conn, ISocialDao socialDao,
        PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _socialDao    = socialDao;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r) => _friendObjectId = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        await _socialDao.RemoveFriendAsync(player.ObjectId, _friendObjectId, ct);

        // Notify the deleted friend if they are online
        var targetConn = _connRegistry.Get(_friendObjectId);
        if (targetConn?.ActivePlayer is not null)
        {
            try { await targetConn.SendAsync(new SM_FRIEND_NOTIFY(SM_FRIEND_NOTIFY.Deleted, player.Name), ct); } catch { }
        }

        // ResultCode.Removed with empty entry signals deletion to client
        await _conn.SendAsync(new SM_FRIEND_RESPONSE(SM_FRIEND_RESPONSE.ResultCode.Removed), ct);
    }
}
