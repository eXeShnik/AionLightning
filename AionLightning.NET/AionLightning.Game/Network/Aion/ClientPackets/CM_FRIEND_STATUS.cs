using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client reports friend list availability status (invisible/visible/busy).
/// Re-sends the full friend list so the client refreshes online indicators.
/// Opcode 0x148.
/// </summary>
public sealed class CM_FRIEND_STATUS : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly ISocialDao               _socialDao;
    private readonly PlayerConnectionRegistry _connRegistry;

    public CM_FRIEND_STATUS(GsClientConnection conn, ISocialDao socialDao,
        PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _socialDao    = socialDao;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r) => r.ReadC(); // status byte (ignored)

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var friends = await _socialDao.GetFriendsAsync(player.ObjectId, ct);
        var onlineIds = _connRegistry.GetAll()
            .Where(c => c.ActivePlayer is not null)
            .Select(c => c.ActivePlayer!.ObjectId)
            .ToHashSet();

        await _conn.SendAsync(new SM_FRIEND_LIST(friends, onlineIds), ct);
    }
}
