using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;
using AionLightning.Commons.Network;
using AionLightning.Login.Network.Factories;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.GameServer;

public sealed class GsConnection : AConnection
{
    private readonly ILogger<GsConnection> _log;
    private readonly GsPacketHandlerFactory _factory;
    private int _disposed;

    public GsState State { get; set; } = GsState.CONNECTED;
    public GameServerInfo? GameServerInfo { get; set; }
    public ILogger Log => _log;

    public enum GsState { CONNECTED, AUTHED }

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

        _log.LogDebug("[GS {IP}] RECV opcode=0x{Op:X2} len={Len} hex={Hex}",
            IP, opcode, data.Length, BitConverter.ToString(data));

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

        int payloadLen = 1 + bodyBuf.WrittenCount;
        int wireLen = 2 + payloadLen;

        byte[] wire = new byte[wireLen];
        BinaryPrimitives.WriteInt16LittleEndian(wire, (short)wireLen);
        wire[2] = (byte)packet.Opcode;
        bodyBuf.WrittenSpan.CopyTo(wire.AsSpan(3));

        _log.LogDebug("[GS {IP}] SEND opcode=0x{Op:X2} len={Len} hex={Hex}",
            IP, (byte)packet.Opcode, wireLen, BitConverter.ToString(wire));

        await WriteRawAsync(wire, ct);
    }

    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0 && GameServerInfo != null)
        {
            GameServerInfo.GscHandler = null;
            _log.LogInformation("GameServer #{Id} disconnected", GameServerInfo.Id);
        }
        await base.DisposeAsync();
    }
}
