using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;
using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Network.Cs.ServerPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Network.Cs;

public sealed class CsConnection : AConnection
{
    public enum CsState { CONNECTED, AUTHED }

    private readonly ILogger<CsConnection> _log;
    private readonly CsPacketHandlerFactory _factory;
    private readonly GameServerInfoOptions _gsInfo;
    private readonly CsConnectionOptions _csOpts;

    public CsState State { get; set; } = CsState.CONNECTED;

    // Chat client-facing IP + port received from Chat server after auth
    public byte[] ChatClientIp { get; set; } = [127, 0, 0, 1];
    public int ChatClientPort { get; set; } = 10241;

    public CsConnection(Socket socket, ILogger<CsConnection> log,
        CsPacketHandlerFactory factory, GameServerInfoOptions gsInfo, CsConnectionOptions csOpts)
        : base(socket)
    {
        _log = log;
        _factory = factory;
        _gsInfo = gsInfo;
        _csOpts = csOpts;
    }

    protected override async ValueTask OnConnectedAsync(CancellationToken ct)
    {
        _log.LogInformation("Connected to ChatServer — sending SM_CS_AUTH");
        await SendAsync(new SM_CS_AUTH(_gsInfo.Id, _csOpts.Password), ct);
    }

    protected override async ValueTask OnPacketAsync(ReadOnlySequence<byte> frame, CancellationToken ct)
    {
        if (frame.Length < 1) return;

        var data = new byte[frame.Length];
        frame.CopyTo(data);

        byte opcode = data[0];
        var body = new ReadOnlySequence<byte>(data, 1, data.Length - 1);

        var packet = _factory.Resolve(opcode, State, this);
        if (packet is null) return;

        // Tolerate short/malformed reads like Java BaseClientPacket — don't drop the chat link.
        try
        {
            var reader = new PacketReader(body);
            packet.Read(ref reader);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[CS] Malformed packet 0x{Op:X2} — skipped", opcode);
            return;
        }
        await packet.RunAsync(ct);
    }

    public async ValueTask SendAsync(AionServerPacket packet, CancellationToken ct = default)
    {
        var bodyBuf = new ArrayBufferWriter<byte>();
        var w = new PacketWriter(bodyBuf);
        packet.Write(ref w);

        int wireLen = 2 + 1 + bodyBuf.WrittenCount;
        byte[] wire = new byte[wireLen];
        BinaryPrimitives.WriteInt16LittleEndian(wire, (short)wireLen);
        wire[2] = (byte)packet.Opcode;
        bodyBuf.WrittenSpan.CopyTo(wire.AsSpan(3));

        await WriteRawAsync(wire, ct);
    }
}
