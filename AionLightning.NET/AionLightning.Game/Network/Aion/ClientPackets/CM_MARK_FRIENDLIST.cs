using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests the friend list. Sends SM_FRIEND_LIST + SM_MARK_FRIENDLIST. Opcode 0x10C.</summary>
public sealed class CM_MARK_FRIENDLIST : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly ISocialDao               _socialDao;
    private readonly PlayerConnectionRegistry _connRegistry;

    public CM_MARK_FRIENDLIST(GsClientConnection conn, ISocialDao socialDao,
        PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _socialDao    = socialDao;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r) { }

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
        await _conn.SendAsync(new SM_MARK_FRIENDLIST(player.ObjectId), ct);
    }
}
