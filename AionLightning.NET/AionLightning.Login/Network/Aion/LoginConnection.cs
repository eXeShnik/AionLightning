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
        var blowfishKey = new byte[16];
        Random.Shared.NextBytes(blowfishKey);

        // SM_INIT is encrypted with the default blowfish key (client knows it too)
        await SendAsync(new SM_INIT(SessionId, _rsaKeyPair.PublicKey, blowfishKey), ct);

        // All subsequent packets use the per-session key
        _crypt.UpdateKey(blowfishKey);
    }

    protected override async ValueTask OnPacketAsync(ReadOnlySequence<byte> frame, CancellationToken ct)
    {
        var data = new byte[frame.Length];
        frame.CopyTo(data);

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

    public async ValueTask SendAsync(AionServerPacket packet, CancellationToken ct = default)
    {
        var bodyBuf = new ArrayBufferWriter<byte>();
        var w = new PacketWriter(bodyBuf);
        packet.Write(ref w);

        // Layout: [opcode(1)][body][zero-padding][checksum(4)]
        // Total must be a multiple of 8 (Blowfish block size)
        int contentLen = 1 + bodyBuf.WrittenCount;
        int encLen = ((contentLen + 4 + 7) / 8) * 8;

        byte[] enc = new byte[encLen];
        enc[0] = packet.Opcode;
        bodyBuf.WrittenSpan.CopyTo(enc.AsSpan(1));

        _crypt.Encrypt(enc, 0, encLen);

        int wireLen = 2 + encLen;
        byte[] wire = new byte[wireLen];
        BinaryPrimitives.WriteInt16LittleEndian(wire, (short)wireLen);
        enc.CopyTo(wire.AsSpan(2));

        await WriteRawAsync(wire, ct);
    }
}
