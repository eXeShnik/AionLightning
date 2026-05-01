using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;
using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ClientPackets;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Network.Cs;
using AionLightning.Game.Network.Cs.ServerPackets;
using AionLightning.Game.Network.Ls;
using AionLightning.Game.Network.Ls.ServerPackets;
using AionLightning.Game.Services;
using GameWorld = AionLightning.Game.World.World;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Network.Aion;

public sealed class GsClientConnection : AConnection
{
    public enum AionState { CONNECTED, AUTHED, IN_GAME }

    private readonly ILogger<GsClientConnection> _log;
    private readonly GsPacketHandlerFactory _factory;
    private readonly LsConnectionHolder _ls;
    private readonly CsConnectionHolder _cs;
    private readonly GameAccountRegistry _registry;
    private readonly IPlayerDao _playerDao;
    private readonly IItemDao _itemDao;
    private readonly IQuestDao _questDao;
    private readonly ISocialDao _socialDao;
    private readonly ILegionDao _legionDao;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly GroupService _groupService;
    private readonly DuelService  _duelService;
    private readonly LegionService _legionService;
    private readonly GsCrypt _crypt = new();

    public AionState State { get; set; } = AionState.CONNECTED;

    // Set after CM_L2AUTH_LOGIN_CHECK succeeds
    public int AccountId { get; private set; }

    // Set after CM_ENTER_WORLD completes
    public Player? ActivePlayer { get; set; }

    public GsClientConnection(Socket socket, ILogger<GsClientConnection> log,
        GsPacketHandlerFactory factory, LsConnectionHolder ls, CsConnectionHolder cs,
        GameAccountRegistry registry, IPlayerDao playerDao, IItemDao itemDao, IQuestDao questDao,
        ISocialDao socialDao, ILegionDao legionDao, GameWorld world, PlayerConnectionRegistry connRegistry,
        GroupService groupService, DuelService duelService, LegionService legionService)
        : base(socket)
    {
        _log           = log;
        _factory       = factory;
        _ls            = ls;
        _cs            = cs;
        _registry      = registry;
        _playerDao     = playerDao;
        _itemDao       = itemDao;
        _questDao      = questDao;
        _socialDao     = socialDao;
        _legionDao     = legionDao;
        _world         = world;
        _connRegistry  = connRegistry;
        _groupService  = groupService;
        _duelService   = duelService;
        _legionService = legionService;
    }

    protected override async ValueTask OnConnectedAsync(CancellationToken ct)
    {
        int falseKey = _crypt.EnableKey();
        await SendAsync(new SM_KEY(falseKey), ct);
    }

    protected override async ValueTask OnPacketAsync(ReadOnlySequence<byte> frame, CancellationToken ct)
    {
        if (frame.Length < 5) return;

        var data = new byte[frame.Length];
        frame.CopyTo(data);

        if (!_crypt.Decrypt(data, 0, data.Length))
        {
            _log.LogWarning("Decrypt/checksum failed from {IP} — dropping packet", IP);
            return;
        }

        // After decryption: [opcode(2)][0x65(1)][~opcode(2)][body]
        ushort opcode = (ushort)(data[0] | (data[1] << 8));
        var body = new ReadOnlySequence<byte>(data, 5, data.Length - 5);

        if (opcode == 0x0177)
        {
            await HandleLoginCheckAsync(body, ct);
            return;
        }

        var packet = _factory.Resolve(opcode, State, this);
        if (packet is null) return;

        try
        {
            var reader = new PacketReader(body);
            packet.Read(ref reader);
            await packet.RunAsync(ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unhandled error in packet 0x{Opcode:X4} from {IP} — connection kept alive", opcode, IP);
        }
    }

    private async ValueTask HandleLoginCheckAsync(ReadOnlySequence<byte> body, CancellationToken ct)
    {
        var pkt = new CM_L2AUTH_LOGIN_CHECK();
        var reader = new PacketReader(body);
        pkt.Read(ref reader);

        var tcs = _registry.RegisterPending(pkt.AccountId);

        var lsConn = _ls.Current;
        if (lsConn is null)
        {
            _log.LogWarning("Account {Id} auth rejected — no LS connection", pkt.AccountId);
            _registry.CancelPending(pkt.AccountId);
            await DisposeAsync();
            return;
        }

        await lsConn.SendAsync(new SM_ACCOUNT_AUTH(pkt.AccountId, pkt.LoginOk, pkt.PlayOk1, pkt.PlayOk2), ct);

        bool ok;
        try
        {
            ok = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        }
        catch (TimeoutException)
        {
            _registry.CancelPending(pkt.AccountId);
            ok = false;
        }

        if (ok)
        {
            AccountId = pkt.AccountId;
            State = AionState.AUTHED;
            _log.LogInformation("Account {Id} authenticated on GS from {IP}", pkt.AccountId, IP);
        }
        else
        {
            _log.LogWarning("Account {Id} auth failed from {IP} — disconnecting", pkt.AccountId, IP);
            await DisposeAsync();
        }
    }

    public async ValueTask SendAsync(AionServerPacket packet, CancellationToken ct = default)
    {
        var bodyBuf = new ArrayBufferWriter<byte>();
        var w = new PacketWriter(bodyBuf);
        packet.Write(ref w);

        // 5-byte header: [encoded_opcode(2)][0x43(1)][~encoded_opcode(2)]
        ushort encodedOp = GsCrypt.EncodeOpcode(packet.Opcode);
        int payloadLen   = 5 + bodyBuf.WrittenCount;

        byte[] payload = new byte[payloadLen];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, encodedOp);
        payload[2] = 0x43;
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(3), (ushort)(~encodedOp));
        bodyBuf.WrittenSpan.CopyTo(payload.AsSpan(5));

        _crypt.Encrypt(payload, 0, payloadLen);

        int wireLen = 2 + payloadLen;
        byte[] wire = new byte[wireLen];
        BinaryPrimitives.WriteInt16LittleEndian(wire, (short)wireLen);
        payload.CopyTo(wire.AsSpan(2));

        await WriteRawAsync(wire, ct);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        var player = ActivePlayer;
        if (player is null) return;

        // Unregister from broadcast registry and world; notify zone peers before removal
        int worldId = player.Position.WorldId;
        _connRegistry.Unregister(player.ObjectId);
        _world.Remove(player);

        var deletePkt = new SM_DELETE(player.ObjectId);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(deletePkt, CancellationToken.None); } catch { /* ignore */ }

        _log.LogInformation("Player {Name} (id={Id}) disconnected — saving state", player.Name, player.ObjectId);

        // Notify online friends that this player went offline (player already unregistered, so onlineIds excludes them)
        try
        {
            var myFriends = await _socialDao.GetFriendsAsync(player.ObjectId, CancellationToken.None);
            if (myFriends.Count > 0)
            {
                var nowOnlineIds = new HashSet<int>(_connRegistry.GetAll()
                    .Select(c => c.ActivePlayer?.ObjectId ?? 0).Where(id => id != 0));
                var logoutNotify = new SM_FRIEND_NOTIFY(SM_FRIEND_NOTIFY.Logout, player.Name);
                foreach (var f in myFriends)
                {
                    var fc = _connRegistry.Get(f.PlayerId);
                    if (fc?.ActivePlayer is null) continue;
                    try { await fc.SendAsync(logoutNotify, CancellationToken.None); } catch { }
                    var friendFriends = await _socialDao.GetFriendsAsync(f.PlayerId, CancellationToken.None);
                    try { await fc.SendAsync(new SM_FRIEND_LIST(friendFriends, nowOnlineIds), CancellationToken.None); } catch { }
                }
            }
        }
        catch (Exception ex) { _log.LogError(ex, "Failed to notify friends of {Name} logout", player.Name); }

        try
        {
            await _playerDao.UpdatePositionAsync(player.ObjectId, player.Position, CancellationToken.None);
            await _playerDao.UpdateExpLevelAsync(player.ObjectId, player.Exp, player.Level, CancellationToken.None);
            await _playerDao.UpdateAbyssAsync(player.ObjectId, player.AbyssPoints, player.AbyssRank, CancellationToken.None);
            await _playerDao.UpdateAbyssKillStatsAsync(player.ObjectId,
                player.AbyssAllKill, player.AbyssMaxRank,
                player.AbyssDailyKill, player.AbyssDailyAp,
                player.AbyssWeeklyKill, player.AbyssWeeklyAp,
                player.AbyssLastKill, player.AbyssLastAp, CancellationToken.None);
            await _playerDao.UpdateHpMpAsync(player.ObjectId, player.CurrentHp, player.CurrentMp, CancellationToken.None);
            await _playerDao.UpdateFpAsync(player.ObjectId, player.CurrentFp, CancellationToken.None);
            await _playerDao.UpdateDpAsync(player.ObjectId, player.Dp, CancellationToken.None);
            await _playerDao.UpdateSoulSicknessAsync(player.ObjectId, player.SoulSicknessCount, CancellationToken.None);
            await _playerDao.UpdateOnlineAsync(player.ObjectId, online: false, CancellationToken.None);
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, CancellationToken.None);
            await _itemDao.SaveWarehouseAsync(player.ObjectId, player.Warehouse.All, CancellationToken.None);
            await _itemDao.SaveAccountWarehouseAsync(AccountId, player.AccountWarehouse.All, CancellationToken.None);
            if (player.Legion is { } saveLegion)
                await _legionDao.SaveWarehouseItemsAsync(saveLegion.LegionId, saveLegion.WarehouseItems.All, CancellationToken.None);
            await _questDao.SaveAllAsync(player.ObjectId, player.Quests.Active, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to save player state for {Name}", player.Name);
        }

        // Clear any active duel on disconnect
        _duelService.RemovePlayer(player.ObjectId);

        // Mark legion member offline; notify remaining members (offline status, not remove)
        var playerLegion = player.Legion;
        if (playerLegion is not null && playerLegion.Members.TryGetValue(player.ObjectId, out var legionMember))
        {
            legionMember.IsOnline = false;
            var offlinePkt = new SM_LEGION_UPDATE_MEMBER(legionMember, isOnline: false);
            foreach (var lm in playerLegion.Members.Values)
            {
                if (lm.ObjectId == player.ObjectId) continue;
                var lmc = _connRegistry.Get(lm.ObjectId);
                if (lmc is not null)
                    try { await lmc.SendAsync(offlinePkt, CancellationToken.None); } catch { }
            }
        }

        // Leave group and notify remaining members via SM_GROUP_MEMBER_INFO(Disconnected)
        var leftGroup = _groupService.LeaveGroup(player);
        if (leftGroup is not null)
        {
            var disconnectNotify = new SM_GROUP_MEMBER_INFO(leftGroup.GroupId, player, SM_GROUP_MEMBER_INFO.GroupEvent.Disconnected);
            foreach (var member in leftGroup.Members)
            {
                var memberConn = _connRegistry.Get(member.ObjectId);
                if (memberConn is not null)
                    try { await memberConn.SendAsync(disconnectNotify, CancellationToken.None); } catch { /* ignore */ }
            }

            // If the group disbanded (fell below 2 members), clear the survivor's group UI
            if (leftGroup.Members.Count < 2 && leftGroup.Members.Count > 0)
            {
                var survivor     = leftGroup.Members[0];
                var survivorConn = _connRegistry.Get(survivor.ObjectId);
                if (survivorConn is not null)
                {
                    try { await survivorConn.SendAsync(new SM_LEAVE_GROUP_MEMBER(), CancellationToken.None); } catch { }
                    try { await survivorConn.SendAsync(new SM_GROUP_INFO(), CancellationToken.None); } catch { }
                }
            }
        }

        var csConn = _cs.Current;
        if (csConn is not null)
        {
            try { await csConn.SendAsync(new SM_CS_PLAYER_LOGOUT(player.ObjectId), CancellationToken.None); }
            catch (Exception ex) { _log.LogError(ex, "Failed to notify Chat server of logout for {Name}", player.Name); }
        }
    }
}
