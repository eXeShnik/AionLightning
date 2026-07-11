using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;
using AionLightning.Chat.Model;
using AionLightning.Commons.Network;
using Microsoft.Extensions.Logging;

namespace AionLightning.Chat.Network.Aion;

public sealed class AionClientConnection : AConnection
{
    public enum ClientState { CONNECTED, AUTHED }

    private readonly ILogger<AionClientConnection> _log;
    private readonly AionPacketHandlerFactory _factory;

    public ClientState State { get; set; } = ClientState.CONNECTED;
    public ChatClient? ChatClient { get; private set; }

    public AionClientConnection(Socket socket, ILogger<AionClientConnection> log, AionPacketHandlerFactory factory)
        : base(socket)
    {
        _log = log;
        _factory = factory;
    }

    public void SetChatClient(ChatClient client)
    {
        ChatClient = client;
        State = ClientState.AUTHED;
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

        // Java BaseClientPacket tolerates short/malformed reads (logs and continues);
        // a PacketFormatException must not tear down the whole chat session.
        try
        {
            var reader = new PacketReader(body);
            packet.Read(ref reader);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[{IP}] Malformed chat packet 0x{Op:X2} — skipped", IP, opcode);
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
        _log.LogDebug("Aion chat client {IP} disconnected", IP);
    }
}
