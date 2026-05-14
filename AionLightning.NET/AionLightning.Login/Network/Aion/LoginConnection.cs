using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;
using AionLightning.Commons.Network;
using AionLightning.Commons.Network.Ncrypt;
using AionLightning.Login.Model;
using AionLightning.Login.Network.Aion.ServerPackets;
using AionLightning.Login.Network.Factories;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.Aion;

public sealed class LoginConnection : AConnection
{
    private readonly ILogger<LoginConnection> _log;
    private readonly AionPacketHandlerFactory _factory;
    private readonly CryptEngine _crypt = new();
    private readonly EncryptedRSAKeyPair _rsaKeyPair;

    public int SessionId { get; }
    public LoginState State { get; set; } = LoginState.CONNECTED;
    public Account? Account { get; set; }
    public SessionKey? SessionKey { get; set; }
    public GameServerInfo? JoinedGs { get; set; }
    public EncryptedRSAKeyPair RsaKeyPair => _rsaKeyPair;
    public ILogger Log => _log;

    public enum LoginState { CONNECTED, AUTHED_GG, AUTHED_LOGIN }

    public LoginConnection(Socket socket, ILogger<LoginConnection> log, AionPacketHandlerFactory factory)
        : base(socket)
    {
        _log = log;
        _factory = factory;
        SessionId = Math.Abs(Random.Shared.Next());
        _rsaKeyPair = KeyGen.GetEncryptedRSAKeyPair();
    }

    protected override async ValueTask OnConnectedAsync(CancellationToken ct)
    {
        _log.LogInformation("[{IP}] Sending SM_INIT (session {SessionId})", IP, SessionId);
        var blowfishKey = new byte[16];
        Random.Shared.NextBytes(blowfishKey);

        // SM_INIT is encrypted with the default blowfish key (client knows it too)
        await SendAsync(new SM_INIT(SessionId, _rsaKeyPair.PublicKey, blowfishKey), ct);
        _log.LogInformation("[{IP}] SM_INIT sent", IP);

        // All subsequent packets use the per-session key
        _crypt.UpdateKey(blowfishKey);
    }

    protected override async ValueTask OnPacketAsync(ReadOnlySequence<byte> frame, CancellationToken ct)
    {
        var data = new byte[frame.Length];
        frame.CopyTo(data);

        _log.LogDebug("[{IP}] RECV raw frameLen={FrameLen} hex={Hex}",
            IP, frame.Length, BitConverter.ToString(data));

        if (!_crypt.Decrypt(data, 0, data.Length))
        {
            _log.LogWarning("Checksum mismatch from {IP} — dropping packet", IP);
            return;
        }

        if (data.Length < 1) return;

        byte opcode = data[0];
        var body = new ReadOnlySequence<byte>(data, 1, data.Length - 1);

        var packet = _factory.Resolve(opcode, State, this);
        if (packet is null) return;

        var reader = new PacketReader(body);
        packet.Read(ref reader);
        await packet.RunAsync(ct);
    }

    public override async ValueTask DisposeAsync()
    {
        var ip = IP;
        await base.DisposeAsync();
        _log.LogInformation("[{IP}] Login connection closed (state={State}, joinedGs={GsId})",
            ip, State, JoinedGs?.Id.ToString() ?? "none");
    }

    public async ValueTask SendAsync(AionServerPacket packet, CancellationToken ct = default)
    {
        var bodyBuf = new ArrayBufferWriter<byte>();
        var w = new PacketWriter(bodyBuf);
        packet.Write(ref w);

        // Java: size = b.limit()-2 = body-1. First packet gets +4 extra for EncXorPass ecx tail.
        bool isFirstPacket = _crypt.IsFirstPacket;
        int encLen = bodyBuf.WrittenCount - 1;
        encLen += 4;
        if (isFirstPacket) encLen += 4;
        encLen += 8 - encLen % 8;

        byte[] enc = new byte[encLen];
        enc[0] = (byte)packet.Opcode;
        bodyBuf.WrittenSpan.CopyTo(enc.AsSpan(1));

        _crypt.Encrypt(enc, 0, encLen);

        int wireLen = 2 + encLen;
        byte[] wire = new byte[wireLen];
        BinaryPrimitives.WriteInt16LittleEndian(wire, (short)wireLen);
        enc.CopyTo(wire.AsSpan(2));

        await WriteRawAsync(wire, ct);

        _log.LogDebug("[{IP}] SEND opcode=0x{Op:X2} bodyLen={BodyLen} encLen={EncLen} wire={Wire}",
            IP, (byte)packet.Opcode, bodyBuf.WrittenCount, encLen, BitConverter.ToString(wire));
    }
}
