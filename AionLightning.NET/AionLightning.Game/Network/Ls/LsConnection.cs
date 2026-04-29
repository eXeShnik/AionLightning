using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;
using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Network.Ls.ServerPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Network.Ls;

public sealed class LsConnection : AConnection
{
    private readonly ILogger<LsConnection> _log;
    private readonly LsPacketHandlerFactory _factory;
    private readonly GameServerInfoOptions _info;

    public LsState State { get; set; } = LsState.CONNECTED;
    public GameServerInfoOptions Info => _info;

    public enum LsState { CONNECTED, AUTHED }

    public LsConnection(Socket socket, ILogger<LsConnection> log,
        LsPacketHandlerFactory factory, GameServerInfoOptions info) : base(socket)
    {
        _log = log;
        _factory = factory;
        _info = info;
    }

    protected override async ValueTask OnConnectedAsync(CancellationToken ct)
    {
        _log.LogInformation("Connected to LoginServer — sending SM_GS_AUTH");
        await SendAsync(new SM_GS_AUTH(_info), ct);
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
        wire[2] = packet.Opcode;
        bodyBuf.WrittenSpan.CopyTo(wire.AsSpan(3));

        await WriteRawAsync(wire, ct);
    }
}
