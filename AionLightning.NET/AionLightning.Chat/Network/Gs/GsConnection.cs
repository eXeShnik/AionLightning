using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;
using AionLightning.Commons.Network;
using Microsoft.Extensions.Logging;

namespace AionLightning.Chat.Network.Gs;

public sealed class GsConnection : AConnection
{
    public enum GsState { CONNECTED, AUTHED }

    private readonly ILogger<GsConnection> _log;
    private readonly GsPacketHandlerFactory _factory;

    public GsState State { get; set; } = GsState.CONNECTED;
    public byte GsId { get; set; }

    public GsConnection(Socket socket, ILogger<GsConnection> log, GsPacketHandlerFactory factory)
        : base(socket)
    {
        _log = log;
        _factory = factory;
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

        // Tolerate short/malformed reads like Java BaseClientPacket — don't drop the GS link.
        try
        {
            var reader = new PacketReader(body);
            packet.Read(ref reader);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[GS {IP}] Malformed packet 0x{Op:X2} — skipped", IP, opcode);
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

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        _log.LogInformation("GameServer #{Id} disconnected", GsId);
    }
}
