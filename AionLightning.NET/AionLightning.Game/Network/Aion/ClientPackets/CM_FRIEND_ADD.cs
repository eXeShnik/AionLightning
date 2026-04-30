using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Model.Social;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client adds a friend by name. Opcode 0x10D.</summary>
public sealed class CM_FRIEND_ADD : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly IPlayerDao               _playerDao;
    private readonly ISocialDao               _socialDao;
    private readonly PlayerConnectionRegistry _connRegistry;

    private string _targetName = string.Empty;

    public CM_FRIEND_ADD(GsClientConnection conn, IPlayerDao playerDao,
        ISocialDao socialDao, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _playerDao    = playerDao;
        _socialDao    = socialDao;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r) => _targetName = r.ReadS();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var target = await _playerDao.FindByNameAsync(_targetName, ct);
        if (target is null)
        {
            await _conn.SendAsync(new SM_FRIEND_RESPONSE(SM_FRIEND_RESPONSE.ResultCode.NotFound), ct);
            return;
        }

        if (await _socialDao.AreFriendsAsync(player.ObjectId, target.ObjectId, ct))
        {
            await _conn.SendAsync(new SM_FRIEND_RESPONSE(SM_FRIEND_RESPONSE.ResultCode.AlreadyFriends), ct);
            return;
        }

        await _socialDao.AddFriendAsync(player.ObjectId, target.ObjectId, ct);

        bool isOnline = _connRegistry.GetByName(target.Name) is not null;
        var entry = new FriendEntry(target.ObjectId, target.Name, target.Level, target.PlayerClass, target.Race, "", target.Note);
        await _conn.SendAsync(new SM_FRIEND_RESPONSE(entry, SM_FRIEND_RESPONSE.ResultCode.Added, isOnline), ct);
    }
}
