using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client opens the friend list panel. Opcode 0x184.</summary>
public sealed class CM_SHOW_FRIENDLIST : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly ISocialDao               _socialDao;
    private readonly PlayerConnectionRegistry _connRegistry;

    public CM_SHOW_FRIENDLIST(GsClientConnection conn, ISocialDao socialDao,
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
    }
}
