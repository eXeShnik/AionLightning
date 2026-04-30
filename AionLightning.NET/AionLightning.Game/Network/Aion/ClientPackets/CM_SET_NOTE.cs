using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client sets the character's bio note visible to friends. Opcode 0x118.</summary>
public sealed class CM_SET_NOTE : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly IPlayerDao               _playerDao;
    private readonly ISocialDao               _socialDao;
    private readonly PlayerConnectionRegistry _connRegistry;

    private string _note = string.Empty;

    public CM_SET_NOTE(GsClientConnection conn, IPlayerDao playerDao,
        ISocialDao socialDao, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _playerDao    = playerDao;
        _socialDao    = socialDao;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r) => _note = r.ReadS();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        if (player.Note == _note) return;

        player.Note = _note;
        await _playerDao.UpdateNoteAsync(player.ObjectId, _note, ct);

        // Notify each online friend so their friend list reflects the new note
        var myFriends = await _socialDao.GetFriendsAsync(player.ObjectId, ct);
        if (myFriends.Count == 0) return;

        var onlineIds = new HashSet<int>(_connRegistry.GetAll()
            .Select(c => c.ActivePlayer?.ObjectId ?? 0).Where(id => id != 0));

        foreach (var f in myFriends)
        {
            var fc = _connRegistry.Get(f.PlayerId);
            if (fc?.ActivePlayer is null) continue;
            var friendFriends = await _socialDao.GetFriendsAsync(f.PlayerId, ct);
            try { await fc.SendAsync(new SM_FRIEND_LIST(friendFriends, onlineIds), ct); } catch { }
        }
    }
}
