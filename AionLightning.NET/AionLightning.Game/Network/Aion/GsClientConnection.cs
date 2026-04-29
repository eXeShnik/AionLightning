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
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly GsCrypt _crypt = new();

    public AionState State { get; set; } = AionState.CONNECTED;

    // Set after CM_L2AUTH_LOGIN_CHECK succeeds
    public int AccountId { get; private set; }

    // Set after CM_ENTER_WORLD completes
    public Player? ActivePlayer { get; set; }

    public GsClientConnection(Socket socket, ILogger<GsClientConnection> log,
        GsPacketHandlerFactory factory, LsConnectionHolder ls, CsConnectionHolder cs,
        GameAccountRegistry registry, IPlayerDao playerDao, GameWorld world,
        PlayerConnectionRegistry connRegistry)
        : base(socket)
    {
        _log          = log;
        _factory      = factory;
        _ls           = ls;
        _cs           = cs;
        _registry     = registry;
        _playerDao    = playerDao;
        _world        = world;
        _connRegistry = connRegistry;
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

        var reader = new PacketReader(body);
        packet.Read(ref reader);
        await packet.RunAsync(ct);
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

        // Unregister from broadcast registry and world
        _connRegistry.Unregister(player.ObjectId);
        _world.Remove(player);

        _log.LogInformation("Player {Name} (id={Id}) disconnected — saving state", player.Name, player.ObjectId);

        try
        {
            await _playerDao.UpdatePositionAsync(player.ObjectId, player.Position, CancellationToken.None);
            await _playerDao.UpdateOnlineAsync(player.ObjectId, online: false, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to save player state for {Name}", player.Name);
        }

        var csConn = _cs.Current;
        if (csConn is not null)
        {
            try { await csConn.SendAsync(new SM_CS_PLAYER_LOGOUT(player.ObjectId), CancellationToken.None); }
            catch (Exception ex) { _log.LogError(ex, "Failed to notify Chat server of logout for {Name}", player.Name); }
        }
    }
}
